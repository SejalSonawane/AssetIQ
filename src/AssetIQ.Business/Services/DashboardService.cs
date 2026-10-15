using AssetIQ.Business.DTOs;
using AssetIQ.Business.Interfaces;

namespace AssetIQ.Business.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly IRollingMillDatasetAccessor _datasetAccessor;

    public DashboardService(IRollingMillDatasetAccessor datasetAccessor)
    {
        _datasetAccessor = datasetAccessor;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await _datasetAccessor.GetSnapshotAsync(cancellationToken);
        if (!snapshot.IsValid || snapshot.Equipment.Count == 0)
        {
            return new DashboardSummaryDto
            {
                DataSourceLabel = snapshot.SourceLabel,
                IsDatasetLoaded = snapshot.IsLoaded,
                IsDatasetValid = snapshot.IsValid
            };
        }

        var equipment = snapshot.Equipment;
        var quality = snapshot.QualityReport!;

        var totalEnergyKwh = equipment.Sum(e => e.EnergyKwh);
        var totalProductionTons = equipment.Sum(e => e.ProductionTons);
        decimal? avgIntensity = totalProductionTons > 0 ? totalEnergyKwh / totalProductionTons : null;

        var riskDistribution = new[]
            {
                ("Critical", quality.CriticalRiskCount),
                ("High", quality.HighRiskCount),
                ("Medium", quality.MediumRiskCount)
            }
            .Select(x => new RiskBucketDto { Label = x.Item1, Count = x.Item2 })
            .ToList();

        var energyByDay = equipment
            .GroupBy(e => e.ObservedAtLocal.Date)
            .OrderBy(g => g.Key)
            .Select(g => new TrendPointDto
            {
                Label = g.Key.ToString("MM-dd"),
                Value = Math.Round(g.Sum(x => x.EnergyKwh) / 1000m, 2)
            })
            .ToList();

        var maintenanceGroups = equipment
            .GroupBy(e => e.MaintenanceRecommendation, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .Select(g => new RiskBucketDto { Label = g.Key, Count = g.Count() })
            .ToList();

        var healthOverview = new[]
            {
                ("0 (lowest band)", equipment.Count(e => e.HealthScore == 0)),
                ("0.01–40", equipment.Count(e => e.HealthScore > 0 && e.HealthScore <= 40)),
                ("40.01–60", equipment.Count(e => e.HealthScore > 40 && e.HealthScore <= 60)),
                ("> 60", equipment.Count(e => e.HealthScore > 60))
            }
            .Select(x => new RiskBucketDto { Label = x.Item1, Count = x.Item2 })
            .ToList();

        var priorityAssets = equipment
            .OrderBy(e => e.HealthScore)
            .ThenByDescending(e => SeverityRank(e.RiskLevel))
            .Take(10)
            .Select(e => new PriorityAssetDto
            {
                EquipmentCode = e.EquipmentCode,
                Name = $"{e.EquipmentTypeKey} — {e.Section}",
                RiskLevel = e.RiskLevel,
                Reason = e.HealthScore == 0
                    ? "Health_Score 0 (dataset band); review recommended action — not a confirmed failure."
                    : $"Health_Score {e.HealthScore:F2}; {e.MaintenanceRecommendation} (dataset label)."
            })
            .ToList();

        var recentMaintenance = equipment
            .OrderBy(e => e.MaintenanceDaysAgo)
            .Take(8)
            .Select(e => new RecentActivityDto
            {
                EquipmentName = e.EquipmentCode,
                Activity = e.MaintenanceRecommendation,
                Status = $"{e.MaintenanceDaysAgo:F0} days since maintenance (dataset field)",
                WhenLocal = e.ObservedAtLocal
            })
            .ToList();

        return new DashboardSummaryDto
        {
            DataSourceLabel = snapshot.SourceLabel,
            IsDatasetLoaded = true,
            IsDatasetValid = true,
            TotalEquipment = equipment.Count,
            CriticalRiskCount = quality.CriticalRiskCount,
            HighRiskCount = quality.HighRiskCount,
            MediumRiskCount = quality.MediumRiskCount,
            ActiveAlerts = equipment.Count,
            EstimatedDowntimeExposureHours = equipment.Sum(e => e.EstimatedDowntimeHours),
            EnergyConsumptionMwh = Math.Round(totalEnergyKwh / 1000m, 2),
            EnergyIntensityKwhPerUnit = avgIntensity.HasValue ? Math.Round(avgIntensity.Value, 2) : null,
            RiskDistribution = riskDistribution,
            HealthOverview = healthOverview,
            MaintenanceRecommendations = maintenanceGroups,
            EnergyTrend = energyByDay,
            RecentMaintenance = recentMaintenance,
            PriorityAssets = priorityAssets,
            QualityReport = quality
        };
    }

    private static int SeverityRank(string riskLevel) => riskLevel.ToUpperInvariant() switch
    {
        "CRITICAL" => 3,
        "HIGH" => 2,
        "MEDIUM" => 1,
        _ => 0
    };
}
