namespace AssetIQ.Data.Entities;

public class EnergyRecord
{
    public Guid Id { get; set; }
    public Guid EquipmentId { get; set; }
    public decimal PowerKw { get; set; }
    public decimal EnergyKwh { get; set; }
    public DateTime PeriodStartUtc { get; set; }
    public DateTime PeriodEndUtc { get; set; }
    public decimal? ProductionOutput { get; set; }
    public string? ProductionUnit { get; set; }
    public Guid? SourceRowId { get; set; }
    public Equipment? Equipment { get; set; }
    public CsvImportBatchRow? SourceRow { get; set; }
}
