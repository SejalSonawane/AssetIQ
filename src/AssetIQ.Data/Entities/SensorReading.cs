namespace AssetIQ.Data.Entities;

public class SensorReading
{
    public Guid Id { get; set; }
    public Guid EquipmentId { get; set; }
    public string MeasurementKey { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string? Unit { get; set; }
    public DateTime RecordedAtUtc { get; set; }
    public Equipment? Equipment { get; set; }
}
