using System.Security.Cryptography;
using AssetIQ.Business.DTOs;
using AssetIQ.Business.Interfaces;
using AssetIQ.Data;
using AssetIQ.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using AssetIQ.Data.Options;

namespace AssetIQ.Business.Services;

public sealed class RollingMillCsvImportService : IRollingMillCsvImportService
{
    private const string MappingProfileKey = "RollingMillCsv";
    private const string MappingProfileVersion = "1";
    private const string UnknownCriticality = "Unknown";
    private const string UnknownOperatingStatus = "Unknown";

    private readonly AssetIQDbContext _dbContext;
    private readonly IRollingMillDatasetAccessor _datasetAccessor;
    private readonly DatasetOptions _options;

    public RollingMillCsvImportService(
        AssetIQDbContext dbContext,
        IRollingMillDatasetAccessor datasetAccessor,
        IOptions<DatasetOptions> options)
    {
        _dbContext = dbContext;
        _datasetAccessor = datasetAccessor;
        _options = options.Value;
    }

    public async Task<CsvImportResultDto> ImportConfiguredCsvAsync(
        CancellationToken cancellationToken = default)
    {
        var snapshot = await _datasetAccessor.GetSnapshotAsync(cancellationToken);
        if (!snapshot.IsValid)
        {
            return Failure("The configured CSV did not pass validation, so nothing was imported.", snapshot.SourceLabel, 0, 0, snapshot.ValidationIssues.Count);
        }

        var sourcePath = snapshot.FilePath;
        var fileBytes = await File.ReadAllBytesAsync(sourcePath, cancellationToken);
        var sourceHash = Convert.ToHexString(SHA256.HashData(fileBytes));
        var sourceFileName = Path.GetFileName(sourcePath);

        var plantName = _options.PlantDisplayName.Trim();
        if (plantName.Length == 0)
        {
            return Failure("The configured plant display name is required for importing equipment.", sourceFileName, snapshot.Equipment.Count, 0, snapshot.Equipment.Count);
        }

        var plant = await _dbContext.Plants.FirstOrDefaultAsync(existing => existing.PlantCode == plantName, cancellationToken);
        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);

        var plantIdForLookup = plant?.Id ?? Guid.Empty;
        var existingBatch = await _dbContext.CsvImportBatches
            .FirstOrDefaultAsync(batch =>
                batch.PlantId == plantIdForLookup &&
                batch.SourceFileSha256 == sourceHash &&
                batch.MappingProfileKey == MappingProfileKey &&
                batch.MappingProfileVersion == MappingProfileVersion,
                cancellationToken);

        if (existingBatch is not null)
        {
            return new CsvImportResultDto
            {
                Succeeded = true,
                Message = "This CSV content has already been imported for this plant; no duplicate equipment or import history was created.",
                SourceFileName = sourceFileName,
                SourceFileSha256 = sourceHash,
                TotalRowCount = existingBatch.TotalRowCount,
                AcceptedRowCount = existingBatch.AcceptedRowCount,
                RejectedRowCount = existingBatch.RejectedRowCount
            };
        }

        var now = DateTime.UtcNow;
        if (plant is null)
        {
            plant = new Plant
            {
                Id = Guid.NewGuid(),
                PlantCode = plantName,
                Name = plantName,
                TimeZoneId = "UTC",
                CreatedAtUtc = now
            };
            _dbContext.Plants.Add(plant);
        }

        var importBatch = new CsvImportBatch
        {
            Id = Guid.NewGuid(),
            PlantId = plant.Id,
            Plant = plant,
            SourceFileName = sourceFileName,
            SourceFileSha256 = sourceHash,
            MappingProfileKey = MappingProfileKey,
            MappingProfileVersion = MappingProfileVersion,
            Status = CsvImportBatchStatus.Pending,
            TotalRowCount = snapshot.Equipment.Count,
            StartedAtUtc = now
        };

        var rejectedReasons = new List<string>();
        var importedRows = new List<(CsvImportBatchRow Row, Equipment Equipment)>();

        foreach (var record in snapshot.Equipment)
        {
            var issue = ValidateRecord(record);
            var row = new CsvImportBatchRow
            {
                Id = Guid.NewGuid(),
                ImportBatchId = importBatch.Id,
                ImportBatch = importBatch,
                SourceRowNumber = record.SourceRowNumber,
                Status = issue is null ? CsvImportRowStatus.Accepted : CsvImportRowStatus.Rejected,
                ValidationMessage = issue,
                Classification = CsvImportRowClassification.Unclassified
            };

            importBatch.Rows.Add(row);
            if (issue is not null)
            {
                rejectedReasons.Add(issue);
                continue;
            }

            var equipmentType = await GetOrCreateEquipmentTypeAsync(record.EquipmentTypeKey, cancellationToken);
            var existingEquipment = await _dbContext.Equipment
                .FirstOrDefaultAsync(existing => existing.PlantId == plant.Id && existing.EquipmentCode == record.EquipmentCode, cancellationToken);

            if (existingEquipment is null)
            {
                var newEquipment = new Equipment
                {
                    Id = Guid.NewGuid(),
                    PlantId = plant.Id,
                    Plant = plant,
                    EquipmentTypeId = equipmentType.Id,
                    EquipmentType = equipmentType,
                    EquipmentCode = record.EquipmentCode,
                    Name = record.EquipmentCode,
                    Section = record.Section,
                    Criticality = UnknownCriticality,
                    OperatingStatus = UnknownOperatingStatus,
                    HealthScore = record.HealthScore,
                    RiskLevel = record.RiskLevel,
                    SourceRowId = row.Id,
                    SourceRow = row,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                };
                importedRows.Add((row, newEquipment));
                _dbContext.Equipment.Add(newEquipment);
            }
            else
            {
                existingEquipment.EquipmentTypeId = equipmentType.Id;
                existingEquipment.Section = record.Section;
                existingEquipment.HealthScore = record.HealthScore;
                existingEquipment.RiskLevel = record.RiskLevel;
                existingEquipment.SourceRowId = row.Id;
                existingEquipment.UpdatedAtUtc = now;
                importedRows.Add((row, existingEquipment));
            }
        }

        if (rejectedReasons.Count > 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Failure(
                $"Import rejected {rejectedReasons.Count} row(s); no database changes were saved.",
                sourceFileName,
                importBatch.TotalRowCount,
                0,
                rejectedReasons.Count,
                rejectedReasons);
        }

        _dbContext.CsvImportBatches.Add(importBatch);
        await _dbContext.SaveChangesAsync(cancellationToken);

        importBatch.Status = CsvImportBatchStatus.Imported;
        importBatch.AcceptedRowCount = importBatch.Rows.Count(row => row.Status == CsvImportRowStatus.Accepted);
        importBatch.RejectedRowCount = importBatch.Rows.Count(row => row.Status == CsvImportRowStatus.Rejected);
        importBatch.CompletedAtUtc = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new CsvImportResultDto
        {
            Succeeded = true,
            Message = $"Imported {importBatch.AcceptedRowCount} equipment rows from the configured CSV.",
            SourceFileName = sourceFileName,
            SourceFileSha256 = sourceHash,
            TotalRowCount = importBatch.TotalRowCount,
            AcceptedRowCount = importBatch.AcceptedRowCount,
            RejectedRowCount = importBatch.RejectedRowCount
        };
    }

    private static string? ValidateRecord(EquipmentRecordDto record)
    {
        if (record.SourceRowNumber <= 0)
        {
            return "The source row number is missing.";
        }

        if (string.IsNullOrWhiteSpace(record.EquipmentCode))
        {
            return "Equipment_ID is required.";
        }

        if (string.IsNullOrWhiteSpace(record.EquipmentTypeKey))
        {
            return "Equipment_Type is required.";
        }

        if (string.IsNullOrWhiteSpace(record.Section))
        {
            return "Section is required.";
        }

        return null;
    }

    // private async Task<EquipmentType> GetOrCreateEquipmentTypeAsync(string key, CancellationToken cancellationToken)
    // {
    //     var equipmentType = await _dbContext.EquipmentTypes
    //         .FirstOrDefaultAsync(existing => existing.Key == key, cancellationToken);
    //     if (equipmentType is not null)
    //     {
    //         return equipmentType;
    //     }

    //     equipmentType = new EquipmentType
    //     {
    //         Id = Guid.NewGuid(),
    //         Key = key,
    //         Name = key,
    //         IsActive = true
    //     };
    //     _dbContext.EquipmentTypes.Add(equipmentType);
    //     return equipmentType;
    // }


private async Task<EquipmentType> GetOrCreateEquipmentTypeAsync(
    string key,
    CancellationToken cancellationToken)
{
    key = key.Trim();

    // First check equipment types already tracked by Entity Framework,
    // including new types that have not yet been saved to PostgreSQL.
    var equipmentType = _dbContext.EquipmentTypes.Local
        .FirstOrDefault(existing =>
            string.Equals(existing.Key, key, StringComparison.OrdinalIgnoreCase));

    if (equipmentType is not null)
    {
        return equipmentType;
    }

    // Next check the database for an existing equipment type.
    equipmentType = await _dbContext.EquipmentTypes
        .FirstOrDefaultAsync(
            existing => existing.Key == key,
            cancellationToken);

    if (equipmentType is not null)
    {
        return equipmentType;
    }

    // Only create a type if it is not tracked or persisted.
    equipmentType = new EquipmentType
    {
        Id = Guid.NewGuid(),
        Key = key,
        Name = key,
        IsActive = true
    };

    _dbContext.EquipmentTypes.Add(equipmentType);

    return equipmentType;
}


    private static CsvImportResultDto Failure(
        string message,
        string sourceFileName,
        int totalRows,
        int acceptedRows,
        int rejectedRows,
        IReadOnlyList<string>? reasons = null) =>
        new()
        {
            Succeeded = false,
            Message = message,
            SourceFileName = sourceFileName,
            TotalRowCount = totalRows,
            AcceptedRowCount = acceptedRows,
            RejectedRowCount = rejectedRows,
            RejectedReasons = reasons ?? Array.Empty<string>()
        };
}
