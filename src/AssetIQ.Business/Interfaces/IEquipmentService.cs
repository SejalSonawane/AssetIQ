using AssetIQ.Business.DTOs;

namespace AssetIQ.Business.Interfaces;

public sealed class EquipmentQuery
{
    public string? Search { get; init; }
    public string? EquipmentType { get; init; }
    public string? Section { get; init; }
    public string? RiskLevel { get; init; }
}

public interface IEquipmentService
{
    Task<IReadOnlyList<EquipmentRecordDto>> GetEquipmentAsync(EquipmentQuery query, CancellationToken cancellationToken = default);
    Task<EquipmentRecordDto?> GetByCodeAsync(string equipmentCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetEquipmentTypesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetSectionsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetRiskLevelsAsync(CancellationToken cancellationToken = default);
}
