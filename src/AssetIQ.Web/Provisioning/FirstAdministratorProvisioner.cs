using System.ComponentModel.DataAnnotations;
using System.Data;
using AssetIQ.Web.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AssetIQ.Data;

namespace AssetIQ.Web.Provisioning;

public sealed record FirstAdministratorProvisioningResult(
    bool Succeeded,
    string Message,
    IReadOnlyList<string> ErrorCodes);

public sealed class FirstAdministratorProvisioner
{
    public const string AdministratorRole = AssetIQRoles.Administrator;

    private readonly AssetIQDbContext _dbContext;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly UserManager<IdentityUser<Guid>> _userManager;

    public FirstAdministratorProvisioner(
        AssetIQDbContext dbContext,
        RoleManager<IdentityRole<Guid>> roleManager,
        UserManager<IdentityUser<Guid>> userManager)
    {
        _dbContext = dbContext;
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public async Task<FirstAdministratorProvisioningResult> ProvisionAsync(
        string? email,
        string? password,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedEmail) ||
            !new EmailAddressAttribute().IsValid(normalizedEmail) ||
            string.IsNullOrWhiteSpace(password))
        {
            return Failure("Provide a valid administrator email and password through approved secret configuration.");
        }

        try
        {
            await using var transaction = await _dbContext.Database
                .BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

            if (await _roleManager.RoleExistsAsync(AdministratorRole) &&
                (await _userManager.GetUsersInRoleAsync(AdministratorRole)).Count > 0)
            {
                return Failure("An Administrator account already exists; provisioning is one-time.");
            }

            var existingUser = await _userManager.FindByEmailAsync(normalizedEmail)
                ?? await _userManager.FindByNameAsync(normalizedEmail);
            if (existingUser is not null)
            {
                return Failure("An account with the requested administrator identity already exists.");
            }

            var user = new IdentityUser<Guid>
            {
                Id = Guid.NewGuid(),
                UserName = normalizedEmail,
                Email = normalizedEmail,
                LockoutEnabled = true
            };
            var userResult = await _userManager.CreateAsync(user, password);
            if (!userResult.Succeeded)
            {
                return Failure("Administrator credentials did not meet Identity validation requirements.", userResult.Errors);
            }

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
                    return Failure("An AssetIQ role could not be created.", roleResult.Errors);
                }
            }

            var assignment = await _userManager.AddToRoleAsync(user, AdministratorRole);
            if (!assignment.Succeeded)
            {
                return Failure("Administrator role assignment failed.", assignment.Errors);
            }

            await transaction.CommitAsync(cancellationToken);
            return new FirstAdministratorProvisioningResult(
                true,
                "The first Administrator account was provisioned successfully.",
                Array.Empty<string>());
        }
        catch (Exception)
        {
            return Failure("Administrator provisioning could not complete; verify database availability and retry.");
        }
    }

    private static FirstAdministratorProvisioningResult Failure(
        string message,
        IEnumerable<IdentityError>? errors = null) =>
        new(false, message, errors?.Select(error => error.Code).ToArray() ?? Array.Empty<string>());
}