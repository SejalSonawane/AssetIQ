namespace AssetIQ.Business.DTOs;

public sealed class GroupMetricDto
{
    public string Label { get; init; } = string.Empty;
    public decimal Value { get; init; }
    public string? Unit { get; init; }
    public int Count { get; init; }
}

public sealed class ProductionEnergyPointDto
{
    public string EquipmentCode { get; init; } = string.Empty;
    public decimal ProductionTons { get; init; }
    public decimal EnergyKwh { get; init; }
    public string Section { get; init; } = string.Empty;
}

public sealed class EquipmentEnergyRowDto
{
    public string EquipmentCode { get; init; } = string.Empty;
    public string EquipmentTypeKey { get; init; } = string.Empty;
    public string Section { get; init; } = string.Empty;
    public decimal EnergyKwh { get; init; }
    public decimal ProductionTons { get; init; }
    public decimal EnergyIntensityKwhPerTon { get; init; }
    public decimal PowerKw { get; init; }
}

public sealed class EnergyAnalyticsSummaryDto
{
    public bool IsDatasetValid { get; init; }
    public string DataSourceLabel { get; init; } = string.Empty;
    public decimal TotalEnergyKwh { get; init; }
    public decimal TotalProductionTons { get; init; }
    public decimal? PlantIntensityKwhPerTon { get; init; }
    public IReadOnlyList<GroupMetricDto> EnergyKwhBySection { get; init; } = Array.Empty<GroupMetricDto>();
    public IReadOnlyList<GroupMetricDto> EnergyKwhByEquipmentType { get; init; } = Array.Empty<GroupMetricDto>();
    public IReadOnlyList<GroupMetricDto> AvgIntensityBySection { get; init; } = Array.Empty<GroupMetricDto>();
    public IReadOnlyList<ProductionEnergyPointDto> ProductionVsEnergyPoints { get; init; } = Array.Empty<ProductionEnergyPointDto>();
    public IReadOnlyList<EquipmentEnergyRowDto> TopEnergyConsumers { get; init; } = Array.Empty<EquipmentEnergyRowDto>();
}

public sealed class ConditionAssessmentDto
{
    public EquipmentRecordDto Equipment { get; init; } = new();
    public string AssessmentSource { get; init; } = string.Empty;
    public string Explanation { get; init; } = string.Empty;
    public IReadOnlyList<string> RuleFactors { get; init; } = Array.Empty<string>();
    public bool IsFlaggedForReview { get; init; }
}

public sealed class PredictiveMaintenanceSummaryDto
{
    public bool IsDatasetValid { get; init; }
    public string DataSourceLabel { get; init; } = string.Empty;
    public int TotalAssets { get; init; }
    public int FlaggedAssetCount { get; init; }
    public IReadOnlyList<GroupMetricDto> RiskLevelCounts { get; init; } = Array.Empty<GroupMetricDto>();
    public IReadOnlyList<GroupMetricDto> AvgVibrationByRisk { get; init; } = Array.Empty<GroupMetricDto>();
    public IReadOnlyList<GroupMetricDto> AvgMotorTempByRisk { get; init; } = Array.Empty<GroupMetricDto>();
    public IReadOnlyList<ConditionAssessmentDto> FlaggedAssets { get; init; } = Array.Empty<ConditionAssessmentDto>();
}

public sealed class FailureImpactTierDto
{
    public string RiskLevel { get; init; } = string.Empty;
    public decimal EstimatedDowntimeHours { get; init; }
    public decimal EstimatedCostImpactInr { get; init; }
    public int EquipmentCount { get; init; }
    public decimal SumFailureCounts { get; init; }
}

public sealed class FailureAnalyticsSummaryDto
{
    public bool IsDatasetValid { get; init; }
    public string DataSourceLabel { get; init; } = string.Empty;
    public decimal TotalFailureCountSum { get; init; }
    public decimal AverageFailureCountPerAsset { get; init; }
    public IReadOnlyList<GroupMetricDto> FailureCountSumByEquipmentType { get; init; } = Array.Empty<GroupMetricDto>();
    public IReadOnlyList<GroupMetricDto> FailureCountSumBySection { get; init; } = Array.Empty<GroupMetricDto>();
    public IReadOnlyList<GroupMetricDto> AssetCountByEquipmentType { get; init; } = Array.Empty<GroupMetricDto>();
    public IReadOnlyList<FailureImpactTierDto> ImpactTiers { get; init; } = Array.Empty<FailureImpactTierDto>();
    public IReadOnlyList<EquipmentRecordDto> HighestFailureCountAssets { get; init; } = Array.Empty<EquipmentRecordDto>();
}

public sealed class MaintenanceWorkOrderDto
{
    public string WorkOrderId { get; init; } = string.Empty;
    public string EquipmentCode { get; init; } = string.Empty;
    public string EquipmentTypeKey { get; init; } = string.Empty;
    public string Section { get; init; } = string.Empty;
    public string Priority { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Source { get; init; } = string.Empty;
    public string Recommendation { get; init; } = string.Empty;
    public decimal MaintenanceDaysAgo { get; init; }
    public string RiskLevel { get; init; } = string.Empty;
    public DateTime ObservedAtLocal { get; init; }
}

public sealed record MaintenanceApplicationActionDto
{
    public Guid Id { get; init; }
    public string EquipmentCode { get; init; } = string.Empty;
    public string EquipmentTypeKey { get; init; } = string.Empty;
    public string Section { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAtLocal { get; init; }
    public DateTime? CompletedAtLocal { get; init; }
}
