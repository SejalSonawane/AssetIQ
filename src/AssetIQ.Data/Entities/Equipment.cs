namespace AssetIQ.Data.Entities;

/// <summary>
/// Generic industrial equipment record. Type-specific parameters are configurable via metadata, not hardcoded tables per type.
/// </summary>
public class Equipment
{
    public Guid Id { get; set; }
    public string EquipmentCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string EquipmentTypeKey { get; set; } = string.Empty;
    public string Plant { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public string Criticality { get; set; } = string.Empty;
    public string OperatingStatus { get; set; } = string.Empty;
    public decimal? HealthScore { get; set; }
    public string? RiskLevel { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
