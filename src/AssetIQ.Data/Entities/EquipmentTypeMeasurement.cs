namespace AssetIQ.Data.Entities;

public sealed class EquipmentTypeMeasurement
{
    public Guid EquipmentTypeId { get; set; }
    public Guid MeasurementDefinitionId { get; set; }
    public bool IsRequired { get; set; }
    public int DisplayOrder { get; set; }
    public EquipmentType? EquipmentType { get; set; }
    public MeasurementDefinition? MeasurementDefinition { get; set; }
}