using AssetIQ.Business.DTOs;

namespace AssetIQ.Web.ViewModels;

public class EquipmentDetailViewModel
{
    public DatasetStatusViewModel DatasetStatus { get; init; } = new();
    public EquipmentRecordDto? Equipment { get; init; }
    public string? NotFoundCode { get; init; }
}
