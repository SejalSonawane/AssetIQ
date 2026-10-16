using System.Net;
using System.Text.RegularExpressions;
using AssetIQ.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using AssetIQ.Web.Authorization;
namespace AssetIQ.Tests;

public sealed class AuthenticationIntegrationTests
{
    private const string TestEmail = "asset.user@example.test";
    private const string TestPassword = "Secure!AssetPassword2026";

    private static async Task CreateUserAsync(AssetIQWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<Guid>>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        if (!await roleManager.RoleExistsAsync(AssetIQRoles.Administrator))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole<Guid>(AssetIQRoles.Administrator)
            {
                Id = Guid.NewGuid()
            });
            Assert.True(roleResult.Succeeded, string.Join("; ", roleResult.Errors.Select(error => error.Code)));
        }

        var user = new IdentityUser<Guid> { Id = Guid.NewGuid(), UserName = TestEmail, Email = TestEmail };
        var result = await manager.CreateAsync(user, TestPassword);
        Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(error => error.Code)));
        var assignment = await manager.AddToRoleAsync(user, AssetIQRoles.Administrator);
        Assert.True(assignment.Succeeded, string.Join("; ", assignment.Errors.Select(error => error.Code)));
    }

    private static async Task<string> GetAntiForgeryTokenAsync(HttpClient client, string path = "/Account/Login")
    {
        var response = await client.GetAsync(path);
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"");
        Assert.True(match.Success, "The page must render an anti-forgery token.");
        return match.Groups[1].Value;
    }

    private static FormUrlEncodedContent LoginForm(string token, string email, string password, string? returnUrl = null) =>
        new(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Email"] = email,
            ["Password"] = password,
            ["RememberMe"] = "false",
            ["ReturnUrl"] = returnUrl ?? string.Empty
        });

    private static string? GetRedirectPath(HttpResponseMessage response)
    {
        var location = response.Headers.Location;
        return location is null ? null : location.IsAbsoluteUri ? location.AbsolutePath : location.OriginalString;
    }

    [Fact]
    public async Task LoginSignsInAndAllowsOnlySafeLocalReturnUrl()
    {
        using var factory = new AssetIQWebApplicationFactory();
        using var client = factory.CreateHttpsClient();
        await CreateUserAsync(factory);

        var token = await GetAntiForgeryTokenAsync(client, "/Account/Login?returnUrl=%2FEquipment");
        using var response = await client.PostAsync(
            "/Account/Login",
            LoginForm(token, TestEmail, TestPassword, "/Equipment"));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Equipment", response.Headers.Location?.OriginalString);
        Assert.Contains(response.Headers.GetValues("Set-Cookie"), cookie =>
            cookie.StartsWith("AssetIQ.Auth=", StringComparison.Ordinal) &&
            cookie.Contains("secure", StringComparison.OrdinalIgnoreCase) &&
            cookie.Contains("httponly", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Equipment")).StatusCode);
    }

    [Fact]
    public async Task LoginRejectsExternalReturnUrlAndUsesGenericFailureMessage()
    {
        using var factory = new AssetIQWebApplicationFactory();
        using var client = factory.CreateHttpsClient();
        await CreateUserAsync(factory);

        var externalToken = await GetAntiForgeryTokenAsync(client, "/Account/Login?returnUrl=https%3A%2F%2Fevil.example");
        using var success = await client.PostAsync(
            "/Account/Login",
            LoginForm(externalToken, TestEmail, TestPassword, string.Empty));
        Assert.Equal(HttpStatusCode.Redirect, success.StatusCode);
        Assert.Equal("/", GetRedirectPath(success));

        using var failureClientFactory = new AssetIQWebApplicationFactory();
        using var failureClient = failureClientFactory.CreateHttpsClient();
        await CreateUserAsync(failureClientFactory);

        var wrongPasswordToken = await GetAntiForgeryTokenAsync(failureClient);
        using var wrongPassword = await failureClient.PostAsync(
            "/Account/Login",
            LoginForm(wrongPasswordToken, TestEmail, "Wrong!Password2026"));
        var wrongPasswordHtml = await wrongPassword.Content.ReadAsStringAsync();

        var unknownUserToken = await GetAntiForgeryTokenAsync(failureClient);
        using var unknownUser = await failureClient.PostAsync(
            "/Account/Login",
            LoginForm(unknownUserToken, "unknown@example.test", "Wrong!Password2026"));
        var unknownUserHtml = await unknownUser.Content.ReadAsStringAsync();

        const string genericMessage = "Sign-in failed. Check your credentials or contact your administrator.";
        Assert.Equal(HttpStatusCode.OK, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.OK, unknownUser.StatusCode);
        Assert.Contains(genericMessage, wrongPasswordHtml);
        Assert.Contains(genericMessage, unknownUserHtml);
    }

    [Fact]
    public async Task LoginAndLogoutRequireAntiForgeryAndLogoutClearsAccess()
    {
        using var factory = new AssetIQWebApplicationFactory();
        using var client = factory.CreateHttpsClient();
        await CreateUserAsync(factory);

        using var missingLoginToken = await client.PostAsync(
            "/Account/Login",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Email"] = TestEmail,
                ["Password"] = TestPassword
            }));
        Assert.Equal(HttpStatusCode.BadRequest, missingLoginToken.StatusCode);

        var loginToken = await GetAntiForgeryTokenAsync(client);
        using var login = await client.PostAsync("/Account/Login", LoginForm(loginToken, TestEmail, TestPassword));
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Dashboard")).StatusCode);

        using var missingLogoutToken = await client.PostAsync("/Account/Logout", new FormUrlEncodedContent([]));
        Assert.Equal(HttpStatusCode.BadRequest, missingLogoutToken.StatusCode);

        var logoutToken = await GetAntiForgeryTokenAsync(client);
        using var logout = await client.PostAsync(
            "/Account/Logout",
            new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = logoutToken }));
        Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);
        Assert.Equal("/Account/Login", GetRedirectPath(logout));

        using var protectedResponse = await client.GetAsync("/Equipment");
        Assert.Equal(HttpStatusCode.Redirect, protectedResponse.StatusCode);
        Assert.Equal("/Account/Login", GetRedirectPath(protectedResponse));
    }

    [Fact]
    public async Task ProtectedRoutesRedirectAnonymousUsersAndAccountRoutesStayPublic()
    {
        using var factory = new AssetIQWebApplicationFactory();
        using var client = factory.CreateHttpsClient();

        var protectedPaths = new[]
        {
            "/Dashboard",
            "/Equipment",
            "/Equipment/Details/RM-1002",
            "/PredictiveMaintenance",
            "/FailureAnalytics",
            "/EnergyAnalytics",
            "/Alerts",
            "/Maintenance",
            "/Settings"
        };
        foreach (var path in protectedPaths)
        {
            using var protectedResponse = await client.GetAsync(path);
            Assert.Equal(HttpStatusCode.Redirect, protectedResponse.StatusCode);
            Assert.Equal("/Account/Login", GetRedirectPath(protectedResponse));
        }

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Account/Login")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Account/AccessDenied")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/Account/Register")).StatusCode);
    }
}

public sealed class AssetIQWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"assetiq_auth_tests_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureTestServices(services =>
        {
            var dbContextConfigurations = services
                .Where(descriptor => descriptor.ServiceType.IsGenericType &&
                    descriptor.ServiceType.GetGenericTypeDefinition().Name == "IDbContextOptionsConfiguration`1")
                .ToList();
            foreach (var descriptor in dbContextConfigurations)
            {
                services.Remove(descriptor);
            }
            services.RemoveAll<DbContextOptions<AssetIQDbContext>>();
            services.RemoveAll<AssetIQDbContext>();
            services.AddDbContext<AssetIQDbContext>(options => options
                .UseInMemoryDatabase(_databaseName)
                .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
        });
    }

    public HttpClient CreateHttpsClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        BaseAddress = new Uri("https://localhost")
    });
}