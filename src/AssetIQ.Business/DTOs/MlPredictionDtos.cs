namespace AssetIQ.Business.DTOs;

public sealed class RiskModelEvaluationDto
{
    public bool IsAvailable { get; init; }
    public string? UnavailableReason { get; init; }
    public string ModelIdentifier { get; init; } = "RiskLevel.SdcaMaximumEntropy";
    public string ModelVersion { get; init; } = "1.0";
    public string TargetKey { get; init; } = "Risk_Level";
    public string TargetDescription { get; init; } = string.Empty;
    public int TrainingRowCount { get; init; }
    public int HoldoutRowCount { get; init; }
    public double MacroAccuracy { get; init; }
    public double MicroAccuracy { get; init; }
    public double LogLoss { get; init; }
    public IReadOnlyList<RiskClassMetricDto> PerClassMetrics { get; init; } = Array.Empty<RiskClassMetricDto>();
    public IReadOnlyList<string> FeatureKeys { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> ExcludedLeakageFields { get; init; } = Array.Empty<string>();
    public string Limitations { get; init; } = string.Empty;
}

public sealed class RiskClassMetricDto
{
    public string ClassName { get; init; } = string.Empty;
    public double Precision { get; init; }
    public double Recall { get; init; }
    public double F1Score { get; init; }
}

public sealed class EquipmentRiskPredictionDto
{
    public string EquipmentCode { get; init; } = string.Empty;
    public string DatasetRiskLevel { get; init; } = string.Empty;
    public string PredictedRiskLevel { get; init; } = string.Empty;
    public bool MatchesDatasetLabel { get; init; }
    public float Confidence { get; init; }
    public IReadOnlyList<RiskScoreFactorDto> SupportingFactors { get; init; } = Array.Empty<RiskScoreFactorDto>();
}

public sealed class RiskScoreFactorDto
{
    public string Factor { get; init; } = string.Empty;
    public float Score { get; init; }
}
