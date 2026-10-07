using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Repository;
using Repository.Constants;
using Repository.Models.Users;

namespace DbMigrator;

public class DataSeeder(ApplicationDbContext db)
{
    private readonly PasswordHasher<User> _hasher = new();

    public async Task SeedAsync()
    {
        Console.WriteLine("🌱 Bắt đầu quá trình nạp dữ liệu (Seed Data)...");

        await SeedRolesAsync();
        await SeedUsersAsync();
        await SeedSampleSqlDataAsync();

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

        // 1. Admin mặc định (gán tất cả các roles)
        await CreateUserIfNotExistsAsync("0000000000", "Admin@123", Role.AllRoles);

        // 2. Manager mặc định
        await CreateUserIfNotExistsAsync("0123456789", "Manager@123", Role.Manager);

        // 3. Người dùng mẫu (Candidate, Employer, Censor, SupportStaff)
        for (var i = 1; i <= 5; i++)
        {
            await CreateUserIfNotExistsAsync($"000000000{i}", "Candidate@123", Role.Candidate);
            await CreateUserIfNotExistsAsync($"00000000{i}0", "Employer@123", Role.Employer);
            await CreateUserIfNotExistsAsync($"0000000{i}00", "Censor@123", Role.Censor);
            await CreateUserIfNotExistsAsync($"000000{i}000", "SupportStaff@123", Role.SupportStaff);
        }
    }

    private async Task CreateUserIfNotExistsAsync(string username, string password, params string[] roleNames)
    {
        var existingUser = await db.Users.FirstOrDefaultAsync(u => u.UserName == username);
        if (existingUser != null)
        {
            return;
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            UserName = username,
            NormalizedUserName = username.ToUpperInvariant(),
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };
        user.PasswordHash = _hasher.HashPassword(user, password);

        db.Users.Add(user);
        await db.SaveChangesAsync();

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

        await db.SaveChangesAsync();
    }

    private async Task SeedSampleSqlDataAsync()
    {
        var sampleDataDir = Path.Combine(AppContext.BaseDirectory, "Sample Data");

        // 1. Address SQL (Provinces & Communes)
        var hasProvinces = await db.Provinces.AnyAsync();
        if (!hasProvinces)
        {
            var addressSqlPath = Path.Combine(sampleDataDir, "address.sql");
            if (File.Exists(addressSqlPath))
            {
                Console.WriteLine("➡️ Đang nạp dữ liệu từ address.sql...");
                var sql = await File.ReadAllTextAsync(addressSqlPath);
                await db.Database.ExecuteSqlRawAsync(sql);
                Console.WriteLine("  [+] Đã thực thi address.sql thành công.");
            }
        }
        else
        {
            Console.WriteLine("ℹ️ Dữ liệu địa chỉ (Provinces) đã có sẵn.");
        }

        // 2. Notification SQL
        var hasNotifications = await db.Notifications.AnyAsync();
        if (!hasNotifications)
        {
            var notificationSqlPath = Path.Combine(sampleDataDir, "notification.sql");
            if (File.Exists(notificationSqlPath))
            {
                Console.WriteLine("➡️ Đang nạp dữ liệu từ notification.sql...");
                var sql = await File.ReadAllTextAsync(notificationSqlPath);
                await db.Database.ExecuteSqlRawAsync(sql);
                Console.WriteLine("  [+] Đã thực thi notification.sql thành công.");
            }
        }
        else
        {
            Console.WriteLine("ℹ️ Dữ liệu thông báo (Notifications) đã có sẵn.");
        }
    }
}