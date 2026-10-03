using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Repository;
using Repository.Constants;
using Repository.Models.Users;

namespace Api;

public static class SeedDataExtensions
{
    public static async Task SeedDataAsync(this WebApplication app)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        foreach (var roleName in Role.AllRoles)
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
        }

        var defaultAdmin = new User { UserName = "0000000000" };
        await userManager.CreateAsync(defaultAdmin, "Admin@123");
        await userManager.AddToRolesAsync(defaultAdmin, Role.AllRoles);

        var defaultManager = new User { UserName = "0123456789" };
        await userManager.CreateAsync(defaultManager, "Manager@123");
        await userManager.AddToRoleAsync(defaultManager, Role.Manager);

        var defaultUsers = new List<(string Username, string Password, string Role)>();
        for (var i = 1; i <= 5; i++)
        {
            defaultUsers.Add(new ValueTuple<string, string, string>
            {
                Item1 = $"000000000{i}",
                Item2 = "Candidate@123",
                Item3 = Role.Candidate
            });
            defaultUsers.Add(new ValueTuple<string, string, string>
            {
                Item1 = $"00000000{i}0",
                Item2 = "Employer@123",
                Item3 = Role.Employer
            });

            defaultUsers.Add(new ValueTuple<string, string, string>
            {
                Item1 = $"0000000{i}00",
                Item2 = "Censor@123",
                Item3 = Role.Censor
            });

            defaultUsers.Add(new ValueTuple<string, string, string>
            {
                Item1 = $"000000{i}000",
                Item2 = "SupportStaff@123",
                Item3 = Role.SupportStaff
            });
        }

        foreach (var user in defaultUsers)
        {
            var u = new User { UserName = user.Username };
            await userManager.CreateAsync(u, user.Password);
            await userManager.AddToRoleAsync(u, user.Role);
        }

        await dbContext.Database.ExecuteSqlRawAsync(await File.ReadAllTextAsync("Sample Data/address.sql"));

        await dbContext.SaveChangesAsync();
    }
}