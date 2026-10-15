using AssetIQ.Business.DTOs;
using AssetIQ.Business.Interfaces;

namespace AssetIQ.Business.Services;

public sealed class EquipmentService : IEquipmentService
{
    private readonly IRollingMillDatasetAccessor _datasetAccessor;

    public EquipmentService(IRollingMillDatasetAccessor datasetAccessor)
    {
        _datasetAccessor = datasetAccessor;
    }

    public async Task<IReadOnlyList<EquipmentRecordDto>> GetEquipmentAsync(
        EquipmentQuery query,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await _datasetAccessor.GetSnapshotAsync(cancellationToken);
        if (!snapshot.IsValid)
        {
            return Array.Empty<EquipmentRecordDto>();
        }

        IEnumerable<EquipmentRecordDto> results = snapshot.Equipment;

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            results = results.Where(e =>
                e.EquipmentCode.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                e.EquipmentTypeKey.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                e.Section.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.EquipmentType))
        {
            results = results.Where(e =>
                e.EquipmentTypeKey.Equals(query.EquipmentType, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.Section))
        {
            results = results.Where(e =>
                e.Section.Equals(query.Section, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query.RiskLevel))
        {
            results = results.Where(e =>
                e.RiskLevel.Equals(query.RiskLevel, StringComparison.OrdinalIgnoreCase));
        }

        return results
            .OrderBy(e => e.EquipmentCode, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<EquipmentRecordDto?> GetByCodeAsync(string equipmentCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(equipmentCode))
        {
            return null;
        }

        var snapshot = await _datasetAccessor.GetSnapshotAsync(cancellationToken);
        if (!snapshot.IsValid)
        {
            return null;
        }

        return snapshot.Equipment.FirstOrDefault(e =>
            e.EquipmentCode.Equals(equipmentCode.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IReadOnlyList<string>> GetEquipmentTypesAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await _datasetAccessor.GetSnapshotAsync(cancellationToken);
        return snapshot.IsValid
            ? snapshot.Equipment.Select(e => e.EquipmentTypeKey).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList()
            : Array.Empty<string>();
    }

    public async Task<IReadOnlyList<string>> GetSectionsAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await _datasetAccessor.GetSnapshotAsync(cancellationToken);
        return snapshot.IsValid
            ? snapshot.Equipment.Select(e => e.Section).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList()
            : Array.Empty<string>();
    }

    public async Task<IReadOnlyList<string>> GetRiskLevelsAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await _datasetAccessor.GetSnapshotAsync(cancellationToken);
        return snapshot.IsValid
            ? snapshot.Equipment.Select(e => e.RiskLevel).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList()
            : Array.Empty<string>();
    }
}
