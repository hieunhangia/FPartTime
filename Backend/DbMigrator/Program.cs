using DbMigrator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Repository;

var isReset = args.Contains("--reset");
var isMigrate = args.Contains("--migrate");

if (args.Length == 0 || (!isReset && !isMigrate))
{
    Console.WriteLine("""
                      =====================================================
                                FPartTime Database Migrator & Seeder
                      =====================================================
                      Cách sử dụng:
                        dotnet run --project DbMigrator [options]

                      Options:
                        --migrate    Chạy EF Core Migrations
                        --reset      XÓA DB hiện tại, chạy lại Migrations và nạp Seed Data

                      Ví dụ:
                        dotnet run --project DbMigrator --migrate
                        dotnet run --project DbMigrator --reset

                      Chạy qua Docker Compose:
                        docker compose run --rm db-migrator --migrate
                        docker compose run --rm db-migrator --reset
                      =====================================================
                      """);
    return;
}

var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile("appsettings.secret.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var connectionString = config.GetConnectionString("DefaultConnection");

var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseSqlServer(connectionString, b => b.MigrationsAssembly(typeof(ApplicationDbContextFactory).Assembly.FullName))
    .Options;

await using var db = new ApplicationDbContext(options);

var shouldMigrate = isReset || isMigrate;

Console.WriteLine("==================================================");
Console.WriteLine("🚀 Bắt đầu FPartTime Database Tool (Pure DbContext)...");
Console.WriteLine("==================================================");

try
{
    if (isReset)
    {
        Console.WriteLine("⚠️  Đang xóa toàn bộ Database (--reset)...");
        await db.Database.EnsureDeletedAsync();
        Console.WriteLine("✅ Đã xóa Database thành công.");
    }

    if (shouldMigrate)
    {
        Console.WriteLine("🔄 Đang áp dụng EF Core Migrations...");
        await db.Database.MigrateAsync();
        Console.WriteLine("✅ Migrations hoàn tất.");
    }

    if (isReset)
    {
        var seeder = new DataSeeder(db);
        await seeder.SeedAsync();
    }

    Console.WriteLine("==================================================");
    Console.WriteLine("🎉 TẤT CẢ TÁC VỤ ĐÃ HOÀN TẤT THÀNH CÔNG!");
    Console.WriteLine("==================================================");
}
catch (Exception ex)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"❌ Đã xảy ra lỗi: {ex.Message}");
    Console.ResetColor();
    Environment.ExitCode = 1;
}