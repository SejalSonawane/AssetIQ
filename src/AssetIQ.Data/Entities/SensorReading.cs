namespace AssetIQ.Data.Entities;

public class SensorReading
{
    public Guid Id { get; set; }
    public Guid EquipmentId { get; set; }
    public Guid EquipmentTypeId { get; set; }
    public Guid MeasurementDefinitionId { get; set; }
    public decimal Value { get; set; }
    public string? Unit { get; set; }
    public DateTime RecordedAtUtc { get; set; }
    public Guid? SourceRowId { get; set; }
    public Equipment? Equipment { get; set; }
    public MeasurementDefinition? MeasurementDefinition { get; set; }
    public CsvImportBatchRow? SourceRow { get; set; }
}
