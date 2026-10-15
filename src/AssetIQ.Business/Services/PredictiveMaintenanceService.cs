using AssetIQ.Business.Analysis;
using AssetIQ.Business.DTOs;
using AssetIQ.Business.Interfaces;

namespace AssetIQ.Business.Services;

public sealed class PredictiveMaintenanceService : IPredictiveMaintenanceService
{
    private readonly IRollingMillDatasetAccessor _datasetAccessor;

    public PredictiveMaintenanceService(IRollingMillDatasetAccessor datasetAccessor)
    {
        _datasetAccessor = datasetAccessor;
    }

    public async Task<PredictiveMaintenanceSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await _datasetAccessor.GetSnapshotAsync(cancellationToken);
        if (!snapshot.IsValid || snapshot.Equipment.Count == 0)
        {
            return new PredictiveMaintenanceSummaryDto
            {
                IsDatasetValid = snapshot.IsValid,
                DataSourceLabel = snapshot.SourceLabel
            };
        }

        var equipment = snapshot.Equipment;

        var riskCounts = equipment
            .GroupBy(e => e.RiskLevel, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => SeverityRank(g.Key))
            .Select(g => new GroupMetricDto { Label = g.Key, Value = g.Count(), Unit = "assets" })
            .ToList();

        var avgVibration = equipment
            .GroupBy(e => e.RiskLevel, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => SeverityRank(g.Key))
            .Select(g => new GroupMetricDto
            {
                Label = g.Key,
                Value = Math.Round(g.Average(x => x.VibrationMmS), 2),
                Unit = "mm/s (mean in dataset row)"
            })
            .ToList();

        var avgMotorTemp = equipment
            .GroupBy(e => e.RiskLevel, StringComparer.OrdinalIgnoreCase)
            .Select(g => new GroupMetricDto
            {
                Label = g.Key,
                Value = Math.Round(g.Average(x => x.MotorTempC), 2),
                Unit = "°C (mean in dataset row)"
            })
            .OrderByDescending(x => SeverityRank(x.Label))
            .ToList();

        var flagged = equipment
            .Where(e => IsFlagged(e))
            .OrderBy(e => e.HealthScore)
            .ThenByDescending(e => SeverityRank(e.RiskLevel))
            .ThenBy(e => e.EquipmentCode, StringComparer.OrdinalIgnoreCase)
            .Select(MapAssessment)
            .ToList();

        return new PredictiveMaintenanceSummaryDto
        {
            IsDatasetValid = true,
            DataSourceLabel = snapshot.SourceLabel,
            TotalAssets = equipment.Count,
            FlaggedAssetCount = flagged.Count,
            RiskLevelCounts = riskCounts,
            AvgVibrationByRisk = avgVibration,
            AvgMotorTempByRisk = avgMotorTemp,
            FlaggedAssets = flagged
        };
    }

    private static bool IsFlagged(EquipmentRecordDto record) =>
        !record.MaintenanceRecommendation.Equals("Monitor", StringComparison.OrdinalIgnoreCase);

    private static ConditionAssessmentDto MapAssessment(EquipmentRecordDto record) => new()
    {
        Equipment = record,
        AssessmentSource = DatasetConditionExplainer.AssessmentSource,
        Explanation = DatasetConditionExplainer.BuildSummary(record),
        RuleFactors = DatasetConditionExplainer.BuildRuleFactors(record),
        IsFlaggedForReview = true
    };

    private static int SeverityRank(string riskLevel) => riskLevel.ToUpperInvariant() switch
    {
        "CRITICAL" => 3,
        "HIGH" => 2,
        "MEDIUM" => 1,
        _ => 0
    };
}
