using AssetIQ.Business.DTOs;

namespace AssetIQ.Business.Interfaces;

public interface IAlertService
{
    Task<IReadOnlyList<AlertRecordDto>> GetAlertsAsync(CancellationToken cancellationToken = default);
}
