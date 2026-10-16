namespace AssetIQ.Data.Entities;

public sealed class Plant
{
    public Guid Id { get; set; }
    public string PlantCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string TimeZoneId { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public ICollection<Equipment> Equipment { get; set; } = new List<Equipment>();
    public ICollection<CsvImportBatch> ImportBatches { get; set; } = new List<CsvImportBatch>();
}