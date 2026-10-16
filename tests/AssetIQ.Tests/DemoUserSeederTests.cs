using AssetIQ.Data;
using AssetIQ.Web.Authorization;
using AssetIQ.Web.Provisioning;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AssetIQ.Tests;

public sealed class DemoUserSeederTests
{
    private const string Password = "Secure!DemoUser2026";

    private static IConfiguration CreateConfiguration(string? administratorPassword = Password) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"DemoUsers:{AssetIQRoles.Administrator}:Password"] = administratorPassword,
                [$"DemoUsers:{AssetIQRoles.PlantManager}:Password"] = Password,
                [$"DemoUsers:{AssetIQRoles.MaintenanceEngineer}:Password"] = Password,
                [$"DemoUsers:{AssetIQRoles.EnergyEngineer}:Password"] = Password
            })
            .Build();

    private static async Task<(IServiceProvider Services, UserManager<IdentityUser<Guid>> Users)> ResolveAsync(
        AssetIQWebApplicationFactory factory)
    {
        var scope = factory.Services.CreateScope();
        await Task.CompletedTask;
        return (scope.ServiceProvider, scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>());
    }

    [Fact]
    public async Task SeedCreatesFourDemoUsersWithOnlyCorrespondingRoles()
    {
        using var factory = new AssetIQWebApplicationFactory();
        var (services, userManager) = await ResolveAsync(factory);
        var seeder = services.GetRequiredService<DemoUserSeeder>();

        var result = await seeder.SeedAsync(CreateConfiguration());

        Assert.True(result.Succeeded);
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var roleName in AssetIQRoles.All)
        {
            Assert.True(await roleManager.RoleExistsAsync(roleName));
        }

        foreach (var (roleName, email) in new (string, string)[]
        {
            (AssetIQRoles.Administrator, "admin@assetiq.local"),
            (AssetIQRoles.PlantManager, "plantmanager@assetiq.local"),
            (AssetIQRoles.MaintenanceEngineer, "maintenance@assetiq.local"),
            (AssetIQRoles.EnergyEngineer, "energy@assetiq.local")
        })
        {
            var user = await userManager.FindByEmailAsync(email);
            Assert.NotNull(user);
            Assert.NotEqual(Password, user.PasswordHash);
            Assert.True(user.LockoutEnabled);
            Assert.True(await userManager.CheckPasswordAsync(user, Password));
            Assert.True(await userManager.IsInRoleAsync(user, roleName));
            var roles = await userManager.GetRolesAsync(user);
            Assert.Single(roles);
            Assert.Equal(roleName, roles.Single());
        }
    }

    [Fact]
    public async Task SeedIsIdempotentAndDoesNotResetExistingUser()
    {
        using var factory = new AssetIQWebApplicationFactory();
        var (services, userManager) = await ResolveAsync(factory);
        var seeder = services.GetRequiredService<DemoUserSeeder>();

        Assert.True((await seeder.SeedAsync(CreateConfiguration())).Succeeded);
        var firstUser = await userManager.FindByEmailAsync("admin@assetiq.local");
        Assert.NotNull(firstUser);
        var originalHash = firstUser.PasswordHash;
        var originalId = firstUser.Id;

        var result = await seeder.SeedAsync(CreateConfiguration("Another!StrongPassword2026"));

        Assert.True(result.Succeeded);
        var user = await userManager.FindByEmailAsync("admin@assetiq.local");
        Assert.NotNull(user);
        Assert.Equal(originalId, user.Id);
        Assert.Equal(originalHash, user.PasswordHash);
        Assert.False(await userManager.CheckPasswordAsync(user, "Another!StrongPassword2026"));
        Assert.Equal(4, await userManager.Users.CountAsync());
    }

    [Fact]
    public async Task SeedRejectsInvalidOrMissingPasswords()
    {
        using var factory = new AssetIQWebApplicationFactory();
        var (services, userManager) = await ResolveAsync(factory);
        var seeder = services.GetRequiredService<DemoUserSeeder>();

        var missing = await seeder.SeedAsync(CreateConfiguration(null));
        Assert.False(missing.Succeeded);
        Assert.Contains("Configure demo password 'DemoUsers:Administrator:Password'", missing.Message);

        var invalid = await seeder.SeedAsync(CreateConfiguration("weak"));
        Assert.False(invalid.Succeeded);
        Assert.NotEmpty(invalid.ErrorCodes);
        Assert.Empty(await userManager.Users.ToListAsync());
    }

    [Fact]
    public async Task NormalWebStartupDoesNotSeedDemoUsers()
    {
        using var factory = new AssetIQWebApplicationFactory();
        using var client = factory.CreateHttpsClient();

        Assert.Equal(System.Net.HttpStatusCode.OK, (await client.GetAsync("/Account/Login")).StatusCode);
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();
        Assert.Empty(await userManager.Users.ToListAsync());
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var roleName in AssetIQRoles.All)
        {
            Assert.False(await roleManager.RoleExistsAsync(roleName));
        }
    }
}
