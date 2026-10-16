using AssetIQ.Business.Interfaces;
using AssetIQ.Web.Authorization;
using AssetIQ.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetIQ.Web.Controllers;

[Authorize(Policy = AssetIQPolicies.FailureAnalyticsRead)]
public class FailureAnalyticsController : Controller
{
    private readonly IFailureAnalyticsService _failureAnalyticsService;
    private readonly IRollingMillDatasetAccessor _datasetAccessor;

    public FailureAnalyticsController(
        IFailureAnalyticsService failureAnalyticsService,
        IRollingMillDatasetAccessor datasetAccessor)
    {
        _failureAnalyticsService = failureAnalyticsService;
        _datasetAccessor = datasetAccessor;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["ActiveModule"] = "failure";
        ViewData["PageTitle"] = "Failure Analytics";

        var snapshot = await _datasetAccessor.GetSnapshotAsync(cancellationToken);
        return View(new FailureAnalyticsViewModel
        {
            DatasetStatus = DatasetStatusViewModel.FromSnapshot(snapshot),
            Summary = await _failureAnalyticsService.GetSummaryAsync(cancellationToken)
        });
    }
}
