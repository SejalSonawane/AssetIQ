using AssetIQ.Business.DependencyInjection;
using AssetIQ.Data;
using AssetIQ.Web.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AssetIQ.Web.Provisioning;

var builder = WebApplication.CreateBuilder(args);

var csvRelativePath = builder.Configuration["AssetIQ:Dataset:RollingMillCsvRelativePath"]
    ?? "../../data/rolling-mill/AssetIQ_RollingMill_2000Rows_Demo.csv";
var csvAbsolutePath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, csvRelativePath));

builder.Services.AddControllersWithViews(options =>
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddDbContext<AssetIQDbContext>((serviceProvider, options) =>
{
    var connectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString("AssetIQ");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "Configure ConnectionStrings:AssetIQ using User Secrets or the ConnectionStrings__AssetIQ environment variable before resolving AssetIQDbContext.");
    }

    options.UseNpgsql(connectionString);
});
builder.Services
    .AddIdentity<IdentityUser<Guid>, IdentityRole<Guid>>(options =>
    {
        options.Password.RequiredLength = 12;
        options.Password.RequiredUniqueChars = 4;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AssetIQDbContext>()
    .AddDefaultTokenProviders();
builder.Services.AddAuthorization(AssetIQPolicies.Configure);
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "AssetIQ.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});
builder.Services.AddScoped<FirstAdministratorProvisioner>();
builder.Services.AddScoped<DemoUserSeeder>();
builder.Services.AddAssetIQBusiness(builder.Configuration, csvAbsolutePath);

var app = builder.Build();

if (args.Contains("--provision-first-admin", StringComparer.OrdinalIgnoreCase))
{
    await using var scope = app.Services.CreateAsyncScope();
    var provisioner = scope.ServiceProvider.GetRequiredService<FirstAdministratorProvisioner>();
    var result = await provisioner.ProvisionAsync(
        builder.Configuration["AdminProvisioning:Email"],
        builder.Configuration["AdminProvisioning:Password"]);

    if (result.Succeeded)
    {
        Console.WriteLine(result.Message);
    }
    else
    {
        Console.Error.WriteLine(result.Message);
        foreach (var errorCode in result.ErrorCodes)
        {
            Console.Error.WriteLine($"Identity validation code: {errorCode}");
        }

        Environment.ExitCode = 1;
    }

    return;
}

if (args.Contains("--seed-demo-users", StringComparer.OrdinalIgnoreCase))
{
    await using var scope = app.Services.CreateAsyncScope();
    var seeder = scope.ServiceProvider.GetRequiredService<DemoUserSeeder>();
    var result = await seeder.SeedAsync(builder.Configuration);

    if (result.Succeeded)
    {
        Console.WriteLine(result.Message);
    }
    else
    {
        Console.Error.WriteLine(result.Message);
        foreach (var errorCode in result.ErrorCodes)
        {
            Console.Error.WriteLine($"Identity validation code: {errorCode}");
        }

        Environment.ExitCode = 1;
    }

    return;
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}");

app.Run();

public partial class Program;
