using AssetIQ.Business.DTOs;

namespace AssetIQ.Web.ViewModels;

public class AlertsListViewModel
{
    public DatasetStatusViewModel DatasetStatus { get; init; } = new();
    public IReadOnlyList<AlertRecordDto> Alerts { get; init; } = Array.Empty<AlertRecordDto>();
}
