using AssetIQ.Business.DTOs;

namespace AssetIQ.Business.Interfaces;

public interface IEnergyAnalyticsService
{
    Task<EnergyAnalyticsSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
}

public interface IPredictiveMaintenanceService
{
    Task<PredictiveMaintenanceSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
}

public interface IFailureAnalyticsService
{
    Task<FailureAnalyticsSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
}

public sealed class MaintenanceWorkOrderQuery
{
    public string? Search { get; init; }
    public string? Status { get; init; }
    public string? Priority { get; init; }
    public string? Section { get; init; }
}

public interface IMaintenanceWorkOrderService
{
    Task<IReadOnlyList<MaintenanceWorkOrderDto>> GetWorkOrdersAsync(
        MaintenanceWorkOrderQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetStatusesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetPrioritiesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetSectionsAsync(CancellationToken cancellationToken = default);
}

public interface IMaintenanceActionService
{
    Task<MaintenanceApplicationActionDto> CreateActionAsync(
        string? equipmentCode,
        string? description,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MaintenanceApplicationActionDto>> GetActionsAsync(CancellationToken cancellationToken = default);
    Task<bool> CompleteActionAsync(Guid id, CancellationToken cancellationToken = default);
}
