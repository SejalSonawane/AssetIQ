using AssetIQ.Business.DTOs;

namespace AssetIQ.Business.Interfaces;

public interface IRiskClassificationModelService
{
    Task<RiskModelEvaluationDto> GetEvaluationAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EquipmentRiskPredictionDto>> PredictAllAsync(
        CancellationToken cancellationToken = default);

    Task<EquipmentRiskPredictionDto?> PredictEquipmentAsync(
        string equipmentCode,
        CancellationToken cancellationToken = default);
}
