namespace AssetIQ.Data.Entities;

public enum CsvImportBatchStatus
{
    Pending,
    Validated,
    Imported,
    Failed
}

public sealed class CsvImportBatch
{
    public Guid Id { get; set; }
    public Guid PlantId { get; set; }
    public string SourceFileName { get; set; } = string.Empty;
    public string SourceFileSha256 { get; set; } = string.Empty;
    public string MappingProfileKey { get; set; } = string.Empty;
    public string MappingProfileVersion { get; set; } = string.Empty;
    public CsvImportBatchStatus Status { get; set; }
    public int TotalRowCount { get; set; }
    public int AcceptedRowCount { get; set; }
    public int RejectedRowCount { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public Plant? Plant { get; set; }
    public ICollection<CsvImportBatchRow> Rows { get; set; } = new List<CsvImportBatchRow>();
}