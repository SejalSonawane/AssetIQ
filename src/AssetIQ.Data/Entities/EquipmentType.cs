namespace AssetIQ.Data.Entities;

public sealed class EquipmentType
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<Equipment> Equipment { get; set; } = new List<Equipment>();
    public ICollection<EquipmentTypeMeasurement> Measurements { get; set; } = new List<EquipmentTypeMeasurement>();
}