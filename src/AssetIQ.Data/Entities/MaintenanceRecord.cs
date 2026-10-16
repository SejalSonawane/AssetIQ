namespace AssetIQ.Data.Entities;

public class MaintenanceRecord
{
    public Guid Id { get; set; }
    public Guid EquipmentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string? AssignedTo { get; set; }
    public DateTime? ScheduledAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? CompletionNotes { get; set; }
    public string Source { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public Guid? SourceRowId { get; set; }
    public Equipment? Equipment { get; set; }
    public CsvImportBatchRow? SourceRow { get; set; }
}
