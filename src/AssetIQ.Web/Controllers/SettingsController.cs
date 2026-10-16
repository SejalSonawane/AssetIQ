using AssetIQ.Business.DTOs;
using AssetIQ.Business.Interfaces;
using AssetIQ.Web.Authorization;
using AssetIQ.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetIQ.Web.Controllers;

[Authorize(Policy = AssetIQPolicies.SettingsManage)]
public class SettingsController : Controller
{
    private readonly IRollingMillCsvImportService _csvImportService;
    private readonly IPredictionResultSyncService _predictionResultSyncService;
    private readonly IConfiguration _configuration;

    public SettingsController(
        IRollingMillCsvImportService csvImportService,
        IPredictionResultSyncService predictionResultSyncService,
        IConfiguration configuration)
    {
        _csvImportService = csvImportService;
        _predictionResultSyncService = predictionResultSyncService;
        _configuration = configuration;
    }

    public IActionResult Index()
    {
        ViewData["ActiveModule"] = "settings";
        ViewData["PageTitle"] = "Settings";
        var relativePath = _configuration["AssetIQ:Dataset:RollingMillCsvRelativePath"]
            ?? "../../data/rolling-mill/AssetIQ_RollingMill_2000Rows_Demo.csv";
        return View(new SettingsViewModel { ConfiguredCsvRelativePath = relativePath });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportConfiguredCsv(CancellationToken cancellationToken)
    {
        var result = await _csvImportService.ImportConfiguredCsvAsync(cancellationToken);
        if (result.Succeeded)
        {
            var synced = await _predictionResultSyncService.SyncPersistedEquipmentPredictionsAsync(cancellationToken);
            TempData["ImportMessage"] = $"{result.Message} Synced {synced} ML prediction record(s) for persisted equipment.";
        }
        else
        {
            TempData["ImportError"] = result.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
