using AssetIQ.Business.Interfaces;
using AssetIQ.Web.Authorization;
using AssetIQ.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetIQ.Web.Controllers;

[Authorize(Policy = AssetIQPolicies.EquipmentRead)]
public class EquipmentController : Controller
{
    private readonly IEquipmentService _equipmentService;
    private readonly IRollingMillDatasetAccessor _datasetAccessor;
    private readonly IRiskClassificationModelService _riskClassificationModelService;

    public EquipmentController(
        IEquipmentService equipmentService,
        IRollingMillDatasetAccessor datasetAccessor,
        IRiskClassificationModelService riskClassificationModelService)
    {
        _equipmentService = equipmentService;
        _datasetAccessor = datasetAccessor;
        _riskClassificationModelService = riskClassificationModelService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        string? equipmentType,
        string? section,
        string? riskLevel,
        CancellationToken cancellationToken)
    {
        ViewData["ActiveModule"] = "equipment";
        ViewData["PageTitle"] = "Equipment";

        var snapshot = await _datasetAccessor.GetSnapshotAsync(cancellationToken);
        var query = new EquipmentQuery
        {
            Search = search,
            EquipmentType = equipmentType,
            Section = section,
            RiskLevel = riskLevel
        };

        var model = new EquipmentListViewModel
        {
            DatasetStatus = DatasetStatusViewModel.FromSnapshot(snapshot),
            Items = await _equipmentService.GetEquipmentAsync(query, cancellationToken),
            Search = search,
            SelectedEquipmentType = equipmentType,
            SelectedSection = section,
            SelectedRiskLevel = riskLevel,
            EquipmentTypes = await _equipmentService.GetEquipmentTypesAsync(cancellationToken),
            Sections = await _equipmentService.GetSectionsAsync(cancellationToken),
            RiskLevels = await _equipmentService.GetRiskLevelsAsync(cancellationToken)
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(string id, CancellationToken cancellationToken)
    {
        ViewData["ActiveModule"] = "equipment";
        ViewData["PageTitle"] = "Equipment Details";

        var snapshot = await _datasetAccessor.GetSnapshotAsync(cancellationToken);
        var equipment = await _equipmentService.GetByCodeAsync(id, cancellationToken);
        var mlPrediction = equipment is null
            ? null
            : await _riskClassificationModelService.PredictEquipmentAsync(id, cancellationToken);

        return View(new EquipmentDetailViewModel
        {
            DatasetStatus = DatasetStatusViewModel.FromSnapshot(snapshot),
            Equipment = equipment,
            MachineLearningPrediction = mlPrediction,
            NotFoundCode = equipment is null ? id : null
        });
    }
}
