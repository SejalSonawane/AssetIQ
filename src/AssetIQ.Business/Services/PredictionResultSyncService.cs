using System.Text.Json;
using AssetIQ.Business.Interfaces;
using AssetIQ.Data;
using AssetIQ.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AssetIQ.Business.Services;

public sealed class PredictionResultSyncService : IPredictionResultSyncService
{
    private const string ModelIdentifier = "RiskLevel.SdcaMaximumEntropy";
    private const string ModelVersion = "1.0";
    private const string TargetKey = "Risk_Level";

    private readonly AssetIQDbContext _dbContext;
    private readonly IRiskClassificationModelService _riskClassificationModelService;

    public PredictionResultSyncService(
        AssetIQDbContext dbContext,
        IRiskClassificationModelService riskClassificationModelService)
    {
        _dbContext = dbContext;
        _riskClassificationModelService = riskClassificationModelService;
    }

    public async Task<int> SyncPersistedEquipmentPredictionsAsync(CancellationToken cancellationToken = default)
    {
        var evaluation = await _riskClassificationModelService.GetEvaluationAsync(cancellationToken);
        if (!evaluation.IsAvailable)
        {
            return 0;
        }

        var predictions = await _riskClassificationModelService.PredictAllAsync(cancellationToken);
        if (predictions.Count == 0)
        {
            return 0;
        }

        var predictionLookup = predictions.ToDictionary(
            p => p.EquipmentCode,
            p => p,
            StringComparer.OrdinalIgnoreCase);

        var equipment = await _dbContext.Equipment
            .Where(e => predictionLookup.Keys.Contains(e.EquipmentCode))
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var synced = 0;

        foreach (var item in equipment)
        {
            if (!predictionLookup.TryGetValue(item.EquipmentCode, out var prediction))
            {
                continue;
            }

            var existing = await _dbContext.PredictionResults
                .Where(p =>
                    p.EquipmentId == item.Id &&
                    p.ModelIdentifier == ModelIdentifier &&
                    p.ModelVersion == ModelVersion &&
                    p.TargetKey == TargetKey)
                .OrderByDescending(p => p.PredictedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            var explanationJson = JsonSerializer.Serialize(new
            {
                prediction.DatasetRiskLevel,
                prediction.MatchesDatasetLabel,
                prediction.SupportingFactors
            });

            if (existing is null)
            {
                _dbContext.PredictionResults.Add(new PredictionResult
                {
                    Id = Guid.NewGuid(),
                    EquipmentId = item.Id,
                    ModelIdentifier = ModelIdentifier,
                    ModelVersion = ModelVersion,
                    TargetKey = TargetKey,
                    PredictedAtUtc = now,
                    PredictedClass = prediction.PredictedRiskLevel,
                    Probability = (decimal)prediction.Confidence,
                    ExplanationMetadataJson = explanationJson
                });
            }
            else
            {
                existing.PredictedAtUtc = now;
                existing.PredictedClass = prediction.PredictedRiskLevel;
                existing.Probability = (decimal)prediction.Confidence;
                existing.ExplanationMetadataJson = explanationJson;
            }

            synced++;
        }

        if (synced > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return synced;
    }
}
