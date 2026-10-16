using AssetIQ.Business.DTOs;
using AssetIQ.Business.Interfaces;
using AssetIQ.Business.Ml;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace AssetIQ.Business.Services;

public sealed class RiskClassificationModelService : IRiskClassificationModelService
{
    private const int MinimumRowCount = 50;
    private const string ModelVersion = "1.0";
    private static readonly string[] FeatureKeys =
    [
        "MotorTempC", "BearingTempC", "VibrationMmS", "MotorCurrentA", "PowerKw", "EnergyKwh",
        "HydraulicPressureBar", "OilPressureBar", "RollSpeedMpm", "RollForceKn", "OperatingHours",
        "ProductionTons", "MaintenanceDaysAgo", "FailureCountLastYear", "EquipmentTypeKey", "Section"
    ];

    private static readonly string[] ExcludedLeakageFields =
    [
        "Health_Score", "Risk_Level", "Maintenance_Flag", "Energy_Intensity",
        "Estimated_Downtime_Hours", "Estimated_Cost_Impact_INR"
    ];

    private readonly IRollingMillDatasetAccessor _datasetAccessor;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private CachedModel? _cache;

    public RiskClassificationModelService(IRollingMillDatasetAccessor datasetAccessor)
    {
        _datasetAccessor = datasetAccessor;
    }

    public async Task<RiskModelEvaluationDto> GetEvaluationAsync(CancellationToken cancellationToken = default)
    {
        var cached = await EnsureModelAsync(cancellationToken);
        return cached.Evaluation;
    }

    public async Task<IReadOnlyList<EquipmentRiskPredictionDto>> PredictAllAsync(
        CancellationToken cancellationToken = default)
    {
        var cached = await EnsureModelAsync(cancellationToken);
        if (!cached.Evaluation.IsAvailable || cached.PredictionEngine is null)
        {
            return Array.Empty<EquipmentRiskPredictionDto>();
        }

        return cached.Records.Select(record => PredictRecord(record, cached)).ToList();
    }

    public async Task<EquipmentRiskPredictionDto?> PredictEquipmentAsync(
        string equipmentCode,
        CancellationToken cancellationToken = default)
    {
        var cached = await EnsureModelAsync(cancellationToken);
        var record = cached.Records.FirstOrDefault(r =>
            r.EquipmentCode.Equals(equipmentCode, StringComparison.OrdinalIgnoreCase));
        if (record is null || !cached.Evaluation.IsAvailable || cached.PredictionEngine is null)
        {
            return null;
        }

        return PredictRecord(record, cached);
    }

    private static EquipmentRiskPredictionDto PredictRecord(EquipmentRecordDto record, CachedModel cached)
    {
        var scored = cached.PredictionEngine!.Predict(MapRow(record));
        return new EquipmentRiskPredictionDto
        {
            EquipmentCode = record.EquipmentCode,
            DatasetRiskLevel = record.RiskLevel,
            PredictedRiskLevel = scored.PredictedLabel,
            MatchesDatasetLabel = scored.PredictedLabel.Equals(record.RiskLevel, StringComparison.OrdinalIgnoreCase),
            Confidence = GetConfidence(scored.Score),
            SupportingFactors = BuildFactors(scored.Score, cached.ClassNames)
        };
    }

    private async Task<CachedModel> EnsureModelAsync(CancellationToken cancellationToken)
    {
        var snapshot = await _datasetAccessor.GetSnapshotAsync(cancellationToken);
        var cacheKey = $"{snapshot.SourceLabel}|{snapshot.Equipment.Count}|{snapshot.IsValid}";

        if (_cache is not null && _cache.CacheKey == cacheKey)
        {
            return _cache;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_cache is not null && _cache.CacheKey == cacheKey)
            {
                return _cache;
            }

            _cache = Train(snapshot.IsValid ? snapshot.Equipment : Array.Empty<EquipmentRecordDto>(), cacheKey);
            return _cache;
        }
        finally
        {
            _gate.Release();
        }
    }

    private static CachedModel Train(IReadOnlyList<EquipmentRecordDto> equipment, string cacheKey)
    {
        if (equipment.Count < MinimumRowCount)
        {
            return Unavailable(cacheKey, equipment,
                $"At least {MinimumRowCount} valid equipment rows are required to train the risk classifier.");
        }

        var rows = equipment
            .Where(e => !string.IsNullOrWhiteSpace(e.RiskLevel))
            .Select(MapRow)
            .ToList();

        var classNames = rows.Select(r => r.Label).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToArray();
        if (classNames.Length < 2)
        {
            return Unavailable(cacheKey, equipment,
                "The dataset does not contain enough distinct Risk_Level classes for multiclass training.");
        }

        var ml = new MLContext(seed: 42);
        var data = ml.Data.LoadFromEnumerable(rows);
        var split = ml.Data.TrainTestSplit(data, testFraction: 0.2, seed: 42);

        var pipeline = ml.Transforms.Conversion.MapValueToKey("LabelKey", nameof(RiskLevelTrainingRow.Label))
            .Append(ml.Transforms.Categorical.OneHotEncoding("EquipmentTypeEncoded", nameof(RiskLevelTrainingRow.EquipmentTypeKey)))
            .Append(ml.Transforms.Categorical.OneHotEncoding("SectionEncoded", nameof(RiskLevelTrainingRow.Section)))
            .Append(ml.Transforms.Concatenate(
                "Features",
                nameof(RiskLevelTrainingRow.MotorTempC),
                nameof(RiskLevelTrainingRow.BearingTempC),
                nameof(RiskLevelTrainingRow.VibrationMmS),
                nameof(RiskLevelTrainingRow.MotorCurrentA),
                nameof(RiskLevelTrainingRow.PowerKw),
                nameof(RiskLevelTrainingRow.EnergyKwh),
                nameof(RiskLevelTrainingRow.HydraulicPressureBar),
                nameof(RiskLevelTrainingRow.OilPressureBar),
                nameof(RiskLevelTrainingRow.RollSpeedMpm),
                nameof(RiskLevelTrainingRow.RollForceKn),
                nameof(RiskLevelTrainingRow.OperatingHours),
                nameof(RiskLevelTrainingRow.ProductionTons),
                nameof(RiskLevelTrainingRow.MaintenanceDaysAgo),
                nameof(RiskLevelTrainingRow.FailureCountLastYear),
                "EquipmentTypeEncoded",
                "SectionEncoded"))
            .Append(ml.MulticlassClassification.Trainers.SdcaMaximumEntropy("LabelKey", "Features"))
            .Append(ml.Transforms.Conversion.MapKeyToValue("PredictedLabel", "PredictedLabel"));

        ITransformer model;
        try
        {
            model = pipeline.Fit(split.TrainSet);
        }
        catch (Exception exception)
        {
            return Unavailable(cacheKey, equipment, $"Training failed: {exception.Message}");
        }

        var trainCount = ml.Data.CreateEnumerable<RiskLevelTrainingRow>(split.TrainSet, reuseRowObject: false).Count();
        var holdoutCount = ml.Data.CreateEnumerable<RiskLevelTrainingRow>(split.TestSet, reuseRowObject: false).Count();

        var testPredictions = model.Transform(split.TestSet);
        var metrics = ml.MulticlassClassification.Evaluate(
            testPredictions,
            labelColumnName: "LabelKey",
            predictedLabelColumnName: "PredictedLabel",
            scoreColumnName: "Score");

        var engine = ml.Model.CreatePredictionEngine<RiskLevelTrainingRow, RiskLevelScoredRow>(model);
        var evaluation = new RiskModelEvaluationDto
        {
            IsAvailable = true,
            ModelIdentifier = "RiskLevel.SdcaMaximumEntropy",
            ModelVersion = ModelVersion,
            TargetKey = "Risk_Level",
            TargetDescription =
                "Multiclass Risk_Level labels supplied in the Rolling Mill CSV (rule-derived from Health_Score in this dataset — not confirmed future failures).",
            TrainingRowCount = trainCount,
            HoldoutRowCount = holdoutCount,
            MacroAccuracy = metrics.MacroAccuracy,
            MicroAccuracy = metrics.MicroAccuracy,
            LogLoss = metrics.LogLoss,
            PerClassMetrics = BuildPerClassMetrics(metrics, classNames),
            FeatureKeys = FeatureKeys,
            ExcludedLeakageFields = ExcludedLeakageFields,
            Limitations =
                "Holdout metrics use a random 80/20 split (seed 42). Classes are imbalanced (~95% Critical). " +
                "Predictions approximate dataset rule labels from sensor/context features; they are not independent failure forecasts."
        };

        return new CachedModel
        {
            CacheKey = cacheKey,
            Records = equipment,
            Evaluation = evaluation,
            PredictionEngine = engine,
            ClassNames = classNames
        };
    }

    private static CachedModel Unavailable(string cacheKey, IReadOnlyList<EquipmentRecordDto> equipment, string reason) =>
        new()
        {
            CacheKey = cacheKey,
            Records = equipment,
            Evaluation = new RiskModelEvaluationDto
            {
                IsAvailable = false,
                UnavailableReason = reason,
                FeatureKeys = FeatureKeys,
                ExcludedLeakageFields = ExcludedLeakageFields,
                Limitations = "Model not trained."
            }
        };

    private static IReadOnlyList<RiskClassMetricDto> BuildPerClassMetrics(
        MulticlassClassificationMetrics metrics,
        IReadOnlyList<string> classNames)
    {
        var matrix = metrics.ConfusionMatrix;
        if (matrix is null || classNames.Count == 0)
        {
            return Array.Empty<RiskClassMetricDto>();
        }

        var precision = matrix.PerClassPrecision;
        var recall = matrix.PerClassRecall;
        var count = Math.Min(classNames.Count, Math.Min(precision.Count, recall.Count));
        var results = new List<RiskClassMetricDto>(count);
        for (var i = 0; i < count; i++)
        {
            var p = precision[i];
            var r = recall[i];
            var f1 = p + r > 0 ? 2 * p * r / (p + r) : 0;
            results.Add(new RiskClassMetricDto
            {
                ClassName = classNames[i],
                Precision = p,
                Recall = r,
                F1Score = f1
            });
        }

        return results;
    }

    private static RiskLevelTrainingRow MapRow(EquipmentRecordDto record) => new()
    {
        EquipmentTypeKey = record.EquipmentTypeKey,
        Section = record.Section,
        MotorTempC = (float)record.MotorTempC,
        BearingTempC = (float)record.BearingTempC,
        VibrationMmS = (float)record.VibrationMmS,
        MotorCurrentA = (float)record.MotorCurrentA,
        PowerKw = (float)record.PowerKw,
        EnergyKwh = (float)record.EnergyKwh,
        HydraulicPressureBar = (float)record.HydraulicPressureBar,
        OilPressureBar = (float)record.OilPressureBar,
        RollSpeedMpm = (float)record.RollSpeedMpm,
        RollForceKn = (float)record.RollForceKn,
        OperatingHours = (float)record.OperatingHours,
        ProductionTons = (float)record.ProductionTons,
        MaintenanceDaysAgo = (float)record.MaintenanceDaysAgo,
        FailureCountLastYear = (float)record.FailureCountLastYear,
        Label = record.RiskLevel
    };

    private static float GetConfidence(float[] scores) => scores.Length == 0 ? 0f : scores.Max();

    private static IReadOnlyList<RiskScoreFactorDto> BuildFactors(float[] scores, IReadOnlyList<string> classNames)
    {
        if (scores.Length == 0 || classNames.Count == 0)
        {
            return Array.Empty<RiskScoreFactorDto>();
        }

        var count = Math.Min(scores.Length, classNames.Count);
        var pairs = new List<RiskScoreFactorDto>(count);
        for (var i = 0; i < count; i++)
        {
            pairs.Add(new RiskScoreFactorDto { Factor = $"Class score: {classNames[i]}", Score = scores[i] });
        }

        return pairs.OrderByDescending(p => p.Score).Take(3).ToList();
    }

    private sealed class CachedModel
    {
        public string CacheKey { get; init; } = string.Empty;
        public IReadOnlyList<EquipmentRecordDto> Records { get; init; } = Array.Empty<EquipmentRecordDto>();
        public RiskModelEvaluationDto Evaluation { get; init; } = new();
        public PredictionEngine<RiskLevelTrainingRow, RiskLevelScoredRow>? PredictionEngine { get; init; }
        public IReadOnlyList<string> ClassNames { get; init; } = Array.Empty<string>();
    }
}
