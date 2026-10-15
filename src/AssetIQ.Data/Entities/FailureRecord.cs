namespace AssetIQ.Data.Entities;

public class FailureRecord
{
    public Guid Id { get; set; }
    public Guid EquipmentId { get; set; }
    public string FailureMode { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; }
    public decimal? DowntimeHours { get; set; }
    public decimal? EstimatedCostImpact { get; set; }
    public Equipment? Equipment { get; set; }
}
