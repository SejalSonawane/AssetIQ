using AssetIQ.Web.ViewModels;

namespace AssetIQ.Web.Infrastructure;

public static class ModulePageFactory
{
    public static ModulePageViewModel Create(string moduleKey) => moduleKey switch
    {
        "equipment" => new ModulePageViewModel
        {
            ModuleKey = moduleKey,
            Title = "Equipment",
            PageHeading = "Equipment Management",
            Description = "Generic equipment registry with configurable types, operating status, health scores, and history views.",
            PlannedCapabilities =
            [
                "Search, filter, sort, and paginate equipment",
                "Equipment detail with measurements and limits",
                "Maintenance and failure history",
                "Energy context where applicable"
            ]
        },
        "predictive" => new ModulePageViewModel
        {
            ModuleKey = moduleKey,
            Title = "Predictive Maintenance",
            PageHeading = "Predictive Maintenance",
            Description = "Condition monitoring, trend analysis, and rule-based risk flags. ML predictions will be integrated only after separate validation.",
            PlannedCapabilities =
            [
                "Measurement trends (temperature, vibration, current, etc.)",
                "Risk classification with explanations",
                "Recommended inspections and actions",
                "Historical failure context"
            ]
        },
        "failure" => new ModulePageViewModel
        {
            ModuleKey = moduleKey,
            Title = "Failure Analytics",
            PageHeading = "Failure Analytics",
            Description = "Failure history, repeat-failure patterns, and downtime or cost impact when supported by records.",
            PlannedCapabilities =
            [
                "Failures by equipment type and section",
                "Downtime and cost-impact estimates",
                "Reliability metrics when definitions and data allow"
            ]
        },
        "energy" => new ModulePageViewModel
        {
            ModuleKey = moduleKey,
            Title = "Energy Analytics",
            PageHeading = "Energy Analytics",
            Description = "Power and energy trends with production-normalized intensity. High consumption is not treated as inefficiency without context.",
            PlannedCapabilities =
            [
                "Consumption by equipment and section",
                "Energy intensity vs. production",
                "Period comparisons and anomaly indicators"
            ]
        },
        "maintenance" => new ModulePageViewModel
        {
            ModuleKey = moduleKey,
            Title = "Maintenance",
            PageHeading = "Maintenance Management",
            Description = "Work records, priorities, assignments, and closure status with a simple, extendable workflow.",
            PlannedCapabilities =
            [
                "Maintenance record list and detail",
                "Status, priority, and owner",
                "Recommended corrective actions",
                "Action history"
            ]
        },
        "alerts" => new ModulePageViewModel
        {
            ModuleKey = moduleKey,
            Title = "Alerts",
            PageHeading = "Alerts & Recommendations",
            Description = "Operational alerts distinct from confirmed failures, with severity, reason, and recommended next steps.",
            PlannedCapabilities =
            [
                "Alert list with acknowledgement and resolution",
                "Links to affected equipment",
                "Recommended next steps"
            ]
        },
        "settings" => new ModulePageViewModel
        {
            ModuleKey = moduleKey,
            Title = "Settings",
            PageHeading = "Settings",
            Description = "Platform configuration placeholder. Meaningful settings will be added as integrations (database, ML, notifications) are approved.",
            PlannedCapabilities =
            [
                "Equipment type and measurement configuration",
                "Operating limits and alert rules",
                "Integration endpoints (future)"
            ]
        },
        _ => new ModulePageViewModel
        {
            ModuleKey = moduleKey,
            Title = "Module",
            PageHeading = "Module",
            Description = "Placeholder module.",
            PlannedCapabilities = []
        }
    };
}
