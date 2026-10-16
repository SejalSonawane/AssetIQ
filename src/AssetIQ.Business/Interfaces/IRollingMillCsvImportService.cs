using AssetIQ.Business.DTOs;

namespace AssetIQ.Business.Interfaces;

public interface IRollingMillCsvImportService
{
    Task<CsvImportResultDto> ImportConfiguredCsvAsync(
        CancellationToken cancellationToken = default);
}
