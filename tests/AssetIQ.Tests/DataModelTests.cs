using AssetIQ.Data;
using AssetIQ.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace AssetIQ.Tests;

public class DataModelTests
{
    private static AssetIQDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AssetIQDbContext>()
            .UseNpgsql("Host=localhost;Database=assetiq_model_test")
            .Options);

    [Fact]
    public void ModelBuildsWithoutOpeningDatabaseConnection()
    {
        using var context = CreateContext();

        Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", context.Database.ProviderName);
        Assert.Equal("equipment", context.Model.FindEntityType(typeof(Equipment))!.GetTableName());
        Assert.Equal("plants", context.Model.FindEntityType(typeof(Plant))!.GetTableName());
        Assert.Equal("sensor_readings", context.Model.FindEntityType(typeof(SensorReading))!.GetTableName());
        Assert.Equal("prediction_results", context.Model.FindEntityType(typeof(PredictionResult))!.GetTableName());
        Assert.Equal(System.Data.ConnectionState.Closed, context.Database.GetDbConnection().State);
    }

    [Fact]
    public void EquipmentUsesPlantScopedCodeAndRestrictiveReferenceDeletes()
    {
        using var context = CreateContext();
        var equipment = context.Model.FindEntityType(typeof(Equipment))!;

        Assert.Contains(equipment.GetIndexes(), index =>
            index.IsUnique && index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(Equipment.PlantId), nameof(Equipment.EquipmentCode)]));
        Assert.Contains(equipment.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Select(property => property.Name).SequenceEqual([nameof(Equipment.PlantId)]) &&
            foreignKey.DeleteBehavior == DeleteBehavior.Restrict);
        Assert.Contains(equipment.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Select(property => property.Name).SequenceEqual([nameof(Equipment.EquipmentTypeId)]) &&
            foreignKey.DeleteBehavior == DeleteBehavior.Restrict);
        Assert.Contains(equipment.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Select(property => property.Name).SequenceEqual([nameof(Equipment.SourceRowId)]) &&
            foreignKey.PrincipalEntityType.ClrType == typeof(CsvImportBatchRow) &&
            foreignKey.DeleteBehavior == DeleteBehavior.Restrict);
    }

    [Fact]
    public void MeasurementCatalogAndReadingsUseConfiguredKeysPrecisionAndUtc()
    {
        using var context = CreateContext();
        var association = context.Model.FindEntityType(typeof(EquipmentTypeMeasurement))!;
        var reading = context.Model.FindEntityType(typeof(SensorReading))!;
        var value = reading.FindProperty(nameof(SensorReading.Value))!;
        var timestamp = reading.FindProperty(nameof(SensorReading.RecordedAtUtc))!;

        Assert.Equal(
            [nameof(EquipmentTypeMeasurement.EquipmentTypeId), nameof(EquipmentTypeMeasurement.MeasurementDefinitionId)],
            association.FindPrimaryKey()!.Properties.Select(property => property.Name));
        Assert.Equal(DeleteBehavior.Cascade, association.GetForeignKeys()
            .Single(foreignKey => foreignKey.Properties.Single().Name == nameof(EquipmentTypeMeasurement.EquipmentTypeId))
            .DeleteBehavior);
        Assert.Equal(DeleteBehavior.Restrict, association.GetForeignKeys()
            .Single(foreignKey => foreignKey.Properties.Single().Name == nameof(EquipmentTypeMeasurement.MeasurementDefinitionId))
            .DeleteBehavior);
        Assert.Equal(20, value.GetPrecision());
        Assert.Equal(6, value.GetScale());
        Assert.Equal("timestamp with time zone", timestamp.GetColumnType());

        Assert.Contains(reading.GetForeignKeys(), foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(Equipment) &&
            foreignKey.Properties.Select(property => property.Name).SequenceEqual([
                nameof(SensorReading.EquipmentId), nameof(SensorReading.EquipmentTypeId)
            ]));
        Assert.Contains(reading.GetForeignKeys(), foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(EquipmentTypeMeasurement) &&
            foreignKey.Properties.Select(property => property.Name).SequenceEqual([
                nameof(SensorReading.EquipmentTypeId), nameof(SensorReading.MeasurementDefinitionId)
            ]) && foreignKey.DeleteBehavior == DeleteBehavior.Restrict);
        Assert.Contains(reading.GetIndexes(), index => index.IsUnique && index.Properties.Select(property => property.Name)
            .SequenceEqual([nameof(SensorReading.SourceRowId), nameof(SensorReading.MeasurementDefinitionId)]) &&
            index.GetFilter() is not null);
    }

    [Fact]
    public void ImportBatchRowsHaveIdempotencyAndSourceLineageKeys()
    {
        using var context = CreateContext();
        var batch = context.Model.FindEntityType(typeof(CsvImportBatch))!;
        var batchRow = context.Model.FindEntityType(typeof(CsvImportBatchRow))!;
        var equipment = context.Model.FindEntityType(typeof(Equipment))!;

        Assert.Contains(batch.GetIndexes(), index => index.IsUnique && index.Properties.Select(property => property.Name)
            .SequenceEqual([
                nameof(CsvImportBatch.PlantId),
                nameof(CsvImportBatch.SourceFileSha256),
                nameof(CsvImportBatch.MappingProfileKey),
                nameof(CsvImportBatch.MappingProfileVersion)
            ]));
        Assert.Equal(
            [nameof(CsvImportBatchRow.Id)],
            batchRow.FindPrimaryKey()!.Properties.Select(property => property.Name));
        Assert.Contains(batchRow.GetIndexes(), index => index.IsUnique && index.Properties.Select(property => property.Name)
            .SequenceEqual([nameof(CsvImportBatchRow.ImportBatchId), nameof(CsvImportBatchRow.SourceRowNumber)]));
        Assert.Equal(DeleteBehavior.Restrict, batchRow.GetForeignKeys().Single().DeleteBehavior);
        Assert.Contains(equipment.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Select(property => property.Name).SequenceEqual([nameof(Equipment.SourceRowId)]) &&
            foreignKey.DeleteBehavior == DeleteBehavior.Restrict);
        Assert.Contains(context.Model.FindEntityType(typeof(EnergyRecord))!.GetIndexes(), index =>
            index.IsUnique && index.Properties.Select(property => property.Name).SequenceEqual([nameof(EnergyRecord.SourceRowId)]) &&
            index.GetFilter() is not null);
        var designTimeModel = context.GetService<IDesignTimeModel>().Model;
        Assert.Contains(designTimeModel.FindEntityType(typeof(EnergyRecord))!.GetCheckConstraints(), constraint =>
            constraint.Name == "ck_energy_records_period_order" && constraint.Sql.Contains("PeriodEndUtc") &&
            constraint.Sql.Contains("PeriodStartUtc"));
    }

    [Fact]
    public void EveryImportedEntityUsesOneNullableStableSourceRowForeignKey()
    {
        using var context = CreateContext();
        var importedEntities = new[]
        {
            typeof(Equipment), typeof(SensorReading), typeof(EnergyRecord), typeof(FailureRecord),
            typeof(MaintenanceRecord), typeof(Alert), typeof(RecommendedAction)
        };

        foreach (var clrType in importedEntities)
        {
            var entity = context.Model.FindEntityType(clrType)!;
            Assert.True(entity.FindProperty("SourceRowId")!.IsNullable, clrType.Name);
            Assert.Null(entity.FindProperty("ImportBatchId"));
            Assert.Null(entity.FindProperty("SourceRowNumber"));
            Assert.Contains(entity.GetForeignKeys(), foreignKey =>
                foreignKey.PrincipalEntityType.ClrType == typeof(CsvImportBatchRow) &&
                foreignKey.Properties.Select(property => property.Name).SequenceEqual(["SourceRowId"]) &&
                foreignKey.DeleteBehavior == DeleteBehavior.Restrict);
        }
    }

    [Fact]
    public void PredictionResultsStoreGenericModelOutputAndProvenanceMetadata()
    {
        using var context = CreateContext();
        var prediction = context.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(PredictionResult))!;

        Assert.Contains(prediction.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Select(property => property.Name).SequenceEqual([nameof(PredictionResult.EquipmentId)]) &&
            foreignKey.DeleteBehavior == DeleteBehavior.Restrict);
        Assert.Equal("timestamp with time zone", prediction.FindProperty(nameof(PredictionResult.PredictedAtUtc))!.GetColumnType());
        Assert.Equal("jsonb", prediction.FindProperty(nameof(PredictionResult.InputDataMetadataJson))!.GetColumnType());
        Assert.Equal(9, prediction.FindProperty(nameof(PredictionResult.Probability))!.GetPrecision());
        Assert.Contains(prediction.GetCheckConstraints(), constraint => constraint.Name == "ck_prediction_results_has_output");
        Assert.Contains(prediction.GetCheckConstraints(), constraint => constraint.Name == "ck_prediction_results_probability_range");
        Assert.Contains(prediction.GetCheckConstraints(), constraint => constraint.Name == "ck_prediction_results_nonnegative_horizon");
        Assert.Contains(prediction.GetIndexes(), index => index.Properties.Select(property => property.Name).SequenceEqual([
            nameof(PredictionResult.EquipmentId), nameof(PredictionResult.TargetKey), nameof(PredictionResult.PredictedAtUtc)
        ]));
    }
}