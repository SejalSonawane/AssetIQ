using AssetIQ.Business.Interfaces;
using AssetIQ.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AssetIQ.Web.Controllers;

public class PredictiveMaintenanceController : Controller
{
    private readonly IPredictiveMaintenanceService _predictiveMaintenanceService;
    private readonly IRollingMillDatasetAccessor _datasetAccessor;

    public PredictiveMaintenanceController(
        IPredictiveMaintenanceService predictiveMaintenanceService,
        IRollingMillDatasetAccessor datasetAccessor)
    {
        _predictiveMaintenanceService = predictiveMaintenanceService;
        _datasetAccessor = datasetAccessor;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["ActiveModule"] = "predictive";
        ViewData["PageTitle"] = "Predictive Maintenance";

        var snapshot = await _datasetAccessor.GetSnapshotAsync(cancellationToken);
        return View(new PredictiveMaintenanceViewModel
        {
            DatasetStatus = DatasetStatusViewModel.FromSnapshot(snapshot),
            Summary = await _predictiveMaintenanceService.GetSummaryAsync(cancellationToken)
        });
    }
}
