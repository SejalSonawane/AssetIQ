using System.Security.Claims;
using AssetIQ.Web.Authorization;
using AssetIQ.Web.ViewModels;

namespace AssetIQ.Web.Infrastructure;

public static class AppNavigation
{
    public static IReadOnlyList<NavigationItem> Items { get; } =
    [
        new() { ModuleKey = "dashboard", Label = "Dashboard", Controller = "Dashboard", Icon = "bi-speedometer2", AllowedRoles = AssetIQRoles.All },
        new() { ModuleKey = "equipment", Label = "Equipment", Controller = "Equipment", Icon = "bi-gear-wide-connected", AllowedRoles = AssetIQRoles.All },
        new() { ModuleKey = "predictive", Label = "Predictive Maintenance", Controller = "PredictiveMaintenance", Icon = "bi-graph-up-arrow", AllowedRoles = AssetIQRoles.All },
        new() { ModuleKey = "failure", Label = "Failure Analytics", Controller = "FailureAnalytics", Icon = "bi-exclamation-triangle", AllowedRoles = [AssetIQRoles.Administrator, AssetIQRoles.PlantManager, AssetIQRoles.MaintenanceEngineer] },
        new() { ModuleKey = "energy", Label = "Energy Analytics", Controller = "EnergyAnalytics", Icon = "bi-lightning-charge", AllowedRoles = [AssetIQRoles.Administrator, AssetIQRoles.PlantManager, AssetIQRoles.EnergyEngineer] },
        new() { ModuleKey = "maintenance", Label = "Maintenance Management", Controller = "Maintenance", Icon = "bi-tools", AllowedRoles = [AssetIQRoles.Administrator, AssetIQRoles.PlantManager, AssetIQRoles.MaintenanceEngineer] },
        new() { ModuleKey = "alerts", Label = "Alerts", Controller = "Alerts", Icon = "bi-bell", AllowedRoles = [AssetIQRoles.Administrator, AssetIQRoles.PlantManager, AssetIQRoles.MaintenanceEngineer] },
        new() { ModuleKey = "settings", Label = "Settings", Controller = "Settings", Icon = "bi-sliders", AllowedRoles = [AssetIQRoles.Administrator] }
    ];

    public static bool CanAccess(NavigationItem item, ClaimsPrincipal user) =>
        item.AllowedRoles.Any(user.IsInRole);
}
