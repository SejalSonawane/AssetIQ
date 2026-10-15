using AssetIQ.Data.Entities;
using AssetIQ.Data.Interfaces;

namespace AssetIQ.Data.Repositories;

/// <summary>
/// Temporary in-memory stub so the solution compiles without a database. Not used for production data.
/// </summary>
public class InMemoryEquipmentRepository : IEquipmentRepository
{
    public Task<IReadOnlyList<Equipment>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Equipment> empty = Array.Empty<Equipment>();
        return Task.FromResult(empty);
    }
}
