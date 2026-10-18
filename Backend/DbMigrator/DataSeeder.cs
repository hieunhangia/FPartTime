using Domain.Shared.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Repository;
using Domain.Shared.Enums;
using Repository.Models.Users;

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
            await CreateUserIfNotExistsAsync($"000000{i}000", "SupportStaff@123", Role.SupportStaff);
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
}