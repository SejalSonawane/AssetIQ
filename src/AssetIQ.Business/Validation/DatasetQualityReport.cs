namespace AssetIQ.Business.Validation;

/// <summary>
/// Documented limitations observed in the Rolling Mill CSV (revalidated at load time).
/// </summary>
public sealed class DatasetQualityReport
{
    public int TotalRecords { get; init; }
    public int CriticalRiskCount { get; init; }
    public int HighRiskCount { get; init; }
    public int MediumRiskCount { get; init; }
    public int ZeroHealthScoreCount { get; init; }
    public int UniqueDowntimeTierCount { get; init; }
    public int UniqueCostTierCount { get; init; }
    public bool OneRowPerEquipmentId { get; init; }
    public bool RiskAlignedToHealthScoreBands { get; init; }
    public IReadOnlyList<string> LimitationNotes { get; init; } = Array.Empty<string>();
}
