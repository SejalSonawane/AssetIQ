namespace AssetIQ.Data.Entities;

public enum CsvImportRowClassification
{
    Unclassified,
    Original,
    SimulatedDemonstration
}

public enum CsvImportRowStatus
{
    Pending,
    Accepted,
    Rejected
}

public sealed class CsvImportBatchRow
{
    public Guid Id { get; set; }
    public Guid ImportBatchId { get; set; }
    public int SourceRowNumber { get; set; }
    public CsvImportRowClassification Classification { get; set; }
    public CsvImportRowStatus Status { get; set; }
    public string? ValidationMessage { get; set; }
    public CsvImportBatch? ImportBatch { get; set; }
}