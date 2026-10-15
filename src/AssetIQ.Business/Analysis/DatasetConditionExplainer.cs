using AssetIQ.Business.DTOs;

namespace AssetIQ.Business.Analysis;

/// <summary>
/// Transparent, rule-based explanations using dataset fields only (not ML).
/// </summary>
public static class DatasetConditionExplainer
{
    public const string AssessmentSource = "Dataset rule bands (Health_Score → Risk_Level / Maintenance_Flag)";

    public static string BuildSummary(EquipmentRecordDto record)
    {
        var healthNote = record.HealthScore == 0
            ? "Health_Score is 0 (lowest band in this dataset)."
            : $"Health_Score is {record.HealthScore:F2} (maps to {record.RiskLevel} per dataset bands).";

        return $"{healthNote} Observed vibration {record.VibrationMmS:F2} mm/s, motor temp {record.MotorTempC:F0} °C, " +
               $"bearing temp {record.BearingTempC:F0} °C, motor current {record.MotorCurrentA:F0} A. " +
               "Not a confirmed failure; not an ML prediction.";
    }

    public static IReadOnlyList<string> BuildRuleFactors(EquipmentRecordDto record)
    {
        var factors = new List<string>
        {
            $"Risk_Level = {record.RiskLevel} (dataset label)",
            $"Maintenance_Flag = {record.MaintenanceRecommendation} (dataset label)",
            $"Health_Score band: {DescribeHealthBand(record.HealthScore)}"
        };

        if (record.FailureCountLastYear > 0)
        {
            factors.Add($"Failure_Count_Last_Year = {record.FailureCountLastYear:F0} (historical count field in CSV)");
        }

        if (record.MaintenanceDaysAgo >= 90)
        {
            factors.Add($"Maintenance_Days_Ago = {record.MaintenanceDaysAgo:F0} (≥ 90 days in dataset row)");
        }

        return factors;
    }

    public static string DescribeHealthBand(decimal healthScore) => healthScore switch
    {
        <= 0 => "≤ 0 / zero (Critical band in file)",
        <= 40 => "0.01–40 (Critical band in file)",
        <= 60 => "40.01–60 (High band in file)",
        _ => "> 60 (Medium band in file)"
    };
}
