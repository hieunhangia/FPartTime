using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Repository;

namespace DbMigrator;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.secret.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = config.GetConnectionString("DefaultConnection");

        var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
        builder.UseSqlServer(connectionString,
            b => b.MigrationsAssembly(typeof(ApplicationDbContextFactory).Assembly.FullName));

        return new ApplicationDbContext(builder.Options);
    }
}