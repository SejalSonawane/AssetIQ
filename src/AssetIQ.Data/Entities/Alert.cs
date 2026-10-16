namespace AssetIQ.Data.Entities;

public class Alert
{
    public Guid Id { get; set; }
    public Guid EquipmentId { get; set; }
    public string Severity { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string RecommendedNextStep { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime RaisedAtUtc { get; set; }
    public Guid? SourceRowId { get; set; }
    public Equipment? Equipment { get; set; }
    public CsvImportBatchRow? SourceRow { get; set; }
}
