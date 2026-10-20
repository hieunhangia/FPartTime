using Domain.Shared.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Domain;
using Domain.Shared.Enums;
using Domain.Models.EmployerSubscriptions;
using Domain.Models.Users;

namespace DbMigrator;

public class DataSeeder(ApplicationDbContext db)
{
    private readonly PasswordHasher<User> _hasher = new();

    public async Task SeedAsync()
    {
        Console.WriteLine("🌱 Bắt đầu quá trình nạp dữ liệu (Seed Data)...");

        await SeedSqlFileIfEmptyAsync(() => db.Provinces.AnyAsync(), "address.sql");
        await SeedRolesAsync();
        await SeedUsersAsync();
        await SeedSubscriptionPlansAsync();
        await SeedSqlFileIfEmptyAsync(() => db.Notifications.AnyAsync(), "notification.sql");
        await SeedSqlFileIfEmptyAsync(() => db.Jobs.AnyAsync(), "job.sql");

        Console.WriteLine("✅ Nạp dữ liệu hoàn tất thành công!");
    }

    private async Task SeedRolesAsync()
    {
        Console.WriteLine("➡️ Đang kiểm tra và tạo Roles...");
        foreach (var roleName in Role.AllRoles)
        {
            var exists = await db.Roles.AnyAsync(r => r.Name == roleName);
            if (!exists)
            {
                var role = new IdentityRole<Guid>
                {
                    Id = Guid.NewGuid(),
                    Name = roleName,
                    NormalizedName = roleName.ToUpperInvariant(),
                    ConcurrencyStamp = Guid.NewGuid().ToString()
                };
                db.Roles.Add(role);
            }
        }

        await db.SaveChangesAsync();
    }

    private async Task SeedUsersAsync()
    {
        Console.WriteLine("➡️ Đang kiểm tra và tạo người dùng mặc định...");
        await CreateUserIfNotExistsAsync("0000000000", "Admin@123", Role.AllRoles);
        await CreateUserIfNotExistsAsync("0123456789", "Manager@123", Role.Manager);
        for (var i = 1; i <= 5; i++)
        {
            await CreateUserIfNotExistsAsync($"000000000{i}", "Candidate@123", Role.Candidate);
            await CreateUserIfNotExistsAsync($"00000000{i}0", "Employer@123", Role.Employer);
            await CreateUserIfNotExistsAsync($"0000000{i}00", "Censor@123", Role.Censor);
        }
    }

    private async Task CreateUserIfNotExistsAsync(string phoneNumber, string password, params string[] roleNames)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phoneNumber);
        if (user == null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                UserName = phoneNumber,
                PhoneNumber = phoneNumber,
                NormalizedUserName = phoneNumber.ToUpperInvariant(),
                SecurityStamp = Guid.NewGuid().ToString(),
                LockoutEnabled = true
            };
            user.PasswordHash = _hasher.HashPassword(user, password);

            db.Users.Add(user);
            await db.SaveChangesAsync();
        }

        foreach (var roleName in roleNames)
        {
            var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
            if (role != null)
            {
                var inRole = await db.UserRoles.AnyAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id);
                if (!inRole)
                {
                    db.UserRoles.Add(new IdentityUserRole<Guid>
                    {
                        UserId = user.Id,
                        RoleId = role.Id
                    });
                }
            }
        }

        if (roleNames.Contains(Role.Candidate) && !await db.Candidates.AnyAsync(c => c.UserId == user.Id))
        {
            db.Candidates.Add(new CandidateProfile
            {
                UserId = user.Id,
                AvatarFilePath = BusinessRuleConstants.DefaultAvatarFilePath,
                Bio = "Ứng viên mẫu",
                FullName = $"Ứng viên {user.UserName}",
                DateOfBirth = new DateOnly(1990, 1, 1),
                Gender = Gender.Male,
                PreferredWorkCommuneCode = "16279",
                PreferredWorkSchedules = new List<CandidateSchedule>
                {
                    new() { DayOfWeek = DayOfWeek.Monday, Shift = Shift.Morning },
                    new() { DayOfWeek = DayOfWeek.Tuesday, Shift = Shift.Afternoon },
                    new() { DayOfWeek = DayOfWeek.Wednesday, Shift = Shift.Evening },
                    new() { DayOfWeek = DayOfWeek.Thursday, Shift = Shift.Night },
                    new() { DayOfWeek = DayOfWeek.Friday, Shift = Shift.Morning }
                }
            });
        }

        if (roleNames.Contains(Role.Employer) && !await db.Employers.AnyAsync(e => e.UserId == user.Id))
        {
            db.Employers.Add(new EmployerProfile
            {
                UserId = user.Id,
                LogoFilePath = BusinessRuleConstants.DefaultEmployerLogoFilePath,
                CompanyName = $"Công ty {user.UserName}",
                MaxActivePosts = BusinessRuleConstants.Employers.DefaultMaxActivePosts,
                TaxCode = $"TAX-{user.UserName}",
                VerificationDocumentFilePath =
                    $"{BusinessRuleConstants.ObjectStorage.EmployerRegistration.BasePath}/{BusinessRuleConstants.ObjectStorage.EmployerRegistration.Document}/{user.UserName}.pdf",
                ContactPhone = user.PhoneNumber,
                Description = "Công ty mẫu"
            });
        }

        if (roleNames.Contains(Role.Censor) && !await db.Censors.AnyAsync(c => c.UserId == user.Id))
        {
            db.Censors.Add(new CensorProfile { UserId = user.Id });
        }

        await db.SaveChangesAsync();
    }

    private async Task SeedSqlFileIfEmptyAsync(Func<Task<bool>> hasDataAsync, string fileName)
    {
        if (await hasDataAsync())
        {
            Console.WriteLine($"ℹ️ Không cần nạp dữ liệu từ {fileName} vì đã có sẵn dữ liệu.");
            return;
        }

        var filePath = Path.IsPathRooted(fileName)
            ? fileName
            : Path.Combine(AppContext.BaseDirectory, "Sample Data", fileName);

        if (!File.Exists(filePath))
        {
            Console.WriteLine($"⚠️  Không tìm thấy file SQL: {filePath}");
            return;
        }

        Console.WriteLine($"➡️ Đang nạp dữ liệu từ {fileName}...");
        var sql = await File.ReadAllTextAsync(filePath);
        await db.Database.ExecuteSqlRawAsync(sql);
        Console.WriteLine($"  [+] Đã thực thi {fileName} thành công.");
    }

    private async Task SeedSubscriptionPlansAsync()
    {
        Console.WriteLine("➡️ Đang kiểm tra và tạo dữ liệu gói đăng ký (Subscription Plans)...");
        if (await db.EmployerSubscriptionPlans.AnyAsync())
        {
            Console.WriteLine("ℹ️ Đã có dữ liệu gói đăng ký, bỏ qua.");
            return;
        }

        db.EmployerSubscriptionPlans.AddRange(
            new EmployerSubscriptionPlan
            {
                Tier = EmployerSubscriptionTier.Plus,
                Duration = EmployerSubscriptionDuration.Monthly,
                Name = "Gói Plus (1 Tháng)",
                Description = "Tăng thêm 5 tin đăng hoạt động, tặng 5 lượt đẩy tin.",
                Price = BusinessRuleConstants.Employers.Subscriptions.Plus.MonthlyPrice,
                DurationDays = BusinessRuleConstants.Employers.Subscriptions.MonthlyDurationDays,
                ExtraActivePosts = BusinessRuleConstants.Employers.Subscriptions.Plus.ExtraActivePosts,
                FreeBumps = BusinessRuleConstants.Employers.Subscriptions.Plus.MonthlyFreeBumps,
                CanRecommendCandidates = false,
                CanQueryCandidates = false,
                IsActive = true
            },
            new EmployerSubscriptionPlan
            {
                Tier = EmployerSubscriptionTier.Plus,
                Duration = EmployerSubscriptionDuration.Yearly,
                Name = "Gói Plus (1 Năm)",
                Description = "Tăng thêm 5 tin đăng hoạt động, tặng 20 lượt đẩy tin, tiết kiệm hơn.",
                Price = BusinessRuleConstants.Employers.Subscriptions.Plus.YearlyPrice,
                DurationDays = BusinessRuleConstants.Employers.Subscriptions.YearlyDurationDays,
                ExtraActivePosts = BusinessRuleConstants.Employers.Subscriptions.Plus.ExtraActivePosts,
                FreeBumps = BusinessRuleConstants.Employers.Subscriptions.Plus.YearlyFreeBumps,
                CanRecommendCandidates = false,
                CanQueryCandidates = false,
                IsActive = true
            },
            new EmployerSubscriptionPlan
            {
                Tier = EmployerSubscriptionTier.Pro,
                Duration = EmployerSubscriptionDuration.Monthly,
                Name = "Gói Pro (1 Tháng)",
                Description = "Tăng thêm 10 tin đăng hoạt động, tặng 10 lượt đẩy tin, gợi ý ứng viên phù hợp cho từng công việc.",
                Price = BusinessRuleConstants.Employers.Subscriptions.Pro.MonthlyPrice,
                DurationDays = BusinessRuleConstants.Employers.Subscriptions.MonthlyDurationDays,
                ExtraActivePosts = BusinessRuleConstants.Employers.Subscriptions.Pro.ExtraActivePosts,
                FreeBumps = BusinessRuleConstants.Employers.Subscriptions.Pro.MonthlyFreeBumps,
                CanRecommendCandidates = true,
                CanQueryCandidates = false,
                IsActive = true
            },
            new EmployerSubscriptionPlan
            {
                Tier = EmployerSubscriptionTier.Pro,
                Duration = EmployerSubscriptionDuration.Yearly,
                Name = "Gói Pro (1 Năm)",
                Description = "Tăng thêm 10 tin đăng hoạt động, tặng 40 lượt đẩy tin, gợi ý ứng viên phù hợp cho từng công việc, tiết kiệm chi phí.",
                Price = BusinessRuleConstants.Employers.Subscriptions.Pro.YearlyPrice,
                DurationDays = BusinessRuleConstants.Employers.Subscriptions.YearlyDurationDays,
                ExtraActivePosts = BusinessRuleConstants.Employers.Subscriptions.Pro.ExtraActivePosts,
                FreeBumps = BusinessRuleConstants.Employers.Subscriptions.Pro.YearlyFreeBumps,
                CanRecommendCandidates = true,
                CanQueryCandidates = false,
                IsActive = true
            },
            new EmployerSubscriptionPlan
            {
                Tier = EmployerSubscriptionTier.Ultra,
                Duration = EmployerSubscriptionDuration.Monthly,
                Name = "Gói Ultra (1 Tháng)",
                Description = "Tăng thêm 20 tin đăng hoạt động, tặng 50 lượt đẩy tin, gợi ý ứng viên cho công việc và chủ động tìm kiếm ứng viên linh hoạt.",
                Price = BusinessRuleConstants.Employers.Subscriptions.Ultra.MonthlyPrice,
                DurationDays = BusinessRuleConstants.Employers.Subscriptions.MonthlyDurationDays,
                ExtraActivePosts = BusinessRuleConstants.Employers.Subscriptions.Ultra.ExtraActivePosts,
                FreeBumps = BusinessRuleConstants.Employers.Subscriptions.Ultra.MonthlyFreeBumps,
                CanRecommendCandidates = true,
                CanQueryCandidates = true,
                IsActive = true
            },
            new EmployerSubscriptionPlan
            {
                Tier = EmployerSubscriptionTier.Ultra,
                Duration = EmployerSubscriptionDuration.Yearly,
                Name = "Gói Ultra (1 Năm)",
                Description = "Tăng thêm 20 tin đăng hoạt động, tặng 100 lượt đẩy tin, gợi ý ứng viên cho công việc và chủ động tìm kiếm ứng viên linh hoạt, gói toàn diện nhất.",
                Price = BusinessRuleConstants.Employers.Subscriptions.Ultra.YearlyPrice,
                DurationDays = BusinessRuleConstants.Employers.Subscriptions.YearlyDurationDays,
                ExtraActivePosts = BusinessRuleConstants.Employers.Subscriptions.Ultra.ExtraActivePosts,
                FreeBumps = BusinessRuleConstants.Employers.Subscriptions.Ultra.YearlyFreeBumps,
                CanRecommendCandidates = true,
                CanQueryCandidates = true,
                IsActive = true
            }
        );

        await db.SaveChangesAsync();
        Console.WriteLine("  [+] Đã khởi tạo 6 gói đăng ký mẫu thành công.");
    }
}