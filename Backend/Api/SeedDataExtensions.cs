using Microsoft.AspNetCore.Identity;
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
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var roleName in Role.AllRoles)
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }

        var defaultAdmin = new User { UserName = "admin" };
        await userManager.CreateAsync(defaultAdmin, "Admin@123");
        await userManager.AddToRolesAsync(defaultAdmin, Role.AllRoles);

        var defaultManager = new User { UserName = "manager" };
        await userManager.CreateAsync(defaultManager, "Manager@123");
        await userManager.AddToRoleAsync(defaultManager, Role.Manager);

        var defaultStaffs = new List<(string Username, string Password, string Role)>();
        for (var i = 1; i <= 5; i++)
        {
            defaultStaffs.Add(new ValueTuple<string, string, string>
            {
                Item1 = $"censor{i}",
                Item2 = "Censor@123",
                Item3 = Role.Censor
            });
            defaultStaffs.Add(new ValueTuple<string, string, string>
            {
                Item1 = $"support{i}",
                Item2 = "Support@123",
                Item3 = Role.SupportStaff
            });
        }

        foreach (var staff in defaultStaffs)
        {
            var s = new User { UserName = staff.Username };
            await userManager.CreateAsync(s, staff.Password);
            await userManager.AddToRoleAsync(s, staff.Role);
        }

        var defaultUsers = new List<(string PhoneNumber, string Role)>();
        for (var i = 1; i <= 5; i++)
        {
            defaultUsers.Add(new ValueTuple<string, string>
            {
                Item1 = $"000000000{i}",
                Item2 = Role.Student
            });
            defaultUsers.Add(new ValueTuple<string, string>
            {
                Item1 = $"00000000{i}0",
                Item2 = Role.Employer
            });
        }

        foreach (var user in defaultUsers)
        {
            var u = new User { UserName = user.PhoneNumber };
            await userManager.CreateAsync(u);
            await userManager.AddToRoleAsync(u, user.Role);
        }

        await dbContext.SaveChangesAsync();
    }
}