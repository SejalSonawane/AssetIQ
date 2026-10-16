using AssetIQ.Data;
using AssetIQ.Web.Authorization;
using AssetIQ.Web.Provisioning;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AssetIQ.Tests;

public sealed class FirstAdministratorProvisioningTests
{
    private const string Email = "first.admin@example.test";
    private const string StrongPassword = "Secure!AdminPassword2026";

    [Fact]
    public async Task ProvisioningCreatesAdministratorWithIdentityPasswordHash()
    {
        using var factory = new AssetIQWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var provisioner = scope.ServiceProvider.GetRequiredService<FirstAdministratorProvisioner>();

        var result = await provisioner.ProvisionAsync(Email, StrongPassword);

        Assert.True(result.Succeeded);
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var user = await userManager.FindByEmailAsync(Email);
        Assert.NotNull(user);
        Assert.True(user.LockoutEnabled);
        Assert.NotEqual(StrongPassword, user.PasswordHash);
        Assert.True(await userManager.CheckPasswordAsync(user, StrongPassword));
        Assert.True(await roleManager.RoleExistsAsync(FirstAdministratorProvisioner.AdministratorRole));
        foreach (var roleName in AssetIQRoles.All)
        {
            Assert.True(await roleManager.RoleExistsAsync(roleName));
        }
        Assert.True(await userManager.IsInRoleAsync(user, FirstAdministratorProvisioner.AdministratorRole));
    }

    [Fact]
    public async Task ProvisioningRefusesSecondAdministrator()
    {
        using var factory = new AssetIQWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var provisioner = scope.ServiceProvider.GetRequiredService<FirstAdministratorProvisioner>();

        Assert.True((await provisioner.ProvisionAsync(Email, StrongPassword)).Succeeded);
        var second = await provisioner.ProvisionAsync("second.admin@example.test", "Another!SecurePassword2026");

        Assert.False(second.Succeeded);
        Assert.Contains("already exists", second.Message, StringComparison.OrdinalIgnoreCase);
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();
        Assert.Single(await userManager.GetUsersInRoleAsync(FirstAdministratorProvisioner.AdministratorRole));
    }

    [Fact]
    public async Task ProvisioningRejectsInvalidPasswordAndExistingIdentity()
    {
        using var factory = new AssetIQWebApplicationFactory();
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var provisioner = services.GetRequiredService<FirstAdministratorProvisioner>();
        var weakPasswordResult = await provisioner.ProvisionAsync(Email, "weak");

        Assert.False(weakPasswordResult.Succeeded);
        Assert.NotEmpty(weakPasswordResult.ErrorCodes);
        var userManager = services.GetRequiredService<UserManager<IdentityUser<Guid>>>();
        Assert.Empty(await userManager.Users.ToListAsync());

        var existingUser = new IdentityUser<Guid> { Id = Guid.NewGuid(), Email = Email, UserName = Email };
        Assert.True((await userManager.CreateAsync(existingUser, StrongPassword)).Succeeded);
        var duplicateIdentity = await provisioner.ProvisionAsync(Email, StrongPassword);

        Assert.False(duplicateIdentity.Succeeded);
        Assert.Contains("already exists", duplicateIdentity.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(await services.GetRequiredService<RoleManager<IdentityRole<Guid>>>()
            .RoleExistsAsync(FirstAdministratorProvisioner.AdministratorRole));
    }

    [Fact]
    public async Task NormalWebStartupDoesNotProvisionDefaultAdministrator()
    {
        using var factory = new AssetIQWebApplicationFactory();
        using var client = factory.CreateHttpsClient();

        Assert.Equal(System.Net.HttpStatusCode.OK, (await client.GetAsync("/Account/Login")).StatusCode);
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();
        Assert.Empty(await userManager.Users.ToListAsync());
        Assert.False(await scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>()
            .RoleExistsAsync(FirstAdministratorProvisioner.AdministratorRole));
    }
}