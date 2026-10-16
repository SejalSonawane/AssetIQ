namespace AssetIQ.Business.DTOs;

public class DashboardSummaryDto
{
    public string DataSourceLabel { get; init; } = string.Empty;
    public bool IsDatasetLoaded { get; init; }
    public bool IsDatasetValid { get; init; }
    public int TotalEquipment { get; init; }
    public int CriticalRiskCount { get; init; }
    public int HighRiskCount { get; init; }
    public int MediumRiskCount { get; init; }
    public int ActiveAlerts { get; init; }
    public decimal EstimatedDowntimeExposureHours { get; init; }
    public decimal EnergyConsumptionMwh { get; init; }
    public decimal? EnergyIntensityKwhPerUnit { get; init; }
    public IReadOnlyList<RiskBucketDto> RiskDistribution { get; init; } = Array.Empty<RiskBucketDto>();
    public IReadOnlyList<RiskBucketDto> HealthOverview { get; init; } = Array.Empty<RiskBucketDto>();
    public IReadOnlyList<RiskBucketDto> MaintenanceRecommendations { get; init; } = Array.Empty<RiskBucketDto>();
    public IReadOnlyList<TrendPointDto> EnergyTrend { get; init; } = Array.Empty<TrendPointDto>();
    public Validation.DatasetQualityReport? QualityReport { get; init; }
    public IReadOnlyList<RecentActivityDto> RecentMaintenance { get; init; } = Array.Empty<RecentActivityDto>();
    public IReadOnlyList<PriorityAssetDto> PriorityAssets { get; init; } = Array.Empty<PriorityAssetDto>();
    public RiskModelEvaluationDto? RiskModelEvaluation { get; init; }
}

public class RiskBucketDto
{
    public string Label { get; init; } = string.Empty;
    public int Count { get; init; }
}

public class TrendPointDto
{
    public string Label { get; init; } = string.Empty;
    public decimal Value { get; init; }
}

public class RecentActivityDto
{
    public string EquipmentName { get; init; } = string.Empty;
    public string Activity { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime WhenLocal { get; init; }
}

public class PriorityAssetDto
{
    public string EquipmentCode { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string RiskLevel { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
}
