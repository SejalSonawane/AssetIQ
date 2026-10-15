using AssetIQ.Business.DTOs;

namespace AssetIQ.Web.ViewModels;

public class DashboardViewModel
{
    public DatasetStatusViewModel DatasetStatus { get; init; } = new();
    public DashboardSummaryDto Summary { get; init; } = new();
}
