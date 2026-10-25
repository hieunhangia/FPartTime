using Domain;
using Domain.Models.Orders;
using Domain.Models.Users;
using Domain.Shared.Constants;
using Domain.Shared.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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
        await SeedSqlFileIfEmptyAsync(() => db.EmployerSubscriptionPlans.AnyAsync(), "employer_subscription.sql");
        await SeedSqlFileIfEmptyAsync(() => db.Notifications.AnyAsync(), "notification.sql");
        await SeedSqlFileIfEmptyAsync(() => db.Jobs.AnyAsync(), "job.sql");
        await SeedEmployerRegistrationRequestsAsync();
        await SeedEmployerSubscriptionOrdersAndTransactionsAsync();

        Console.WriteLine("✅ Nạp dữ liệu hoàn tất thành công!");
        Console.WriteLine(
            """
            ========================================================================================
            📋 DANH SÁCH TÀI KHOẢN MẪU & CÁC TRƯỜNG HỢP KIỂM THỬ ĐÃ ĐƯỢC THIẾT LẬP:
            ----------------------------------------------------------------------------------------
            1. QUẢN TRỊ VIÊN HỆ THỐNG:
               • Admin:   0000000000 | Mật khẩu: Admin@123   (Lê Quản Trị - Toàn quyền quản trị)
               • Manager: 0123456789 | Mật khẩu: Manager@123 (Phạm Thị Thu Hà - Trưởng phòng vận hành)

            2. KIỂM DUYỆT VIÊN (CENSOR):
               • Censor 1: 0000000100 | Mật khẩu: Censor@123 (Trần Trung Kiên - Duyệt doanh nghiệp & báo cáo)
               • Censor 2: 0000000200 | Mật khẩu: Censor@123 (Nguyễn Mai Hương - Thẩm định nội dung)

            3. NHÀ TUYỂN DỤNG (EMPLOYER):
               • 0000000010 | Mật khẩu: Employer@123 | Highlands Coffee Hòa Lạc (Gói Ultra Active, 18 Bump)
               • 0000000020 | Mật khẩu: Employer@123 | Circle K Cầu Giấy (Gói Pro Active, 8 Bump, có đơn hết hạn giao dịch)
               • 0000000030 | Mật khẩu: Employer@123 | The IELTS Workshop (Gói Plus Active sắp hết hạn 5 ngày)
               • 0000000040 | Mật khẩu: Employer@123 | Golden Gate Ba Đình (Gói Pro ĐÃ HẾT HẠN - test cảnh báo)
               • 0000000050 | Mật khẩu: Employer@123 | Canifa Thanh Xuân (Tài khoản thường Free 5 tin tối đa)
               • 0000000060 | Mật khẩu: Employer@123 | Ánh Dương Event (Gói Plus ĐÃ HỦY - Cancelled)
               • 0000000070 | Mật khẩu: Employer@123 | Cozy Book Cafe Hoàn Kiếm (Tài khoản thường Free)

            4. ỨNG VIÊN (CANDIDATE):
               • 0000000001 | Mật khẩu: Candidate@123 | Nguyễn Hoàng Nam (SV ĐH FPT, Barista, Đang tìm việc)
               • 0000000002 | Mật khẩu: Candidate@123 | Trần Thị Thu Trang (SV ĐH Ngoại Thương, IELTS 7.5, Đang tìm việc)
               • 0000000003 | Mật khẩu: Candidate@123 | Lê Minh Đức (SV ĐH Bách Khoa, Tìm việc ĐÃ HẾT HẠN)
               • 0000000004 | Mật khẩu: Candidate@123 | Phạm Khánh Linh (SV Báo chí, CHƯA BẬT tìm việc)
               • 0000000005 | Mật khẩu: Candidate@123 | Đặng Văn Long (Kho/Giao hàng, Đang tìm việc)
               • 0000000006 | Mật khẩu: Candidate@123 | Ngô Thanh Trúc (Đăng ký NTD - ĐANG CHỜ DUYỆT Pending)
               • 0000000007 | Mật khẩu: Candidate@123 | Đỗ Quốc Cường (Đăng ký NTD - BỊ TỪ CHỐI Rejected)
               • 0000000008 | Mật khẩu: Candidate@123 | Vũ Thảo Nguyên (Đăng ký NTD - ĐÃ HỦY Cancelled)
            ========================================================================================
            """);
    }

    private async Task SeedRolesAsync()
    {
        Console.WriteLine("➡️ Đang kiểm tra và tạo Roles...");
        foreach (var roleName in Role.AllRoles)
        {
            var exists = await db.Roles.AnyAsync(r => r.Name == roleName);
            if (exists) continue;
            var role = new IdentityRole<Guid>
            {
                Id = Guid.NewGuid(),
                Name = roleName,
                NormalizedName = roleName.ToUpperInvariant(),
                ConcurrencyStamp = Guid.NewGuid().ToString()
            };
            db.Roles.Add(role);
        }

        await db.SaveChangesAsync();
    }

    private async Task SeedUsersAsync()
    {
        Console.WriteLine("➡️ Đang tạo dữ liệu người dùng thực tế và đa dạng...");

        // 1. Quản trị viên hệ thống (Admin & Manager)
        await CreateAdminUserAsync("0000000000", "Admin@123", "Lê Quản Trị", Role.AllRoles);
        await CreateManagerUserAsync("0123456789", "Manager@123", "Phạm Thị Thu Hà");

        // 2. Kiểm duyệt viên (Censor)
        await CreateCensorUserAsync("0000000100", "Censor@123");
        await CreateCensorUserAsync("0000000200", "Censor@123");

        // 3. Nhà tuyển dụng thực tế (Employers)
        await CreateEmployerUserAsync(
            phoneNumber: "0000000010",
            password: "Employer@123",
            companyName: "Highlands Coffee - Chi nhánh Khu CNC Hòa Lạc",
            taxCode: "010203040501",
            contactPhone: "0000000010",
            maxActivePosts: 25,
            bumpBalance: 18,
            description:
            "Chuỗi cà phê thương hiệu hàng đầu Việt Nam. Chi nhánh Khu Công Nghệ Cao Hòa Lạc chuyên phục vụ sinh viên ĐH FPT và kỹ sư công nghệ. Môi trường làm việc trẻ trung, năng động, hỗ trợ xoay ca linh hoạt theo lịch học.",
            logoFile: "highlands-logo.png");

        await CreateEmployerUserAsync(
            phoneNumber: "0000000020",
            password: "Employer@123",
            companyName: "Hệ Thống Cửa Hàng Tiện Lợi Circle K Cầu Giấy",
            taxCode: "030912456789",
            contactPhone: "0000000020",
            maxActivePosts: 15,
            bumpBalance: 8,
            description:
            "Circle K là chuỗi cửa hàng tiện lợi quốc tế mở cửa 24/7. Chúng tôi cung cấp môi trường làm việc chuyên nghiệp, cơ hội thăng tiến lên Trưởng ca và đãi ngộ rõ ràng cho sinh viên làm thêm.",
            logoFile: "circlek-logo.png");

        await CreateEmployerUserAsync(
            phoneNumber: "0000000030",
            password: "Employer@123",
            companyName: "Trung Tâm Ngoại Ngữ The IELTS Workshop Đống Đa",
            taxCode: "010876543210",
            contactPhone: "0000000030",
            maxActivePosts: 10,
            bumpBalance: 2,
            description:
            "Hệ thống đào tạo IELTS uy tín cho học sinh, sinh viên. Môi trường trẻ, chuyên nghiệp, hỗ trợ nâng cao trình độ tiếng Anh, thu nhập hấp dẫn cho trợ giảng và gia sư.",
            logoFile: "ielts-workshop-logo.png");

        await CreateEmployerUserAsync(
            phoneNumber: "0000000040",
            password: "Employer@123",
            companyName: "Hệ Thống Ẩm Thực Lẩu Nướng Golden Gate Ba Đình",
            taxCode: "010278945612",
            contactPhone: "0000000040",
            maxActivePosts: 5,
            bumpBalance: 0,
            description:
            "Tập đoàn ẩm thực sở hữu chuỗi nhà hàng K-Pub, Gogi House, Manwah. Tuyển dụng liên tục nhân viên phục vụ, phụ bếp với chế độ bao ăn ca và thưởng doanh số.",
            logoFile: "goldengate-logo.png");

        await CreateEmployerUserAsync(
            phoneNumber: "0000000050",
            password: "Employer@123",
            companyName: "Công Ty Cổ Phần Thời Trang Canifa Thanh Xuân",
            taxCode: "010145678912",
            contactPhone: "0000000050",
            maxActivePosts: 5,
            bumpBalance: 0,
            description:
            "Thương hiệu thời trang gia đình chất lượng cao hàng đầu Việt Nam. Cung cấp việc làm bán hàng thời vụ, kho đóng gói cho sinh viên năng động.",
            logoFile: "canifa-logo.png");

        await CreateEmployerUserAsync(
            phoneNumber: "0000000060",
            password: "Employer@123",
            companyName: "Công Ty TNHH Truyền Thông & Sự Kiện Ánh Dương Event",
            taxCode: "010998877665",
            contactPhone: "0000000060",
            maxActivePosts: 5,
            bumpBalance: 0,
            description:
            "Đơn vị tổ chức sự kiện chuyên nghiệp tại Hà Nội: tiệc cưới, hội nghị, triển lãm. Tuyển dụng PG, PB, lễ tân và kỹ thuật viên chạy show cuối tuần.",
            logoFile: "anhduong-event-logo.png");

        await CreateEmployerUserAsync(
            phoneNumber: "0000000070",
            password: "Employer@123",
            companyName: "Tiệm Trà & Sách Cozy Book Cafe Hoàn Kiếm",
            taxCode: "010654321987",
            contactPhone: "0000000070",
            maxActivePosts: 5,
            bumpBalance: 0,
            description:
            "Không gian cà phê sách yên tĩnh giữa lòng phố cổ Hà Nội. Tuyển dụng nhân viên thủ thư kiêm pha chế trà hoa quả nhẹ nhàng, lịch sự.",
            logoFile: "cozybook-logo.png");

        // 4. Ứng viên thực tế với tên tuổi, trường đại học, lịch rảnh đa dạng (Candidates)
        await CreateCandidateUserAsync(
            phoneNumber: "0000000001",
            password: "Candidate@123",
            fullName: "Nguyễn Hoàng Nam",
            gender: Gender.Male,
            dob: new DateOnly(2003, 8, 14),
            communeCode: "09988",
            bio:
            "Sinh viên năm 3 chuyên ngành Kỹ thuật Phần mềm ĐH FPT Hòa Lạc. Nhanh nhẹn, trung thực, có kinh nghiệm làm Barista 6 tháng tại quán cà phê acoustic. Mong muốn tìm việc part-time F&B hoặc trực kỹ thuật ca chiều/tối khu vực Thạch Thất - Hòa Lạc.",
            openForJobUntil: DateTime.UtcNow.AddDays(5),
            schedules:
            [
                new CandidateSchedule { DayOfWeek = DayOfWeek.Monday, Shift = Shift.Afternoon },
                new CandidateSchedule { DayOfWeek = DayOfWeek.Wednesday, Shift = Shift.Afternoon },
                new CandidateSchedule { DayOfWeek = DayOfWeek.Saturday, Shift = Shift.Morning },
                new CandidateSchedule { DayOfWeek = DayOfWeek.Saturday, Shift = Shift.Afternoon },
                new CandidateSchedule { DayOfWeek = DayOfWeek.Sunday, Shift = Shift.Morning }
            ]);

        await CreateCandidateUserAsync(
            phoneNumber: "0000000002",
            password: "Candidate@123",
            fullName: "Trần Thị Thu Trang",
            gender: Gender.Female,
            dob: new DateOnly(2004, 3, 22),
            communeCode: "00166",
            bio:
            "Sinh viên năm 2 ĐH Ngoại Thương Hà Nội, IELTS 7.5. Nhiệt tình, phát âm tiếng Anh chuẩn, có 1 năm kinh nghiệm làm gia sư tiếng Anh và trợ giảng trẻ em. Tìm việc trợ giảng trung tâm hoặc lễ tân khách sạn ca tối.",
            openForJobUntil: DateTime.UtcNow.AddDays(12),
            schedules:
            [
                new CandidateSchedule { DayOfWeek = DayOfWeek.Tuesday, Shift = Shift.Evening },
                new CandidateSchedule { DayOfWeek = DayOfWeek.Thursday, Shift = Shift.Evening },
                new CandidateSchedule { DayOfWeek = DayOfWeek.Saturday, Shift = Shift.Afternoon },
                new CandidateSchedule { DayOfWeek = DayOfWeek.Saturday, Shift = Shift.Evening },
                new CandidateSchedule { DayOfWeek = DayOfWeek.Sunday, Shift = Shift.Morning },
                new CandidateSchedule { DayOfWeek = DayOfWeek.Sunday, Shift = Shift.Afternoon }
            ]);

        await CreateCandidateUserAsync(
            phoneNumber: "0000000003",
            password: "Candidate@123",
            fullName: "Lê Minh Đức",
            gender: Gender.Male,
            dob: new DateOnly(2002, 11, 5),
            communeCode: "00256",
            bio:
            "Sinh viên năm cuối Viện CNTT ĐH Bách Khoa Hà Nội. Cần cù, gõ máy tính 75 WPM, thành thạo Excel và Word. Tìm việc trực chat fanpage, nhập liệu dữ liệu hoặc gia sư Toán THCS.",
            openForJobUntil: DateTime.UtcNow.AddDays(-2), // Test case: Đã hết hạn tìm việc
            schedules:
            [
                new CandidateSchedule { DayOfWeek = DayOfWeek.Tuesday, Shift = Shift.Morning },
                new CandidateSchedule { DayOfWeek = DayOfWeek.Thursday, Shift = Shift.Morning },
                new CandidateSchedule { DayOfWeek = DayOfWeek.Friday, Shift = Shift.Afternoon }
            ]);

        await CreateCandidateUserAsync(
            phoneNumber: "0000000004",
            password: "Candidate@123",
            fullName: "Phạm Khánh Linh",
            gender: Gender.Female,
            dob: new DateOnly(2005, 6, 18),
            communeCode: "00190",
            bio:
            "Sinh viên năm nhất HV Báo chí & Tuyên truyền. Ngoại hình sáng, giao tiếp tự tin, có gu thời trang. Thích các công việc tư vấn bán hàng quần áo, lễ tân sự kiện hoặc PG đón khách.",
            openForJobUntil: null, // Test case: Chưa bật tính năng tìm việc
            schedules:
            [
                new CandidateSchedule { DayOfWeek = DayOfWeek.Monday, Shift = Shift.Evening },
                new CandidateSchedule { DayOfWeek = DayOfWeek.Wednesday, Shift = Shift.Evening },
                new CandidateSchedule { DayOfWeek = DayOfWeek.Friday, Shift = Shift.Evening },
                new CandidateSchedule { DayOfWeek = DayOfWeek.Saturday, Shift = Shift.Afternoon },
                new CandidateSchedule { DayOfWeek = DayOfWeek.Saturday, Shift = Shift.Evening }
            ]);

        await CreateCandidateUserAsync(
            phoneNumber: "0000000005",
            password: "Candidate@123",
            fullName: "Đặng Văn Long",
            gender: Gender.Male,
            dob: new DateOnly(2001, 9, 30),
            communeCode: "00367",
            bio:
            "Có xe máy riêng, sức khỏe tốt, thông thuộc các tuyến đường nội thành Hà Nội. Có trách nhiệm cao trong công việc, tìm việc phụ kho, kiểm kê hàng hóa hoặc giao hàng bán thời gian.",
            openForJobUntil: DateTime.UtcNow.AddDays(4),
            schedules:
            [
                new CandidateSchedule { DayOfWeek = DayOfWeek.Monday, Shift = Shift.Morning },
                new CandidateSchedule { DayOfWeek = DayOfWeek.Tuesday, Shift = Shift.Morning },
                new CandidateSchedule { DayOfWeek = DayOfWeek.Wednesday, Shift = Shift.Morning },
                new CandidateSchedule { DayOfWeek = DayOfWeek.Thursday, Shift = Shift.Morning },
                new CandidateSchedule { DayOfWeek = DayOfWeek.Friday, Shift = Shift.Morning }
            ]);

        await CreateCandidateUserAsync(
            phoneNumber: "0000000008",
            password: "Candidate@123",
            fullName: "Vũ Thảo Nguyên",
            gender: Gender.Female,
            dob: new DateOnly(2003, 12, 10),
            communeCode: "00229",
            bio: "Sinh viên ĐH Kinh tế Quốc dân. Yêu thích kinh doanh và các sản phẩm handmade thủ công.",
            openForJobUntil: null,
            schedules: []);

        await CreateCandidateUserAsync(
            phoneNumber: "0000000007",
            password: "Candidate@123",
            fullName: "Đỗ Quốc Cường",
            gender: Gender.Male,
            dob: new DateOnly(2000, 4, 15),
            communeCode: "00070",
            bio: "Kinh doanh tự do các mặt hàng tài chính và dịch vụ số tại Hà Nội.",
            openForJobUntil: null,
            schedules: []);

        await CreateCandidateUserAsync(
            phoneNumber: "0000000006",
            password: "Candidate@123",
            fullName: "Ngô Thanh Trúc",
            gender: Gender.Female,
            dob: new DateOnly(2002, 7, 28),
            communeCode: "00004",
            bio:
            "Sinh viên năm cuối ĐH Mỹ thuật Công nghiệp Hà Nội. Đam mê thiết kế đồ họa, sáng tạo nội dung số và vẽ tranh nghệ thuật.",
            openForJobUntil: DateTime.UtcNow.AddDays(7),
            schedules:
            [
                new() { DayOfWeek = DayOfWeek.Saturday, Shift = Shift.Morning },
                new() { DayOfWeek = DayOfWeek.Saturday, Shift = Shift.Afternoon }
            ]);

        await db.SaveChangesAsync();
    }

    private async Task<User> EnsureBaseUserExistsAsync(string phoneNumber, string password, params string[] roleNames)
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

        return user;
    }

    private async Task CreateAdminUserAsync(string phoneNumber, string password, string fullName,
        params string[] roleNames)
    {
        var user = await EnsureBaseUserExistsAsync(phoneNumber, password, roleNames);

        if (!await db.Candidates.AnyAsync(c => c.UserId == user.Id))
        {
            db.Candidates.Add(new CandidateProfile
            {
                UserId = user.Id,
                AvatarFilePath = BusinessRuleConstants.DefaultAvatarFilePath,
                Bio = "Tài khoản quản trị viên tối cao hệ thống FPartTime.",
                FullName = fullName,
                DateOfBirth = new DateOnly(1990, 5, 15),
                Gender = Gender.Male,
                PreferredWorkCommuneCode = "00004"
            });
        }

        if (!await db.Employers.AnyAsync(e => e.UserId == user.Id))
        {
            db.Employers.Add(new EmployerProfile
            {
                UserId = user.Id,
                LogoFilePath = BusinessRuleConstants.DefaultEmployerLogoFilePath,
                CompanyName = "Tổng Công Ty Công Nghệ F-PartTime System",
                MaxActivePosts = 100,
                BumpBalance = 100,
                TaxCode = "010000000000",
                VerificationDocumentFilePath =
                    $"{BusinessRuleConstants.ObjectStorage.EmployerRegistration.BasePath}/{BusinessRuleConstants.ObjectStorage.EmployerRegistration.Document}/admin.pdf",
                ContactPhone = user.PhoneNumber,
                Description = "Đơn vị quản lý và vận hành nền tảng việc làm bán thời gian FPartTime."
            });
        }

        if (!await db.Censors.AnyAsync(c => c.UserId == user.Id))
        {
            db.Censors.Add(new CensorProfile { UserId = user.Id });
        }

        await db.SaveChangesAsync();
    }

    private async Task CreateManagerUserAsync(string phoneNumber, string password, string fullName)
    {
        var user = await EnsureBaseUserExistsAsync(phoneNumber, password, Role.Manager);

        if (!await db.Candidates.AnyAsync(c => c.UserId == user.Id))
        {
            db.Candidates.Add(new CandidateProfile
            {
                UserId = user.Id,
                AvatarFilePath = BusinessRuleConstants.DefaultAvatarFilePath,
                Bio = "Trưởng phòng vận hành nghiệp vụ FPartTime.",
                FullName = fullName,
                DateOfBirth = new DateOnly(1992, 8, 20),
                Gender = Gender.Female,
                PreferredWorkCommuneCode = "00166"
            });
        }

        await db.SaveChangesAsync();
    }

    private async Task CreateCensorUserAsync(string phoneNumber, string password)
    {
        var user = await EnsureBaseUserExistsAsync(phoneNumber, password, Role.Censor);

        if (!await db.Censors.AnyAsync(c => c.UserId == user.Id))
        {
            db.Censors.Add(new CensorProfile { UserId = user.Id });
        }

        await db.SaveChangesAsync();
    }

    private async Task CreateEmployerUserAsync(
        string phoneNumber,
        string password,
        string companyName,
        string taxCode,
        string contactPhone,
        int maxActivePosts,
        int bumpBalance,
        string description,
        string logoFile)
    {
        var user = await EnsureBaseUserExistsAsync(phoneNumber, password, Role.Employer);

        if (!await db.Employers.AnyAsync(e => e.UserId == user.Id))
        {
            db.Employers.Add(new EmployerProfile
            {
                UserId = user.Id,
                LogoFilePath = logoFile,
                CompanyName = companyName,
                MaxActivePosts = maxActivePosts,
                BumpBalance = bumpBalance,
                TaxCode = taxCode,
                VerificationDocumentFilePath =
                    $"{BusinessRuleConstants.ObjectStorage.EmployerRegistration.BasePath}/{BusinessRuleConstants.ObjectStorage.EmployerRegistration.Document}/{user.UserName}.pdf",
                ContactPhone = contactPhone,
                Description = description
            });
        }

        await db.SaveChangesAsync();
    }

    private async Task CreateCandidateUserAsync(
        string phoneNumber,
        string password,
        string fullName,
        Gender gender,
        DateOnly dob,
        string communeCode,
        string bio,
        DateTime? openForJobUntil,
        List<CandidateSchedule> schedules)
    {
        var user = await EnsureBaseUserExistsAsync(phoneNumber, password, Role.Candidate);

        if (!await db.Candidates.AnyAsync(c => c.UserId == user.Id))
        {
            var profile = new CandidateProfile
            {
                UserId = user.Id,
                AvatarFilePath = BusinessRuleConstants.DefaultAvatarFilePath,
                Bio = bio,
                FullName = fullName,
                DateOfBirth = dob,
                Gender = gender,
                PreferredWorkCommuneCode = communeCode,
                OpenForJobUntil = openForJobUntil,
                PreferredWorkSchedules = schedules
            };

            db.Candidates.Add(profile);
        }

        await db.SaveChangesAsync();
    }

    private async Task SeedEmployerRegistrationRequestsAsync()
    {
        if (await db.EmployerRegistrationRequests.AnyAsync())
        {
            Console.WriteLine("ℹ️ Không cần nạp EmployerRegistrationRequests vì đã có dữ liệu.");
            return;
        }

        Console.WriteLine("➡️ Đang nạp yêu cầu đăng ký nhà tuyển dụng cho Censor kiểm duyệt...");

        var candTruc = await db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == "0000000006");
        var candCuong = await db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == "0000000007");
        var candNguyen = await db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == "0000000008");
        var empHighlands = await db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == "0000000010");
        var censorKien = await db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == "0000000100");

        if (candTruc != null)
        {
            // Case 1: Pending (Đang chờ Censor duyệt)
            db.EmployerRegistrationRequests.Add(new EmployerRegistrationRequest
            {
                Id = Guid.NewGuid(),
                RequesterId = candTruc.Id,
                CompanyName = "Cửa Hàng Thiết Kế Đồ Họa & Tranh Nghệ Thuật Trúc Art",
                TaxCode = "010998822331",
                FullName = "Ngô Thanh Trúc",
                DateOfBirth = new DateOnly(2002, 7, 28),
                PhoneNumber = "0000000006",
                CCCDNumber = "001202019876",
                DocumentPath =
                    $"{BusinessRuleConstants.ObjectStorage.EmployerRegistration.BasePath}/{BusinessRuleConstants.ObjectStorage.EmployerRegistration.Document}/0000000006_gpkd.pdf",
                CCCDFrontImagePath =
                    $"{BusinessRuleConstants.ObjectStorage.EmployerRegistration.BasePath}/{BusinessRuleConstants.ObjectStorage.EmployerRegistration.CccdFront}/0000000006_front.jpg",
                CCCDBackImagePath =
                    $"{BusinessRuleConstants.ObjectStorage.EmployerRegistration.BasePath}/{BusinessRuleConstants.ObjectStorage.EmployerRegistration.CccdBack}/0000000006_back.jpg",
                Status = EmployerRegistrationRequestStatus.Pending,
                CreatedAt = DateTime.UtcNow.AddHours(-18),
                ProcessorId = null,
                ProcessedAt = null,
                RejectReason = null
            });
        }

        if (empHighlands != null && censorKien != null)
        {
            // Case 2: Approved (Đã được Censor duyệt thành công trong quá khứ)
            db.EmployerRegistrationRequests.Add(new EmployerRegistrationRequest
            {
                Id = Guid.NewGuid(),
                RequesterId = empHighlands.Id,
                CompanyName = "Highlands Coffee - Chi nhánh Khu CNC Hòa Lạc",
                TaxCode = "010203040501",
                FullName = "Hoàng Tuấn Anh",
                DateOfBirth = new DateOnly(1995, 10, 12),
                PhoneNumber = "0000000010",
                CCCDNumber = "001095001234",
                DocumentPath =
                    $"{BusinessRuleConstants.ObjectStorage.EmployerRegistration.BasePath}/{BusinessRuleConstants.ObjectStorage.EmployerRegistration.Document}/0000000010_gpkd.pdf",
                CCCDFrontImagePath =
                    $"{BusinessRuleConstants.ObjectStorage.EmployerRegistration.BasePath}/{BusinessRuleConstants.ObjectStorage.EmployerRegistration.CccdFront}/0000000010_front.jpg",
                CCCDBackImagePath =
                    $"{BusinessRuleConstants.ObjectStorage.EmployerRegistration.BasePath}/{BusinessRuleConstants.ObjectStorage.EmployerRegistration.CccdBack}/0000000010_back.jpg",
                Status = EmployerRegistrationRequestStatus.Approved,
                CreatedAt = DateTime.UtcNow.AddDays(-35),
                ProcessedAt = DateTime.UtcNow.AddDays(-34),
                ProcessorId = censorKien.Id,
                RejectReason = null
            });
        }

        if (candCuong != null && censorKien != null)
        {
            // Case 3: Rejected (Bị Censor từ chối kèm lý do xác thực)
            db.EmployerRegistrationRequests.Add(new EmployerRegistrationRequest
            {
                Id = Guid.NewGuid(),
                RequesterId = candCuong.Id,
                CompanyName = "Công Ty Cổ Phần Đầu Tư Tài Chính Quốc Tế Á Châu",
                TaxCode = "030999888777",
                FullName = "Đỗ Quốc Cường",
                DateOfBirth = new DateOnly(2000, 4, 15),
                PhoneNumber = "0000000007",
                CCCDNumber = "001200005432",
                DocumentPath =
                    $"{BusinessRuleConstants.ObjectStorage.EmployerRegistration.BasePath}/{BusinessRuleConstants.ObjectStorage.EmployerRegistration.Document}/0000000007_gpkd.pdf",
                CCCDFrontImagePath =
                    $"{BusinessRuleConstants.ObjectStorage.EmployerRegistration.BasePath}/{BusinessRuleConstants.ObjectStorage.EmployerRegistration.CccdFront}/0000000007_front.jpg",
                CCCDBackImagePath =
                    $"{BusinessRuleConstants.ObjectStorage.EmployerRegistration.BasePath}/{BusinessRuleConstants.ObjectStorage.EmployerRegistration.CccdBack}/0000000007_back.jpg",
                Status = EmployerRegistrationRequestStatus.Rejected,
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                ProcessedAt = DateTime.UtcNow.AddDays(-2),
                ProcessorId = censorKien.Id,
                RejectReason =
                    "Mã số thuế không tồn tại trên Cổng thông tin Quốc gia về đăng ký doanh nghiệp; hình chụp CCCD mặt sau bị mờ lóa sáng, không đọc được số seri và mã vạch bảo mật."
            });
        }

        if (candNguyen != null)
        {
            // Case 4: Cancelled (Ứng viên tự hủy yêu cầu)
            db.EmployerRegistrationRequests.Add(new EmployerRegistrationRequest
            {
                Id = Guid.NewGuid(),
                RequesterId = candNguyen.Id,
                CompanyName = "Tiệm Hoa Tươi & Phụ Kiện Handmade Min Store",
                TaxCode = "010887766554",
                FullName = "Vũ Thảo Nguyên",
                DateOfBirth = new DateOnly(2003, 12, 10),
                PhoneNumber = "0000000008",
                CCCDNumber = "001203009988",
                DocumentPath =
                    $"{BusinessRuleConstants.ObjectStorage.EmployerRegistration.BasePath}/{BusinessRuleConstants.ObjectStorage.EmployerRegistration.Document}/0000000008_gpkd.pdf",
                CCCDFrontImagePath =
                    $"{BusinessRuleConstants.ObjectStorage.EmployerRegistration.BasePath}/{BusinessRuleConstants.ObjectStorage.EmployerRegistration.CccdFront}/0000000008_front.jpg",
                CCCDBackImagePath =
                    $"{BusinessRuleConstants.ObjectStorage.EmployerRegistration.BasePath}/{BusinessRuleConstants.ObjectStorage.EmployerRegistration.CccdBack}/0000000008_back.jpg",
                Status = EmployerRegistrationRequestStatus.Cancelled,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                ProcessorId = null,
                ProcessedAt = null,
                RejectReason = null
            });
        }

        await db.SaveChangesAsync();
        Console.WriteLine("  [+] Đã tạo 4 yêu cầu đăng ký nhà tuyển dụng (Pending, Approved, Rejected, Cancelled).");
    }

    private async Task SeedEmployerSubscriptionOrdersAndTransactionsAsync()
    {
        if (await db.Orders.AnyAsync())
        {
            Console.WriteLine("ℹ️ Không cần nạp Orders và PaymentTransactions vì đã có dữ liệu.");
            return;
        }

        Console.WriteLine("➡️ Đang nạp đơn hàng và giao dịch thanh toán (Orders & PaymentTransactions)...");

        var empHighlands = await db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == "0000000010");
        var empCircleK = await db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == "0000000020");
        var empIelts = await db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == "0000000030");
        var empGoldenGate = await db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == "0000000040");

        if (empHighlands != null)
        {
            // Case 1: Đơn hàng ĐÃ HOÀN THÀNH (Completed) kèm Transaction ĐÃ THANH TOÁN (Paid qua PayOS)
            var order1 = new EmployerSubscriptionOrder
            {
                Id = Guid.Parse("a1111111-0000-0000-0000-000000000001"),
                UserId = empHighlands.Id,
                Tier = EmployerSubscriptionTier.Ultra,
                Duration = EmployerSubscriptionDuration.Monthly,
                PlanName = "Gói Ultra (1 Tháng)",
                TotalAmount = 200000.00m,
                Description = "Thanh toán gói dịch vụ Ultra (1 Tháng) cho Highlands Coffee Hòa Lạc",
                Status = OrderStatus.Completed,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                ExpiredAt = DateTime.UtcNow.AddDays(-5).AddMinutes(15),
                CompletedAt = DateTime.UtcNow.AddDays(-5).AddMinutes(3),
                DurationDays = 30,
                ExtraActivePosts = 20,
                FreeBumps = 50,
                CanRecommendCandidates = true,
                CanQueryCandidates = true
            };
            db.EmployerSubscriptionOrders.Add(order1);

            db.PaymentTransactions.Add(new PaymentTransaction
            {
                Id = Guid.Parse("b1111111-0000-0000-0000-000000000001"),
                OrderId = order1.Id,
                UserId = empHighlands.Id,
                Provider = PaymentProvider.PayOS,
                PaymentReferenceId = "PAYOS-ORD-20261020-001",
                PaymentUrl = "https://pay.payos.vn/web/demo-paid-order-001",
                Amount = 200000.00m,
                Status = PaymentTransactionStatus.Paid,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                ExpiredAt = DateTime.UtcNow.AddDays(-5).AddMinutes(15),
                PaidAt = DateTime.UtcNow.AddDays(-5).AddMinutes(3)
            });

            // Case 2: Đơn hàng ĐANG CHỜ THANH TOÁN (PendingPayment) - Transaction Pending (chưa quá 15 phút, CanCreatePaymentTransaction = false)
            var order2 = new EmployerSubscriptionOrder
            {
                Id = Guid.Parse("a1111111-0000-0000-0000-000000000002"),
                UserId = empHighlands.Id,
                Tier = EmployerSubscriptionTier.Ultra,
                Duration = EmployerSubscriptionDuration.Yearly,
                PlanName = "Gói Ultra (1 Năm)",
                TotalAmount = 1000000.00m,
                Description = "Nâng cấp gói dịch vụ Ultra (1 Năm) gia hạn ưu đãi Highlands Coffee",
                Status = OrderStatus.PendingPayment,
                CreatedAt = DateTime.UtcNow.AddMinutes(-5),
                ExpiredAt = DateTime.UtcNow.AddMinutes(10),
                CompletedAt = null,
                DurationDays = 365,
                ExtraActivePosts = 20,
                FreeBumps = 100,
                CanRecommendCandidates = true,
                CanQueryCandidates = true
            };
            db.EmployerSubscriptionOrders.Add(order2);

            db.PaymentTransactions.Add(new PaymentTransaction
            {
                Id = Guid.Parse("b1111111-0000-0000-0000-000000000002"),
                OrderId = order2.Id,
                UserId = empHighlands.Id,
                Provider = PaymentProvider.VNPAY,
                PaymentReferenceId = "VNPAY-ORD-20261025-002",
                PaymentUrl = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?order=ORD-20261025-002",
                Amount = 1000000.00m,
                Status = PaymentTransactionStatus.Pending,
                CreatedAt = DateTime.UtcNow.AddMinutes(-5),
                ExpiredAt = DateTime.UtcNow.AddMinutes(10),
                PaidAt = null
            });
        }

        if (empCircleK != null)
        {
            // Case 3: Đơn hàng ĐANG CHỜ THANH TOÁN (PendingPayment) NHƯNG Transaction ĐÃ HẾT HẠN (Expired sau 15p)
            // (Case cực kỳ quan trọng để test nút "Tạo giao dịch thanh toán mới" trong Detail.cshtml.cs: CanCreatePaymentTransaction = true)
            var order3 = new EmployerSubscriptionOrder
            {
                Id = Guid.Parse("a1111111-0000-0000-0000-000000000003"),
                UserId = empCircleK.Id,
                Tier = EmployerSubscriptionTier.Pro,
                Duration = EmployerSubscriptionDuration.Monthly,
                PlanName = "Gói Pro (1 Tháng)",
                TotalAmount = 100000.00m,
                Description = "Gia hạn gói dịch vụ Pro (1 Tháng) Circle K Cầu Giấy",
                Status = OrderStatus.PendingPayment,
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                ExpiredAt = DateTime.UtcNow.AddHours(23),
                CompletedAt = null,
                DurationDays = 30,
                ExtraActivePosts = 10,
                FreeBumps = 10,
                CanRecommendCandidates = true,
                CanQueryCandidates = false
            };
            db.EmployerSubscriptionOrders.Add(order3);

            db.PaymentTransactions.Add(new PaymentTransaction
            {
                Id = Guid.Parse("b1111111-0000-0000-0000-000000000003"),
                OrderId = order3.Id,
                UserId = empCircleK.Id,
                Provider = PaymentProvider.PayOS,
                PaymentReferenceId = "PAYOS-ORD-20261025-003",
                PaymentUrl = "https://pay.payos.vn/web/demo-expired-order-003",
                Amount = 100000.00m,
                Status = PaymentTransactionStatus.Expired,
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                ExpiredAt = DateTime.UtcNow.AddHours(-1).AddMinutes(15),
                PaidAt = null
            });
        }

        if (empIelts != null)
        {
            // Case 4: Đơn hàng ĐÃ HỦY (Cancelled)
            var order4 = new EmployerSubscriptionOrder
            {
                Id = Guid.Parse("a1111111-0000-0000-0000-000000000004"),
                UserId = empIelts.Id,
                Tier = EmployerSubscriptionTier.Plus,
                Duration = EmployerSubscriptionDuration.Monthly,
                PlanName = "Gói Plus (1 Tháng)",
                TotalAmount = 30000.00m,
                Description = "Đơn hàng gói Plus hủy theo yêu cầu của nhà tuyển dụng",
                Status = OrderStatus.Cancelled,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                ExpiredAt = DateTime.UtcNow.AddDays(-2).AddMinutes(15),
                CompletedAt = null,
                DurationDays = 30,
                ExtraActivePosts = 5,
                FreeBumps = 5,
                CanRecommendCandidates = false,
                CanQueryCandidates = false
            };
            db.EmployerSubscriptionOrders.Add(order4);

            db.PaymentTransactions.Add(new PaymentTransaction
            {
                Id = Guid.Parse("b1111111-0000-0000-0000-000000000004"),
                OrderId = order4.Id,
                UserId = empIelts.Id,
                Provider = PaymentProvider.ZaloPay,
                PaymentReferenceId = "ZALOPAY-ORD-20261023-004",
                PaymentUrl = "https://qcgateway.zalopay.vn/pay?order=ORD-20261023-004",
                Amount = 30000.00m,
                Status = PaymentTransactionStatus.Cancelled,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                ExpiredAt = DateTime.UtcNow.AddDays(-2).AddMinutes(15),
                PaidAt = null
            });
        }

        if (empGoldenGate != null)
        {
            // Case 5: Đơn hàng QUÁ HẠN THANH TOÁN (Expired)
            var order5 = new EmployerSubscriptionOrder
            {
                Id = Guid.Parse("a1111111-0000-0000-0000-000000000005"),
                UserId = empGoldenGate.Id,
                Tier = EmployerSubscriptionTier.Pro,
                Duration = EmployerSubscriptionDuration.Monthly,
                PlanName = "Gói Pro (1 Tháng)",
                TotalAmount = 100000.00m,
                Description = "Đơn hàng quá hạn thanh toán do không hoàn tất giao dịch",
                Status = OrderStatus.Expired,
                CreatedAt = DateTime.UtcNow.AddDays(-7),
                ExpiredAt = DateTime.UtcNow.AddDays(-6),
                CompletedAt = null,
                DurationDays = 30,
                ExtraActivePosts = 10,
                FreeBumps = 10,
                CanRecommendCandidates = true,
                CanQueryCandidates = false
            };
            db.EmployerSubscriptionOrders.Add(order5);

            db.PaymentTransactions.Add(new PaymentTransaction
            {
                Id = Guid.Parse("b1111111-0000-0000-0000-000000000005"),
                OrderId = order5.Id,
                UserId = empGoldenGate.Id,
                Provider = PaymentProvider.PayOS,
                PaymentReferenceId = "PAYOS-ORD-20261018-005",
                PaymentUrl = "https://pay.payos.vn/web/demo-expired-order-005",
                Amount = 100000.00m,
                Status = PaymentTransactionStatus.Expired,
                CreatedAt = DateTime.UtcNow.AddDays(-7),
                ExpiredAt = DateTime.UtcNow.AddDays(-7).AddMinutes(15),
                PaidAt = null
            });
        }

        await db.SaveChangesAsync();
        Console.WriteLine(
            "  [+] Đã tạo 5 đơn hàng mẫu (Completed, PendingPayment có trans Pending, PendingPayment có trans Expired, Cancelled, Expired) cùng các giao dịch thanh toán.");
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