using AssetIQ.Web.ViewModels;

namespace AssetIQ.Web.Infrastructure;

public static class AppNavigation
{
    public static IReadOnlyList<NavigationItem> Items { get; } =
    [
        new() { ModuleKey = "dashboard", Label = "Dashboard", Controller = "Dashboard", Icon = "bi-speedometer2" },
        new() { ModuleKey = "equipment", Label = "Equipment", Controller = "Equipment", Icon = "bi-gear-wide-connected" },
        new() { ModuleKey = "predictive", Label = "Predictive Maintenance", Controller = "PredictiveMaintenance", Icon = "bi-graph-up-arrow" },
        new() { ModuleKey = "failure", Label = "Failure Analytics", Controller = "FailureAnalytics", Icon = "bi-exclamation-triangle" },
        new() { ModuleKey = "energy", Label = "Energy Analytics", Controller = "EnergyAnalytics", Icon = "bi-lightning-charge" },
        new() { ModuleKey = "maintenance", Label = "Maintenance Management", Controller = "Maintenance", Icon = "bi-tools" },
        new() { ModuleKey = "alerts", Label = "Alerts", Controller = "Alerts", Icon = "bi-bell" },
        new() { ModuleKey = "settings", Label = "Settings", Controller = "Settings", Icon = "bi-sliders" }
    ];
}
