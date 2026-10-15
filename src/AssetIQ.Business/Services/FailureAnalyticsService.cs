using AssetIQ.Business.DTOs;
using AssetIQ.Business.Interfaces;

namespace AssetIQ.Business.Services;

public sealed class FailureAnalyticsService : IFailureAnalyticsService
{
    private readonly IRollingMillDatasetAccessor _datasetAccessor;

    public FailureAnalyticsService(IRollingMillDatasetAccessor datasetAccessor)
    {
        _datasetAccessor = datasetAccessor;
    }

    public async Task<FailureAnalyticsSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await _datasetAccessor.GetSnapshotAsync(cancellationToken);
        if (!snapshot.IsValid || snapshot.Equipment.Count == 0)
        {
            return new FailureAnalyticsSummaryDto
            {
                IsDatasetValid = snapshot.IsValid,
                DataSourceLabel = snapshot.SourceLabel
            };
        }

        var equipment = snapshot.Equipment;
        var failureSum = equipment.Sum(e => e.FailureCountLastYear);

        var byType = equipment
            .GroupBy(e => e.EquipmentTypeKey, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Sum(x => x.FailureCountLastYear))
            .Select(g => new GroupMetricDto
            {
                Label = g.Key,
                Value = g.Sum(x => x.FailureCountLastYear),
                Unit = "sum of Failure_Count_Last_Year",
                Count = g.Count()
            })
            .ToList();

        var bySection = equipment
            .GroupBy(e => e.Section, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Sum(x => x.FailureCountLastYear))
            .Select(g => new GroupMetricDto
            {
                Label = g.Key,
                Value = g.Sum(x => x.FailureCountLastYear),
                Unit = "sum of Failure_Count_Last_Year",
                Count = g.Count()
            })
            .ToList();

        var assetCountByType = equipment
            .GroupBy(e => e.EquipmentTypeKey, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key)
            .Select(g => new GroupMetricDto { Label = g.Key, Value = g.Count(), Unit = "assets" })
            .ToList();

        var impactTiers = equipment
            .GroupBy(e => new { e.RiskLevel, e.EstimatedDowntimeHours, e.EstimatedCostImpactInr })
            .OrderByDescending(g => g.Key.EstimatedCostImpactInr)
            .Select(g => new FailureImpactTierDto
            {
                RiskLevel = g.Key.RiskLevel,
                EstimatedDowntimeHours = g.Key.EstimatedDowntimeHours,
                EstimatedCostImpactInr = g.Key.EstimatedCostImpactInr,
                EquipmentCount = g.Count(),
                SumFailureCounts = g.Sum(x => x.FailureCountLastYear)
            })
            .ToList();

        var highestFailure = equipment
            .OrderByDescending(e => e.FailureCountLastYear)
            .ThenBy(e => e.EquipmentCode, StringComparer.OrdinalIgnoreCase)
            .Take(15)
            .ToList();

        return new FailureAnalyticsSummaryDto
        {
            IsDatasetValid = true,
            DataSourceLabel = snapshot.SourceLabel,
            TotalFailureCountSum = failureSum,
            AverageFailureCountPerAsset = Math.Round(failureSum / equipment.Count, 2),
            FailureCountSumByEquipmentType = byType,
            FailureCountSumBySection = bySection,
            AssetCountByEquipmentType = assetCountByType,
            ImpactTiers = impactTiers,
            HighestFailureCountAssets = highestFailure
        };
    }
}
