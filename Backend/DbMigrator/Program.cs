using DbMigrator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Repository;

var isReset = args.Contains("--reset");
var isAll = args.Contains("--all");
var isMigrate = args.Contains("--migrate") || args.Contains("--migrate-only");
var isSeed = args.Contains("--seed") || args.Contains("--seed-only");

if (args.Length == 0 || (!isReset && !isAll && !isMigrate && !isSeed))
{
    Console.WriteLine("""
                      =====================================================
                                FPartTime Database Migrator & Seeder
                      =====================================================
                      Cách sử dụng:
                        dotnet run --project DbMigrator -- [options]

                      Options:
                        --all        Chạy EF Core Migrations và nạp Seed Data
                        --migrate    Chỉ chạy EF Core Migrations (không nạp dữ liệu mẫu)
                        --seed       Chỉ nạp dữ liệu mẫu (không chạy migration)
                        --reset      XÓA DB hiện tại, chạy lại Migrations và nạp Seed Data

                      Ví dụ:
                        dotnet run --project DbMigrator -- --all
                        dotnet run --project DbMigrator -- --migrate
                        dotnet run --project DbMigrator -- --seed
                        dotnet run --project DbMigrator -- --reset

                      Chạy qua Docker Compose:
                        docker compose run --rm db-migrator --all
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

var shouldMigrate = isReset || isAll || isMigrate;
var shouldSeed = isReset || isAll || isSeed;

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

    if (shouldSeed)
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