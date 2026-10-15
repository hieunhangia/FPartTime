using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Repository;
using Repository.Constants;
using Repository.Models.Jobs;
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
        await SeedJobsAsync();

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
                    new()
                    {
                        DayOfWeek = DayOfWeek.Monday, StartTime = new TimeOnly(9, 0, 0),
                        EndTime = new TimeOnly(17, 0, 0)
                    },
                    new()
                    {
                        DayOfWeek = DayOfWeek.Tuesday, StartTime = new TimeOnly(9, 0, 0),
                        EndTime = new TimeOnly(17, 0, 0)
                    },
                    new()
                    {
                        DayOfWeek = DayOfWeek.Wednesday, StartTime = new TimeOnly(9, 0, 0),
                        EndTime = new TimeOnly(17, 0, 0)
                    },
                    new()
                    {
                        DayOfWeek = DayOfWeek.Thursday, StartTime = new TimeOnly(9, 0, 0),
                        EndTime = new TimeOnly(17, 0, 0)
                    },
                    new()
                    {
                        DayOfWeek = DayOfWeek.Friday, StartTime = new TimeOnly(9, 0, 0),
                        EndTime = new TimeOnly(17, 0, 0)
                    }
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

    private async Task SeedJobsAsync()
    {
        Console.WriteLine("➡️ Đang kiểm tra và tạo dữ liệu việc làm mẫu (Jobs)...");

        if (await db.Jobs.AnyAsync())
        {
            Console.WriteLine("ℹ️ Đã có dữ liệu Jobs trong cơ sở dữ liệu, bỏ qua bước seed việc làm.");
            return;
        }

        // 1. Kiểm tra hoặc tạo các ngành nghề (Industry)
        var fnbIndustry = await db.Industries.FirstOrDefaultAsync(i => i.Name == "F&B / Ẩm thực");
        if (fnbIndustry == null)
        {
            fnbIndustry = new Industry { Id = Guid.NewGuid(), Name = "F&B / Ẩm thực" };
            db.Industries.Add(fnbIndustry);
        }

        var retailIndustry = await db.Industries.FirstOrDefaultAsync(i => i.Name == "Bán lẻ / Cửa hàng");
        if (retailIndustry == null)
        {
            retailIndustry = new Industry { Id = Guid.NewGuid(), Name = "Bán lẻ / Cửa hàng" };
            db.Industries.Add(retailIndustry);
        }

        await db.SaveChangesAsync();

        // 2. Lấy danh sách nhà tuyển dụng (Employer)
        var employers = await db.Employers.Take(2).ToListAsync();
        if (employers.Count == 0)
        {
            Console.WriteLine("⚠️ Không tìm thấy Employer nào để tạo việc làm.");
            return;
        }

        var employer1 = employers[0];
        employer1.CompanyName = "The Coffee House";
        employer1.ContactPhone = "0901234567";
        employer1.Description =
            "Chuỗi cà phê truyền cảm hứng tại Việt Nam với không gian làm việc thân thiện, năng động dành cho các bạn trẻ.";

        var employer2 = employers.Count > 1 ? employers[1] : employer1;
        if (employers.Count > 1)
        {
            employer2.CompanyName = "Circle K Việt Nam";
            employer2.ContactPhone = "0987654321";
            employer2.Description =
                "Hệ thống chuỗi cửa hàng tiện lợi phục vụ 24/7 với môi trường làm việc chuyên nghiệp, ca làm linh hoạt.";
        }

        await db.SaveChangesAsync();

        // 3. Lấy thông tin xã/phường (Commune)
        var commune1 = await db.Communes.FirstOrDefaultAsync(c => c.Code == "00166")
                       ?? await db.Communes.FirstOrDefaultAsync();

        var commune2 = await db.Communes.FirstOrDefaultAsync(c => c.Code == "00235")
                       ?? await db.Communes.FirstOrDefaultAsync();

        if (commune1 == null || commune2 == null)
        {
            Console.WriteLine("⚠️ Không tìm thấy thông tin xã/phường (Communes) trong cơ sở dữ liệu.");
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // 4. Tạo Job 1: Nhân viên phục vụ & Pha chế Part-time
        var job1 = new Job
        {
            Id = Guid.NewGuid(),
            Title = "Nhân viên phục vụ & Pha chế Part-time",
            Description = """
                          The Coffee House tuyển dụng nhân viên phục vụ & phụ pha chế làm việc bán thời gian tại chi nhánh Cầu Giấy.

                          Mô tả công việc:
                          - Chào đón khách hàng, nhận order đồ uống và bánh ngọt tại quầy.
                          - Phụ pha chế các loại trà, cà phê theo công thức chuẩn của quán.
                          - Dọn dẹp bàn ghế, giữ gìn vệ sinh khu vực quầy bar và không gian quán.
                          - Hỗ trợ tính tiền và thanh toán nhanh chóng cho khách hàng.

                          Yêu cầu ứng viên:
                          - Nhanh nhẹn, thân thiện, trung thực, có tinh thần trách nhiệm.
                          - Ưu tiên học sinh, sinh viên cần tìm kiếm công việc làm thêm linh hoạt.
                          - Không yêu cầu kinh nghiệm (được hướng dẫn và đào tạo từ đầu).

                          Quyền lợi:
                          - Mức lương cạnh tranh từ 25.000đ - 30.000đ/giờ.
                          - Giảm giá 50% đồ uống khi mua tại hệ thống cửa hàng.
                          - Đăng ký ca làm việc linh hoạt theo tuần, thuận tiện sắp xếp lịch học.
                          """,
            VacancyCount = 5,
            MinAgeRequirement = 18,
            MaxAgeRequirement = 25,
            GenderRequirement = null,
            CommuneCode = commune1.Code,
            DetailAddress = "Số 122 Cầu Giấy",
            ExpiryAt = today.AddDays(30),
            Status = JobStatus.Active,
            CreatedAt = DateTime.UtcNow,
            EmployerId = employer1.UserId,
            IndustryId = fnbIndustry.Id,
            JobSalaries =
            [
                new JobSalary
                {
                    Id = Guid.NewGuid(),
                    Amount = 25000m,
                    TimeQuantity = 1,
                    TimeUnit = JobSalaryTimeUnit.Hour
                },
                new JobSalary
                {
                    Id = Guid.NewGuid(),
                    Amount = 120000m,
                    TimeQuantity = 1,
                    TimeUnit = JobSalaryTimeUnit.Session
                }
            ],
            JobSchedules =
            [
                new JobSchedule
                {
                    Id = Guid.NewGuid(),
                    DayOfWeek = DayOfWeek.Monday,
                    StartTime = new TimeOnly(8, 0),
                    EndTime = new TimeOnly(12, 0)
                },
                new JobSchedule
                {
                    Id = Guid.NewGuid(),
                    DayOfWeek = DayOfWeek.Wednesday,
                    StartTime = new TimeOnly(13, 0),
                    EndTime = new TimeOnly(17, 0)
                },
                new JobSchedule
                {
                    Id = Guid.NewGuid(),
                    DayOfWeek = DayOfWeek.Friday,
                    StartTime = new TimeOnly(18, 0),
                    EndTime = new TimeOnly(22, 0)
                },
                new JobSchedule
                {
                    Id = Guid.NewGuid(),
                    DayOfWeek = DayOfWeek.Saturday,
                    StartTime = new TimeOnly(8, 0),
                    EndTime = new TimeOnly(12, 0)
                }
            ]
        };

        // 5. Tạo Job 2: Nhân viên bán hàng & Thu ngân Part-time Circle K
        var job2 = new Job
        {
            Id = Guid.NewGuid(),
            Title = "Nhân viên bán hàng & Thu ngân Part-time Circle K",
            Description = """
                          Circle K tuyển dụng nhân viên bán hàng kiêm thu ngân ca linh hoạt tại khu vực Đống Đa, Hà Nội.

                          Mô tả công việc:
                          - Thanh toán tiền mặt, thẻ, ví điện tử cho khách hàng chính xác, nhanh nhẹn.
                          - Trưng bày và sắp xếp hàng hóa lên quầy kệ theo đúng tiêu chuẩn.
                          - Kiểm tra hạn sử dụng và bổ sung hàng hóa đầy đủ.
                          - Tư vấn các chương trình khuyến mãi cho khách hàng khi mua sắm.

                          Yêu cầu ứng viên:
                          - Độ tuổi từ 18 tuổi trở lên, có phương tiện đi lại chủ động.
                          - Thật thà, hòa đồng, vui vẻ với khách hàng.
                          - Đăng ký tối thiểu 4 ca/tuần (mỗi ca từ 4 - 6 tiếng).

                          Quyền lợi:
                          - Lương từ 28.000đ - 35.000đ/giờ + phụ cấp ca đêm.
                          - Làm việc trong môi trường máy lạnh sạch sẽ, an toàn.
                          - Cơ hội thăng tiến lên Trưởng ca hoặc Quản lý cửa hàng.
                          """,
            VacancyCount = 3,
            MinAgeRequirement = 18,
            MaxAgeRequirement = 30,
            GenderRequirement = null,
            CommuneCode = commune2.Code,
            DetailAddress = "Số 45 Chùa Bộc",
            ExpiryAt = today.AddDays(45),
            Status = JobStatus.Active,
            CreatedAt = DateTime.UtcNow,
            EmployerId = employer2.UserId,
            IndustryId = retailIndustry.Id,
            JobSalaries =
            [
                new JobSalary
                {
                    Id = Guid.NewGuid(),
                    Amount = 28000m,
                    TimeQuantity = 1,
                    TimeUnit = JobSalaryTimeUnit.Hour
                },
                new JobSalary
                {
                    Id = Guid.NewGuid(),
                    Amount = 150000m,
                    TimeQuantity = 1,
                    TimeUnit = JobSalaryTimeUnit.Session
                }
            ],
            JobSchedules =
            [
                new JobSchedule
                {
                    Id = Guid.NewGuid(),
                    DayOfWeek = DayOfWeek.Tuesday,
                    StartTime = new TimeOnly(8, 0),
                    EndTime = new TimeOnly(13, 0)
                },
                new JobSchedule
                {
                    Id = Guid.NewGuid(),
                    DayOfWeek = DayOfWeek.Thursday,
                    StartTime = new TimeOnly(13, 0),
                    EndTime = new TimeOnly(18, 0)
                },
                new JobSchedule
                {
                    Id = Guid.NewGuid(),
                    DayOfWeek = DayOfWeek.Saturday,
                    StartTime = new TimeOnly(18, 0),
                    EndTime = new TimeOnly(23, 0)
                },
                new JobSchedule
                {
                    Id = Guid.NewGuid(),
                    DayOfWeek = DayOfWeek.Sunday,
                    StartTime = new TimeOnly(8, 0),
                    EndTime = new TimeOnly(13, 0)
                }
            ]
        };

        db.Jobs.AddRange(job1, job2);
        await db.SaveChangesAsync();

        Console.WriteLine("  [+] Đã tạo thành công 2 việc làm mẫu (Jobs).");
    }
}