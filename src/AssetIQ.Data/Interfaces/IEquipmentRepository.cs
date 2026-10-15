using AssetIQ.Data.Entities;

namespace AssetIQ.Data.Interfaces;

/// <summary>
/// Persistence boundary for equipment. Implementation will be added when PostgreSQL integration is approved.
/// </summary>
public interface IEquipmentRepository
{
    Task<IReadOnlyList<Equipment>> GetAllAsync(CancellationToken cancellationToken = default);
}
