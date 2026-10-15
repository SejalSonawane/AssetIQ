namespace AssetIQ.Data.Import;

/// <summary>
/// One validated row from the Rolling Mill CSV import schema.
/// </summary>
public sealed class RollingMillCsvRecord
{
    public int SourceRowNumber { get; init; }
    public DateTime TimestampLocal { get; init; }
    public string EquipmentId { get; init; } = string.Empty;
    public string EquipmentType { get; init; } = string.Empty;
    public string Section { get; init; } = string.Empty;
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
    public decimal EnergyIntensity { get; init; }
    public decimal HealthScore { get; init; }
    public string RiskLevel { get; init; } = string.Empty;
    public string MaintenanceFlag { get; init; } = string.Empty;
    public decimal EstimatedDowntimeHours { get; init; }
    public decimal EstimatedCostImpactInr { get; init; }
}
