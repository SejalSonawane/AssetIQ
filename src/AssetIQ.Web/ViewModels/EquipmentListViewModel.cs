using AssetIQ.Business.DTOs;

namespace AssetIQ.Web.ViewModels;

public class EquipmentListViewModel
{
    public DatasetStatusViewModel DatasetStatus { get; init; } = new();
    public IReadOnlyList<EquipmentRecordDto> Items { get; init; } = Array.Empty<EquipmentRecordDto>();
    public string? Search { get; init; }
    public string? SelectedEquipmentType { get; init; }
    public string? SelectedSection { get; init; }
    public string? SelectedRiskLevel { get; init; }
    public IReadOnlyList<string> EquipmentTypes { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Sections { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> RiskLevels { get; init; } = Array.Empty<string>();
}
