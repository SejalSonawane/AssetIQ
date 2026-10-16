using AssetIQ.Business.DTOs;
using AssetIQ.Business.Interfaces;
using AssetIQ.Web.Authorization;
using AssetIQ.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetIQ.Web.Controllers;

[Authorize(Policy = AssetIQPolicies.MaintenanceRead)]
public class MaintenanceController : Controller
{
    private readonly IMaintenanceWorkOrderService _maintenanceWorkOrderService;
    private readonly IMaintenanceActionService _maintenanceActionService;
    private readonly IMaintenanceTicketService _maintenanceTicketService;
    private readonly IRollingMillDatasetAccessor _datasetAccessor;

    public MaintenanceController(
        IMaintenanceWorkOrderService maintenanceWorkOrderService,
        IMaintenanceActionService maintenanceActionService,
        IMaintenanceTicketService maintenanceTicketService,
        IRollingMillDatasetAccessor datasetAccessor)
    {
        _maintenanceWorkOrderService = maintenanceWorkOrderService;
        _maintenanceActionService = maintenanceActionService;
        _maintenanceTicketService = maintenanceTicketService;
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
            Tickets = await _maintenanceTicketService.GetTicketsAsync(cancellationToken),
            TicketEquipment = await _maintenanceTicketService.GetPersistedEquipmentOptionsAsync(cancellationToken),
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
    [Authorize(Policy = AssetIQPolicies.MaintenanceManage)]
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
    [Authorize(Policy = AssetIQPolicies.MaintenanceManage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteAction(Guid id, CancellationToken cancellationToken)
    {
        var completed = await _maintenanceActionService.CompleteActionAsync(id, cancellationToken);
        TempData[completed ? "MaintenanceActionMessage" : "MaintenanceActionError"] = completed
            ? "Application action marked complete."
            : "Application action was not found or is already complete.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Policy = AssetIQPolicies.MaintenanceManage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTicket(
        string? equipmentCode,
        string? description,
        string? priority,
        string? assignedTo,
        DateTime? scheduledAtUtc,
        CancellationToken cancellationToken)
    {
        try
        {
            await _maintenanceTicketService.CreateTicketAsync(
                new MaintenanceTicketCreateRequest(equipmentCode, description, priority, assignedTo, scheduledAtUtc),
                User.Identity?.Name ?? string.Empty,
                cancellationToken);
            TempData["MaintenanceActionMessage"] = "Maintenance ticket created and saved to the database.";
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
    [Authorize(Policy = AssetIQPolicies.MaintenanceManage)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTicketStatus(
        Guid id,
        string? status,
        string? completionNotes,
        CancellationToken cancellationToken)
    {
        var updated = await _maintenanceTicketService.UpdateStatusAsync(
            id,
            status ?? string.Empty,
            completionNotes,
            Request.Form["assignedTo"].FirstOrDefault(),
            cancellationToken);
        TempData[updated ? "MaintenanceActionMessage" : "MaintenanceActionError"] = updated
            ? "Maintenance ticket status updated."
            : "Maintenance ticket was not found or the requested status is invalid.";
        return RedirectToAction(nameof(Index));
    }
}
