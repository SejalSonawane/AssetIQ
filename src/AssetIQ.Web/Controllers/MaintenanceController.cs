using AssetIQ.Business.DTOs;
using AssetIQ.Business.Interfaces;
using AssetIQ.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace AssetIQ.Web.Controllers;

public class MaintenanceController : Controller
{
    private readonly IMaintenanceWorkOrderService _maintenanceWorkOrderService;
    private readonly IMaintenanceActionService _maintenanceActionService;
    private readonly IRollingMillDatasetAccessor _datasetAccessor;

    public MaintenanceController(
        IMaintenanceWorkOrderService maintenanceWorkOrderService,
        IMaintenanceActionService maintenanceActionService,
        IRollingMillDatasetAccessor datasetAccessor)
    {
        _maintenanceWorkOrderService = maintenanceWorkOrderService;
        _maintenanceActionService = maintenanceActionService;
        _datasetAccessor = datasetAccessor;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        string? status,
        string? priority,
        string? section,
        CancellationToken cancellationToken)
    {
        ViewData["ActiveModule"] = "maintenance";
        ViewData["PageTitle"] = "Maintenance Management";

        var snapshot = await _datasetAccessor.GetSnapshotAsync(cancellationToken);
        var query = new MaintenanceWorkOrderQuery
        {
            Search = search,
            Status = status,
            Priority = priority,
            Section = section
        };

        return View(new MaintenanceListViewModel
        {
            DatasetStatus = DatasetStatusViewModel.FromSnapshot(snapshot),
            WorkOrders = await _maintenanceWorkOrderService.GetWorkOrdersAsync(query, cancellationToken),
            Actions = await _maintenanceActionService.GetActionsAsync(cancellationToken),
            ActionEquipment = snapshot.IsValid
                ? snapshot.Equipment.OrderBy(e => e.EquipmentCode, StringComparer.OrdinalIgnoreCase).ToList()
                : Array.Empty<EquipmentRecordDto>(),
            Search = search,
            SelectedStatus = status,
            SelectedPriority = priority,
            SelectedSection = section,
            Statuses = await _maintenanceWorkOrderService.GetStatusesAsync(cancellationToken),
            Priorities = await _maintenanceWorkOrderService.GetPrioritiesAsync(cancellationToken),
            Sections = await _maintenanceWorkOrderService.GetSectionsAsync(cancellationToken)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAction(
        string? equipmentCode,
        string? description,
        CancellationToken cancellationToken)
    {
        try
        {
            await _maintenanceActionService.CreateActionAsync(equipmentCode, description, cancellationToken);
            TempData["MaintenanceActionMessage"] = "Application action created. It is stored only in this running application instance.";
        }
        catch (ArgumentException exception)
        {
            TempData["MaintenanceActionError"] = exception.Message;
        }
        catch (InvalidOperationException exception)
        {
            TempData["MaintenanceActionError"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteAction(Guid id, CancellationToken cancellationToken)
    {
        var completed = await _maintenanceActionService.CompleteActionAsync(id, cancellationToken);
        TempData[completed ? "MaintenanceActionMessage" : "MaintenanceActionError"] = completed
            ? "Application action marked complete."
            : "Application action was not found or is already complete.";
        return RedirectToAction(nameof(Index));
    }
}
