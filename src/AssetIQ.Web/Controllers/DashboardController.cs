using AssetIQ.Business.Interfaces;
using AssetIQ.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AssetIQ.Web.Controllers;

public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly IRollingMillDatasetAccessor _datasetAccessor;

    public DashboardController(
        IDashboardService dashboardService,
        IRollingMillDatasetAccessor datasetAccessor)
    {
        _dashboardService = dashboardService;
        _datasetAccessor = datasetAccessor;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["ActiveModule"] = "dashboard";
        ViewData["PageTitle"] = "Operations Dashboard";

        var snapshot = await _datasetAccessor.GetSnapshotAsync(cancellationToken);
        var summary = await _dashboardService.GetSummaryAsync(cancellationToken);

        return View(new DashboardViewModel
        {
            DatasetStatus = DatasetStatusViewModel.FromSnapshot(snapshot),
            Summary = summary
        });
    }
}
