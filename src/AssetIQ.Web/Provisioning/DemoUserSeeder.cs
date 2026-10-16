using System.Data;
using AssetIQ.Data;
using AssetIQ.Web.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AssetIQ.Web.Provisioning;

public sealed record DemoUserSeedingResult(
    bool Succeeded,
    string Message,
    IReadOnlyList<string> ErrorCodes);

public sealed class DemoUserSeeder
{
    private static readonly (string Role, string Email)[] DemoUsers =
    [
        (AssetIQRoles.Administrator, "admin@assetiq.local"),
        (AssetIQRoles.PlantManager, "plantmanager@assetiq.local"),
        (AssetIQRoles.MaintenanceEngineer, "maintenance@assetiq.local"),
        (AssetIQRoles.EnergyEngineer, "energy@assetiq.local")
    ];

    private readonly AssetIQDbContext _dbContext;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly UserManager<IdentityUser<Guid>> _userManager;

    public DemoUserSeeder(
        AssetIQDbContext dbContext,
        RoleManager<IdentityRole<Guid>> roleManager,
        UserManager<IdentityUser<Guid>> userManager)
    {
        _dbContext = dbContext;
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public async Task<DemoUserSeedingResult> SeedAsync(
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        foreach (var (roleName, _) in DemoUsers)
        {
            var passwordKey = $"DemoUsers:{roleName}:Password";
            var password = configuration[passwordKey];
            if (string.IsNullOrWhiteSpace(password))
            {
                return Failure($"Configure demo password '{passwordKey}' through approved secret configuration.");
            }
        }

        try
        {
            await using var transaction = await _dbContext.Database
                .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

            foreach (var roleName in AssetIQRoles.All)
            {
                if (await _roleManager.RoleExistsAsync(roleName))
                {
                    continue;
                }

                var roleResult = await _roleManager.CreateAsync(new IdentityRole<Guid>(roleName)
                {
                    Id = Guid.NewGuid()
                });
                if (!roleResult.Succeeded)
                {
                    return Failure("A required AssetIQ role could not be created.", roleResult.Errors);
                }
            }

            foreach (var (roleName, email) in DemoUsers)
            {
                var existingUser = await _userManager.FindByEmailAsync(email)
                    ?? await _userManager.FindByNameAsync(email);
                if (existingUser is not null)
                {
                    continue;
                }

                var user = new IdentityUser<Guid>
                {
                    Id = Guid.NewGuid(),
                    UserName = email,
                    Email = email,
                    LockoutEnabled = true
                };
                var password = configuration[$"DemoUsers:{roleName}:Password"]!;
                var createResult = await _userManager.CreateAsync(user, password);
                if (!createResult.Succeeded)
                {
                    return Failure($"The {roleName} demo account did not satisfy Identity validation requirements.", createResult.Errors);
                }

                var assignment = await _userManager.AddToRoleAsync(user, roleName);
                if (!assignment.Succeeded)
                {
                    return Failure($"The {roleName} demo account could not be assigned its role.", assignment.Errors);
                }
            }

            await transaction.CommitAsync(cancellationToken);
            return new DemoUserSeedingResult(
                true,
                "Demo users are provisioned; existing accounts were left unchanged.",
                Array.Empty<string>());
        }
        catch (Exception)
        {
            return Failure("Demo user seeding could not complete; verify configuration and database availability.");
        }
    }

    private static DemoUserSeedingResult Failure(
        string message,
        IEnumerable<IdentityError>? errors = null) =>
        new(false, message, errors?.Select(error => error.Code).ToArray() ?? Array.Empty<string>());
}
