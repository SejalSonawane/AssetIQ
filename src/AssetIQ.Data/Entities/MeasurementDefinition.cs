namespace AssetIQ.Data.Entities;

public sealed class MeasurementDefinition
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string PhysicalDimension { get; set; } = string.Empty;
    public string CanonicalUnit { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<EquipmentTypeMeasurement> EquipmentTypeAssociations { get; set; } = new List<EquipmentTypeMeasurement>();
    public ICollection<SensorReading> SensorReadings { get; set; } = new List<SensorReading>();
}