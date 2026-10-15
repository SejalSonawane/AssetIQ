namespace AssetIQ.Data.Interfaces;

using AssetIQ.Data.Import;

public interface IRollingMillCsvReader
{
    Task<RollingMillCsvReadResult> ReadAsync(string filePath, CancellationToken cancellationToken = default);
}
