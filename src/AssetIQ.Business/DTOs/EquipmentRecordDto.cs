namespace AssetIQ.Business.DTOs;

/// <summary>
/// Equipment snapshot for UI and analytics, mapped from an imported plant dataset row.
/// </summary>
public sealed class EquipmentRecordDto
{
    public int SourceRowNumber { get; init; }
    public string EquipmentCode { get; init; } = string.Empty;
    public string EquipmentTypeKey { get; init; } = string.Empty;
    public string Section { get; init; } = string.Empty;
    public string PlantName { get; init; } = string.Empty;
    public DateTime ObservedAtLocal { get; init; }
    public decimal HealthScore { get; init; }
    public string RiskLevel { get; init; } = string.Empty;
    public string MaintenanceRecommendation { get; init; } = string.Empty;
    public decimal MotorTempC { get; init; }
    public decimal BearingTempC { get; init; }
    public decimal VibrationMmS { get; init; }
    public decimal MotorCurrentA { get; init; }
    public decimal PowerKw { get; init; }
    public decimal EnergyKwh { get; init; }
    public decimal HydraulicPressureBar { get; init; }
    public decimal OilPressureBar { get; init; }
    public decimal RollSpeedMpm { get; init; }
    public decimal RollForceKn { get; init; }
    public decimal OperatingHours { get; init; }
    public decimal ProductionTons { get; init; }
    public decimal MaintenanceDaysAgo { get; init; }
    public decimal FailureCountLastYear { get; init; }
    public decimal EnergyIntensityKwhPerTon { get; init; }
    public decimal EstimatedDowntimeHours { get; init; }
    public decimal EstimatedCostImpactInr { get; init; }
}
