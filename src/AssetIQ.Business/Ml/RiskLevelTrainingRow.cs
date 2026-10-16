using Microsoft.ML.Data;

namespace AssetIQ.Business.Ml;

internal class RiskLevelTrainingRow
{
    public string EquipmentTypeKey { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public float MotorTempC { get; set; }
    public float BearingTempC { get; set; }
    public float VibrationMmS { get; set; }
    public float MotorCurrentA { get; set; }
    public float PowerKw { get; set; }
    public float EnergyKwh { get; set; }
    public float HydraulicPressureBar { get; set; }
    public float OilPressureBar { get; set; }
    public float RollSpeedMpm { get; set; }
    public float RollForceKn { get; set; }
    public float OperatingHours { get; set; }
    public float ProductionTons { get; set; }
    public float MaintenanceDaysAgo { get; set; }
    public float FailureCountLastYear { get; set; }

    [ColumnName("Label")]
    public string Label { get; set; } = string.Empty;
}

internal sealed class RiskLevelScoredRow : RiskLevelTrainingRow
{
    [ColumnName("PredictedLabel")]
    public string PredictedLabel { get; set; } = string.Empty;

    [ColumnName("Score")]
    public float[] Score { get; set; } = Array.Empty<float>();
}
