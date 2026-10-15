namespace AssetIQ.Business.DTOs;

public sealed class AlertRecordDto
{
    public string AlertId { get; init; } = string.Empty;
    public string EquipmentCode { get; init; } = string.Empty;
    public string EquipmentTypeKey { get; init; } = string.Empty;
    public string Section { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
    public string RecommendedNextStep { get; init; } = string.Empty;
    public DateTime RaisedAtLocal { get; init; }
    public bool IsConfirmedFailure { get; init; }
    public string Status { get; init; } = "Open (dataset)";
}
