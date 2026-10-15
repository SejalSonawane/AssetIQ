using AssetIQ.Business.DTOs;

namespace AssetIQ.Business.Interfaces;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
}
