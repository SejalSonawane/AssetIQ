using AssetIQ.Business.DTOs;
using AssetIQ.Business.Interfaces;

namespace AssetIQ.Business.Services;

public sealed class AlertService : IAlertService
{
    private readonly IRollingMillDatasetAccessor _datasetAccessor;

    public AlertService(IRollingMillDatasetAccessor datasetAccessor)
    {
        _datasetAccessor = datasetAccessor;
    }

    public async Task<IReadOnlyList<AlertRecordDto>> GetAlertsAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await _datasetAccessor.GetSnapshotAsync(cancellationToken);
        if (!snapshot.IsValid)
        {
            return Array.Empty<AlertRecordDto>();
        }

        return snapshot.Equipment
            .OrderByDescending(e => SeverityRank(e.RiskLevel))
            .ThenBy(e => e.HealthScore)
            .ThenBy(e => e.EquipmentCode, StringComparer.OrdinalIgnoreCase)
            .Select(MapAlert)
            .ToList();
    }

    private static AlertRecordDto MapAlert(EquipmentRecordDto record)
    {
        var reason = BuildReason(record);
        return new AlertRecordDto
        {
            AlertId = $"DS-{record.EquipmentCode}",
            EquipmentCode = record.EquipmentCode,
            EquipmentTypeKey = record.EquipmentTypeKey,
            Section = record.Section,
            Severity = record.RiskLevel,
            Title = $"Dataset condition alert — {record.EquipmentCode}",
            Reason = reason,
            RecommendedNextStep = record.MaintenanceRecommendation,
            RaisedAtLocal = record.ObservedAtLocal,
            IsConfirmedFailure = false,
            Status = "Open (dataset recommendation)"
        };
    }

    private static string BuildReason(EquipmentRecordDto record)
    {
        var healthNote = record.HealthScore == 0
            ? "Health_Score is 0 (lowest band in this dataset)."
            : $"Health_Score is {record.HealthScore:F2} (dataset band maps to {record.RiskLevel}).";

        return $"{healthNote} Observed vibration {record.VibrationMmS:F2} mm/s, motor temperature {record.MotorTempC:F0} °C. " +
               "This is a dataset-derived condition flag, not a confirmed failure event.";
    }

    private static int SeverityRank(string severity) => severity.ToUpperInvariant() switch
    {
        "CRITICAL" => 3,
        "HIGH" => 2,
        "MEDIUM" => 1,
        _ => 0
    };
}
