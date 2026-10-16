namespace AssetIQ.Data.Entities;

/// <summary>
/// Generic industrial equipment record. Type-specific parameters are configurable via metadata, not hardcoded tables per type.
/// </summary>
public class Equipment
{
    public Guid Id { get; set; }
    public string EquipmentCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid EquipmentTypeId { get; set; }
    public Guid PlantId { get; set; }
    public string Section { get; set; } = string.Empty;
    public string Criticality { get; set; } = string.Empty;
    public string OperatingStatus { get; set; } = string.Empty;
    public decimal? HealthScore { get; set; }
    public string? RiskLevel { get; set; }
    public Guid? SourceRowId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public Plant? Plant { get; set; }
    public EquipmentType? EquipmentType { get; set; }
    public CsvImportBatchRow? SourceRow { get; set; }
}
