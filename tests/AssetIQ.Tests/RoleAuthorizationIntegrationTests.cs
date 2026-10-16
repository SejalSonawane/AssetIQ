using System.Net;
using System.Text.RegularExpressions;
using AssetIQ.Data;
using AssetIQ.Web.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace AssetIQ.Tests;

public sealed class RoleAuthorizationIntegrationTests
{
    private static readonly string[] ReadRoutes =
    [
        "/Dashboard", "/Equipment", "/PredictiveMaintenance", "/FailureAnalytics",
        "/EnergyAnalytics", "/Alerts", "/Maintenance"
    ];

    private static async Task<HttpClient> SignInAsAsync(
        AssetIQWebApplicationFactory factory,
        string email,
        string role)
    {
        using var scope = factory.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        if (!await roleManager.RoleExistsAsync(role))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole<Guid>(role) { Id = Guid.NewGuid() });
            Assert.True(roleResult.Succeeded, string.Join("; ", roleResult.Errors.Select(error => error.Code)));
        }

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();
        var user = new IdentityUser<Guid> { Id = Guid.NewGuid(), UserName = email, Email = email, LockoutEnabled = true };
        var password = "Secure!RoleTestPassword2026";
        var userResult = await userManager.CreateAsync(user, password);
        Assert.True(userResult.Succeeded, string.Join("; ", userResult.Errors.Select(error => error.Code)));
        var assignment = await userManager.AddToRoleAsync(user, role);
        Assert.True(assignment.Succeeded, string.Join("; ", assignment.Errors.Select(error => error.Code)));

        var client = factory.CreateHttpsClient();
        var token = await GetAntiForgeryTokenAsync(client, "/Account/Login");
        using var login = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Email"] = email,
            ["Password"] = password,
            ["RememberMe"] = "false",
            ["ReturnUrl"] = string.Empty
        }));
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        return client;
    }

    private static async Task<string> GetAntiForgeryTokenAsync(HttpClient client, string path)
    {
        using var response = await client.GetAsync(path);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"");
        Assert.True(match.Success, "Expected an anti-forgery token on the form.");
        return match.Groups[1].Value;
    }

    private static string? RedirectPath(HttpResponseMessage response)
    {
        var location = response.Headers.Location;
        return location is null ? null : location.IsAbsoluteUri ? location.AbsolutePath : location.OriginalString;
    }

    private static async Task AssertAllowedAsync(HttpClient client, IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            using var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    private static async Task AssertDeniedAsync(HttpClient client, IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            using var response = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/Account/AccessDenied", RedirectPath(response));
        }
    }

    private static async Task<string> SidebarAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/Dashboard");
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        var sidebar = Regex.Match(html, "<aside class=\"app-sidebar\".*?</aside>", RegexOptions.Singleline);
        Assert.True(sidebar.Success, "Expected the shared role-filtered sidebar.");
        return sidebar.Value;
    }

    private static async Task<HttpResponseMessage> PostMaintenanceActionAsync(HttpClient client, string route)
    {
        var token = await GetAntiForgeryTokenAsync(client, "/Account/Login");
        return await client.PostAsync(route, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["equipmentCode"] = "RM-1002",
            ["description"] = "Inspect bearing"
        }));
    }

    private static async Task<HttpResponseMessage> PostSettingsImportAsync(HttpClient client)
    {
        var token = await GetAntiForgeryTokenAsync(client, "/Account/Login");
        return await client.PostAsync("/Settings/ImportConfiguredCsv", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token
        }));
    }

    private static async Task<HttpResponseMessage> CompleteMaintenanceActionAsync(HttpClient client, Guid actionId)
    {
        var token = await GetAntiForgeryTokenAsync(client, "/Account/Login");
        return await client.PostAsync("/Maintenance/CompleteAction", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["id"] = actionId.ToString()
        }));
    }

    [Fact]
    public async Task AdministratorCanReadEveryExistingModuleAndManageMaintenance()
    {
        using var factory = new AssetIQWebApplicationFactory();
        using var client = await SignInAsAsync(factory, "admin.rbac@example.test", AssetIQRoles.Administrator);

        await AssertAllowedAsync(client, ReadRoutes.Append("/Settings"));
        var sidebar = await SidebarAsync(client);
        foreach (var label in new[] { "Dashboard", "Equipment", "Predictive Maintenance", "Failure Analytics", "Energy Analytics", "Alerts", "Maintenance Management", "Settings" })
        {
            Assert.Contains(label, sidebar);
        }

        using var create = await PostMaintenanceActionAsync(client, "/Maintenance/CreateAction");
        Assert.Equal(HttpStatusCode.Redirect, create.StatusCode);
        Assert.Equal("/Maintenance", RedirectPath(create));

        using var import = await PostSettingsImportAsync(client);
        Assert.Equal(HttpStatusCode.Redirect, import.StatusCode);
        Assert.Equal("/Settings", RedirectPath(import));

        using var scope = factory.Services.CreateScope();
        var actionService = scope.ServiceProvider.GetRequiredService<AssetIQ.Business.Interfaces.IMaintenanceActionService>();
        var createdAction = Assert.Single(await actionService.GetActionsAsync());
        using var complete = await CompleteMaintenanceActionAsync(client, createdAction.Id);
        Assert.Equal(HttpStatusCode.Redirect, complete.StatusCode);
        Assert.Equal("/Maintenance", RedirectPath(complete));
    }

    [Fact]
    public async Task PlantManagerHasReadOnlyOperationalAccess()
    {
        using var factory = new AssetIQWebApplicationFactory();
        using var client = await SignInAsAsync(factory, "plant.manager.rbac@example.test", AssetIQRoles.PlantManager);

        await AssertAllowedAsync(client, ReadRoutes);
        await AssertDeniedAsync(client, ["/Settings"]);
        var sidebar = await SidebarAsync(client);
        Assert.DoesNotContain("Settings", sidebar);

        using var create = await PostMaintenanceActionAsync(client, "/Maintenance/CreateAction");
        Assert.Equal(HttpStatusCode.Redirect, create.StatusCode);
        Assert.Equal("/Account/AccessDenied", RedirectPath(create));

        using var import = await PostSettingsImportAsync(client);
        Assert.Equal(HttpStatusCode.Redirect, import.StatusCode);
        Assert.Equal("/Account/AccessDenied", RedirectPath(import));

        using var complete = await CompleteMaintenanceActionAsync(client, Guid.NewGuid());
        Assert.Equal(HttpStatusCode.Redirect, complete.StatusCode);
        Assert.Equal("/Account/AccessDenied", RedirectPath(complete));
    }

    [Fact]
    public async Task MaintenanceEngineerCanManageMaintenanceButNotEnergyOrSettings()
    {
        using var factory = new AssetIQWebApplicationFactory();
        using var client = await SignInAsAsync(factory, "maintenance.rbac@example.test", AssetIQRoles.MaintenanceEngineer);

        await AssertAllowedAsync(client, ["/Dashboard", "/Equipment", "/PredictiveMaintenance", "/FailureAnalytics", "/Alerts", "/Maintenance"]);
        await AssertDeniedAsync(client, ["/EnergyAnalytics", "/Settings"]);
        var sidebar = await SidebarAsync(client);
        Assert.Contains("Maintenance Management", sidebar);
        Assert.DoesNotContain("Energy Analytics", sidebar);
        Assert.DoesNotContain("Settings", sidebar);

        using var import = await PostSettingsImportAsync(client);
        Assert.Equal(HttpStatusCode.Redirect, import.StatusCode);
        Assert.Equal("/Account/AccessDenied", RedirectPath(import));

        using var create = await PostMaintenanceActionAsync(client, "/Maintenance/CreateAction");
        Assert.Equal(HttpStatusCode.Redirect, create.StatusCode);
        Assert.Equal("/Maintenance", RedirectPath(create));

        using var scope = factory.Services.CreateScope();
        var actionService = scope.ServiceProvider.GetRequiredService<AssetIQ.Business.Interfaces.IMaintenanceActionService>();
        var createdAction = Assert.Single(await actionService.GetActionsAsync());
        using var complete = await CompleteMaintenanceActionAsync(client, createdAction.Id);
        Assert.Equal(HttpStatusCode.Redirect, complete.StatusCode);
        Assert.Equal("/Maintenance", RedirectPath(complete));
    }

    [Fact]
    public async Task EnergyEngineerCanViewEnergyButNotUncategorizedAlertsOrMaintenance()
    {
        using var factory = new AssetIQWebApplicationFactory();
        using var client = await SignInAsAsync(factory, "energy.rbac@example.test", AssetIQRoles.EnergyEngineer);

        await AssertAllowedAsync(client, ["/Dashboard", "/Equipment", "/PredictiveMaintenance", "/EnergyAnalytics"]);
        await AssertDeniedAsync(client, ["/FailureAnalytics", "/Alerts", "/Maintenance", "/Settings"]);
        var sidebar = await SidebarAsync(client);
        Assert.Contains("Energy Analytics", sidebar);
        Assert.DoesNotContain("Alerts", sidebar);
        Assert.DoesNotContain("Maintenance Management", sidebar);
        Assert.DoesNotContain("Settings", sidebar);

        using var import = await PostSettingsImportAsync(client);
        Assert.Equal(HttpStatusCode.Redirect, import.StatusCode);
        Assert.Equal("/Account/AccessDenied", RedirectPath(import));

        using var complete = await PostMaintenanceActionAsync(client, "/Maintenance/CompleteAction");
        Assert.Equal(HttpStatusCode.Redirect, complete.StatusCode);
        Assert.Equal("/Account/AccessDenied", RedirectPath(complete));

        using var create = await PostMaintenanceActionAsync(client, "/Maintenance/CreateAction");
        Assert.Equal(HttpStatusCode.Redirect, create.StatusCode);
        Assert.Equal("/Account/AccessDenied", RedirectPath(create));
    }
}