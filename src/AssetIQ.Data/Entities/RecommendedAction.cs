namespace AssetIQ.Data.Entities;

public class RecommendedAction
{
    public Guid Id { get; set; }
    public Guid EquipmentId { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public Guid? SourceRowId { get; set; }
    public Equipment? Equipment { get; set; }
    public CsvImportBatchRow? SourceRow { get; set; }
}
