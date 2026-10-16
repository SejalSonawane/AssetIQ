using Microsoft.AspNetCore.Authorization;

namespace AssetIQ.Web.Authorization;

public static class AssetIQRoles
{
    public const string Administrator = "Administrator";
    public const string PlantManager = "Plant Manager";
    public const string MaintenanceEngineer = "Maintenance Engineer";
    public const string EnergyEngineer = "Energy Engineer";

    public static readonly string[] All =
    [
        Administrator,
        PlantManager,
        MaintenanceEngineer,
        EnergyEngineer
    ];
}

public static class AssetIQPolicies
{
    public const string DashboardRead = "AssetIQ.Dashboard.Read";
    public const string EquipmentRead = "AssetIQ.Equipment.Read";
    public const string PredictiveMaintenanceRead = "AssetIQ.PredictiveMaintenance.Read";
    public const string FailureAnalyticsRead = "AssetIQ.FailureAnalytics.Read";
    public const string EnergyAnalyticsRead = "AssetIQ.EnergyAnalytics.Read";
    public const string AlertsRead = "AssetIQ.Alerts.Read";
    public const string MaintenanceRead = "AssetIQ.Maintenance.Read";
    public const string MaintenanceManage = "AssetIQ.Maintenance.Manage";
    public const string SettingsManage = "AssetIQ.Settings.Manage";

    private static readonly string[] FailureAnalyticsRoles =
        [AssetIQRoles.Administrator, AssetIQRoles.PlantManager, AssetIQRoles.MaintenanceEngineer];
    private static readonly string[] EnergyAnalyticsRoles =
        [AssetIQRoles.Administrator, AssetIQRoles.PlantManager, AssetIQRoles.EnergyEngineer];
    private static readonly string[] AlertsRoles =
        [AssetIQRoles.Administrator, AssetIQRoles.PlantManager, AssetIQRoles.MaintenanceEngineer];
    private static readonly string[] MaintenanceReadRoles =
        [AssetIQRoles.Administrator, AssetIQRoles.PlantManager, AssetIQRoles.MaintenanceEngineer];
    private static readonly string[] MaintenanceManageRoles =
        [AssetIQRoles.Administrator, AssetIQRoles.MaintenanceEngineer];
    private static readonly string[] AdministratorOnly = [AssetIQRoles.Administrator];

    public static void Configure(AuthorizationOptions options)
    {
        options.AddPolicy(DashboardRead, policy => policy.RequireRole(AssetIQRoles.All));
        options.AddPolicy(EquipmentRead, policy => policy.RequireRole(AssetIQRoles.All));
        options.AddPolicy(PredictiveMaintenanceRead, policy => policy.RequireRole(AssetIQRoles.All));
        options.AddPolicy(FailureAnalyticsRead, policy => policy.RequireRole(FailureAnalyticsRoles));
        options.AddPolicy(EnergyAnalyticsRead, policy => policy.RequireRole(EnergyAnalyticsRoles));
        options.AddPolicy(AlertsRead, policy => policy.RequireRole(AlertsRoles));
        options.AddPolicy(MaintenanceRead, policy => policy.RequireRole(MaintenanceReadRoles));
        options.AddPolicy(MaintenanceManage, policy => policy.RequireRole(MaintenanceManageRoles));
        options.AddPolicy(SettingsManage, policy => policy.RequireRole(AdministratorOnly));
    }
}