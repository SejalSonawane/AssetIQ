using AssetIQ.Business.Interfaces;
using AssetIQ.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AssetIQ.Web.Controllers;

public class AlertsController : Controller
{
    private readonly IAlertService _alertService;
    private readonly IRollingMillDatasetAccessor _datasetAccessor;

    public AlertsController(
        IAlertService alertService,
        IRollingMillDatasetAccessor datasetAccessor)
    {
        _alertService = alertService;
        _datasetAccessor = datasetAccessor;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["ActiveModule"] = "alerts";
        ViewData["PageTitle"] = "Alerts & Recommendations";

        var snapshot = await _datasetAccessor.GetSnapshotAsync(cancellationToken);

        return View(new AlertsListViewModel
        {
            DatasetStatus = DatasetStatusViewModel.FromSnapshot(snapshot),
            Alerts = await _alertService.GetAlertsAsync(cancellationToken)
        });
    }
}
