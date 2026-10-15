using AssetIQ.Business.DTOs;
using AssetIQ.Business.Interfaces;

namespace AssetIQ.Business.Services;

public sealed class MaintenanceWorkOrderService : IMaintenanceWorkOrderService
{
    public const string WorkOrderSource = "Rolling Mill CSV import (Maintenance_Flag field)";

    private readonly IRollingMillDatasetAccessor _datasetAccessor;

    public MaintenanceWorkOrderService(IRollingMillDatasetAccessor datasetAccessor)
    {
        _datasetAccessor = datasetAccessor;
    }

    public async Task<IReadOnlyList<MaintenanceWorkOrderDto>> GetWorkOrdersAsync(
        MaintenanceWorkOrderQuery query,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await _datasetAccessor.GetSnapshotAsync(cancellationToken);
        if (!snapshot.IsValid)
        {
            return Array.Empty<MaintenanceWorkOrderDto>();
        }

        IEnumerable<MaintenanceWorkOrderDto> results = snapshot.Equipment.Select(MapWorkOrder);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            results = results.Where(w =>
                w.EquipmentCode.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                w.EquipmentTypeKey.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                w.Recommendation.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            results = results.Where(w =>
                w.Status.Equals(query.Status, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.Priority))
        {
            results = results.Where(w =>
                w.Priority.Equals(query.Priority, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.Section))
        {
            results = results.Where(w =>
                w.Section.Equals(query.Section, StringComparison.OrdinalIgnoreCase));
        }

        return results
            .OrderByDescending(w => PriorityRank(w.Priority))
            .ThenBy(w => w.MaintenanceDaysAgo)
            .ThenBy(w => w.EquipmentCode, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<IReadOnlyList<string>> GetStatusesAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await _datasetAccessor.GetSnapshotAsync(cancellationToken);
        return snapshot.IsValid
            ? snapshot.Equipment.Select(MapWorkOrder).Select(w => w.Status).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList()
            : Array.Empty<string>();
    }

    public async Task<IReadOnlyList<string>> GetPrioritiesAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await _datasetAccessor.GetSnapshotAsync(cancellationToken);
        return snapshot.IsValid
            ? snapshot.Equipment.Select(e => e.RiskLevel).Distinct(StringComparer.OrdinalIgnoreCase).OrderByDescending(PriorityRank).ThenBy(x => x).ToList()
            : Array.Empty<string>();
    }

    public async Task<IReadOnlyList<string>> GetSectionsAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await _datasetAccessor.GetSnapshotAsync(cancellationToken);
        return snapshot.IsValid
            ? snapshot.Equipment.Select(e => e.Section).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList()
            : Array.Empty<string>();
    }

    private static MaintenanceWorkOrderDto MapWorkOrder(EquipmentRecordDto record) => new()
    {
        WorkOrderId = $"WO-{record.EquipmentCode}",
        EquipmentCode = record.EquipmentCode,
        EquipmentTypeKey = record.EquipmentTypeKey,
        Section = record.Section,
        Priority = record.RiskLevel,
        Status = MapStatus(record.MaintenanceRecommendation),
        Source = WorkOrderSource,
        Recommendation = record.MaintenanceRecommendation,
        MaintenanceDaysAgo = record.MaintenanceDaysAgo,
        RiskLevel = record.RiskLevel,
        ObservedAtLocal = record.ObservedAtLocal
    };

    public static string MapStatus(string maintenanceFlag) => maintenanceFlag switch
    {
        var f when f.Equals("Immediate Maintenance", StringComparison.OrdinalIgnoreCase)
            => "Open — urgent (dataset)",
        var f when f.Equals("Plan Maintenance", StringComparison.OrdinalIgnoreCase)
            => "Planned (dataset)",
        var f when f.Equals("Monitor", StringComparison.OrdinalIgnoreCase)
            => "Monitoring (dataset)",
        _ => maintenanceFlag
    };

    private static int PriorityRank(string priority) => priority.ToUpperInvariant() switch
    {
        "CRITICAL" => 3,
        "HIGH" => 2,
        "MEDIUM" => 1,
        _ => 0
    };
}
