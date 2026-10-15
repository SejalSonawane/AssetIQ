using AssetIQ.Business.DTOs;

namespace AssetIQ.Business.Interfaces;

public interface IRollingMillDatasetAccessor
{
    Task<DatasetSnapshotDto> GetSnapshotAsync(CancellationToken cancellationToken = default);
}
