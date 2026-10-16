using AssetIQ.Business.Interfaces;
using AssetIQ.Web.Authorization;
using AssetIQ.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetIQ.Web.Controllers;

[Authorize(Policy = AssetIQPolicies.EnergyAnalyticsRead)]
public class EnergyAnalyticsController : Controller
{
    private readonly IEnergyAnalyticsService _energyAnalyticsService;
    private readonly IRollingMillDatasetAccessor _datasetAccessor;

    public EnergyAnalyticsController(
        IEnergyAnalyticsService energyAnalyticsService,
        IRollingMillDatasetAccessor datasetAccessor)
    {
        _energyAnalyticsService = energyAnalyticsService;
        _datasetAccessor = datasetAccessor;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["ActiveModule"] = "energy";
        ViewData["PageTitle"] = "Energy Analytics";

        var snapshot = await _datasetAccessor.GetSnapshotAsync(cancellationToken);
        return View(new EnergyAnalyticsViewModel
        {
            DatasetStatus = DatasetStatusViewModel.FromSnapshot(snapshot),
            Summary = await _energyAnalyticsService.GetSummaryAsync(cancellationToken)
        });
    }
}
