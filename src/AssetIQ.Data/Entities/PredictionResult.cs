namespace AssetIQ.Data.Entities;

public sealed class PredictionResult
{
    public Guid Id { get; set; }
    public Guid EquipmentId { get; set; }
    public string ModelIdentifier { get; set; } = string.Empty;
    public string ModelVersion { get; set; } = string.Empty;
    public string TargetKey { get; set; } = string.Empty;
    public DateTime PredictedAtUtc { get; set; }
    public int? PredictionHorizonSeconds { get; set; }
    public string? PredictedClass { get; set; }
    public decimal? PredictedValue { get; set; }
    public decimal? Score { get; set; }
    public decimal? Probability { get; set; }
    public string? InputDataMetadataJson { get; set; }
    public string? ExplanationMetadataJson { get; set; }
    public Equipment? Equipment { get; set; }
}