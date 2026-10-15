using AssetIQ.Data.Import;

namespace AssetIQ.Business.Validation;

public static class RollingMillDatasetValidator
{
    private static readonly HashSet<string> AllowedRiskLevels = new(StringComparer.OrdinalIgnoreCase)
    {
        "Critical", "High", "Medium"
    };

    private static readonly HashSet<string> AllowedMaintenanceFlags = new(StringComparer.OrdinalIgnoreCase)
    {
        "Immediate Maintenance", "Plan Maintenance", "Monitor"
    };

    public static IReadOnlyList<DatasetValidationIssue> ValidateRecords(IReadOnlyList<RollingMillCsvRecord> records)
    {
        var issues = new List<DatasetValidationIssue>();

        if (records.Count == 0)
        {
            issues.Add(new DatasetValidationIssue { Message = "No data rows were parsed from the CSV.", Severity = "Error" });
            return issues;
        }

        var duplicateIds = records
            .GroupBy(r => r.EquipmentId, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        foreach (var id in duplicateIds)
        {
            issues.Add(new DatasetValidationIssue
            {
                Field = "Equipment_ID",
                Message = $"Duplicate Equipment_ID '{id}' detected.",
                Severity = "Error"
            });
        }

        foreach (var record in records)
        {
            if (!AllowedRiskLevels.Contains(record.RiskLevel))
            {
                issues.Add(new DatasetValidationIssue
                {
                    RowNumber = record.SourceRowNumber,
                    Field = "Risk_Level",
                    Message = $"Unsupported risk level '{record.RiskLevel}'.",
                    Severity = "Error"
                });
            }

            if (!AllowedMaintenanceFlags.Contains(record.MaintenanceFlag))
            {
                issues.Add(new DatasetValidationIssue
                {
                    RowNumber = record.SourceRowNumber,
                    Field = "Maintenance_Flag",
                    Message = $"Unsupported maintenance flag '{record.MaintenanceFlag}'.",
                    Severity = "Error"
                });
            }

            if (record.ProductionTons <= 0)
            {
                issues.Add(new DatasetValidationIssue
                {
                    RowNumber = record.SourceRowNumber,
                    Field = "Production_Tons",
                    Message = "Production_Tons must be greater than zero for energy intensity validation.",
                    Severity = "Error"
                });
            }
            else
            {
                var calculatedIntensity = record.EnergyKwh / record.ProductionTons;
                var relativeDiff = Math.Abs(calculatedIntensity - record.EnergyIntensity);
                var tolerance = Math.Max(0.01m, Math.Abs(record.EnergyIntensity) * 0.001m);
                if (relativeDiff > tolerance)
                {
                    issues.Add(new DatasetValidationIssue
                    {
                        RowNumber = record.SourceRowNumber,
                        Field = "Energy_Intensity",
                        Message =
                            $"Energy_Intensity ({record.EnergyIntensity}) does not match Energy_kWh/Production_Tons ({calculatedIntensity:F4}).",
                        Severity = "Error"
                    });
                }
            }

            var expectedRisk = ClassifyRiskFromHealthScore(record.HealthScore);
            if (!string.Equals(expectedRisk, record.RiskLevel, StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(new DatasetValidationIssue
                {
                    RowNumber = record.SourceRowNumber,
                    Field = "Risk_Level",
                    Message =
                        $"Risk_Level '{record.RiskLevel}' does not match health-score band expectation '{expectedRisk}' for Health_Score {record.HealthScore}.",
                    Severity = "Error"
                });
            }
        }

        return issues;
    }

    public static DatasetQualityReport BuildQualityReport(IReadOnlyList<RollingMillCsvRecord> records)
    {
        var riskAligned = records.All(r =>
            string.Equals(ClassifyRiskFromHealthScore(r.HealthScore), r.RiskLevel, StringComparison.OrdinalIgnoreCase));

        var notes = new List<string>
        {
            "Labels and maintenance recommendations come from the imported dataset, not live SCADA or an ML model.",
            "Risk_Level values follow fixed Health_Score bands in this file (≤40 Critical, 40–60 High, >60 Medium).",
            "Estimated downtime and cost values use discrete tiers tied to risk level, not row-specific engineering estimates.",
            "Each Equipment_ID appears once — this is a cross-sectional snapshot, not repeated time-series readings per asset.",
            "A Health_Score of zero indicates the lowest band in this dataset, not a confirmed failure event."
        };

        return new DatasetQualityReport
        {
            TotalRecords = records.Count,
            CriticalRiskCount = records.Count(r => r.RiskLevel.Equals("Critical", StringComparison.OrdinalIgnoreCase)),
            HighRiskCount = records.Count(r => r.RiskLevel.Equals("High", StringComparison.OrdinalIgnoreCase)),
            MediumRiskCount = records.Count(r => r.RiskLevel.Equals("Medium", StringComparison.OrdinalIgnoreCase)),
            ZeroHealthScoreCount = records.Count(r => r.HealthScore == 0),
            UniqueDowntimeTierCount = records.Select(r => r.EstimatedDowntimeHours).Distinct().Count(),
            UniqueCostTierCount = records.Select(r => r.EstimatedCostImpactInr).Distinct().Count(),
            OneRowPerEquipmentId = records.Select(r => r.EquipmentId).Distinct(StringComparer.OrdinalIgnoreCase).Count() == records.Count,
            RiskAlignedToHealthScoreBands = riskAligned,
            LimitationNotes = notes
        };
    }

    internal static string ClassifyRiskFromHealthScore(decimal healthScore) =>
        healthScore <= 40 ? "Critical" : healthScore <= 60 ? "High" : "Medium";
}
