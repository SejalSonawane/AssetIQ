using AssetIQ.Business.DTOs;

namespace AssetIQ.Web.ViewModels;

public sealed class EnergyAnalyticsViewModel
{
    public DatasetStatusViewModel DatasetStatus { get; init; } = new();
    public EnergyAnalyticsSummaryDto Summary { get; init; } = new();
}

public sealed class PredictiveMaintenanceViewModel
{
    public DatasetStatusViewModel DatasetStatus { get; init; } = new();
    public PredictiveMaintenanceSummaryDto Summary { get; init; } = new();
}

public sealed class FailureAnalyticsViewModel
{
    public DatasetStatusViewModel DatasetStatus { get; init; } = new();
    public FailureAnalyticsSummaryDto Summary { get; init; } = new();
}

public sealed class MaintenanceListViewModel
{
    public DatasetStatusViewModel DatasetStatus { get; init; } = new();
    public IReadOnlyList<MaintenanceWorkOrderDto> WorkOrders { get; init; } = Array.Empty<MaintenanceWorkOrderDto>();
    public IReadOnlyList<MaintenanceApplicationActionDto> Actions { get; init; } = Array.Empty<MaintenanceApplicationActionDto>();
    public IReadOnlyList<EquipmentRecordDto> ActionEquipment { get; init; } = Array.Empty<EquipmentRecordDto>();
    public string? Search { get; init; }
    public string? SelectedStatus { get; init; }
    public string? SelectedPriority { get; init; }
    public string? SelectedSection { get; init; }
    public IReadOnlyList<string> Statuses { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Priorities { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Sections { get; init; } = Array.Empty<string>();
}
