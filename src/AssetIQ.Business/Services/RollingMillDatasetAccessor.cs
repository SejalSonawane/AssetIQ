using AssetIQ.Business.DTOs;
using AssetIQ.Business.Interfaces;
using AssetIQ.Business.Mapping;
using AssetIQ.Business.Validation;
using AssetIQ.Data.Import;
using AssetIQ.Data.Interfaces;
using AssetIQ.Data.Options;
using Microsoft.Extensions.Options;

namespace AssetIQ.Business.Services;

public sealed class RollingMillDatasetAccessor : IRollingMillDatasetAccessor
{
    private readonly IRollingMillCsvReader _csvReader;
    private readonly DatasetOptions _options;
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private DatasetSnapshotDto? _cachedSnapshot;

    public RollingMillDatasetAccessor(IRollingMillCsvReader csvReader, IOptions<DatasetOptions> options)
    {
        _csvReader = csvReader;
        _options = options.Value;
    }

    public async Task<DatasetSnapshotDto> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedSnapshot is not null)
        {
            return _cachedSnapshot;
        }

        await _loadLock.WaitAsync(cancellationToken);
        try
        {
            if (_cachedSnapshot is not null)
            {
                return _cachedSnapshot;
            }

            _cachedSnapshot = await LoadSnapshotAsync(cancellationToken);
            return _cachedSnapshot;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    private async Task<DatasetSnapshotDto> LoadSnapshotAsync(CancellationToken cancellationToken)
    {
        var path = _options.RollingMillCsvPath;
        var readResult = await _csvReader.ReadAsync(path, cancellationToken);

        var validationIssues = new List<DatasetValidationIssue>();
        foreach (var issue in readResult.Issues.Where(i => i.Severity == "Error"))
        {
            validationIssues.Add(new DatasetValidationIssue
            {
                RowNumber = issue.RowNumber,
                Field = issue.Field,
                Message = issue.Message,
                Severity = issue.Severity
            });
        }

        if (!readResult.FileFound || validationIssues.Count > 0)
        {
            return new DatasetSnapshotDto
            {
                IsLoaded = true,
                IsValid = false,
                SourceLabel = _options.PlantDisplayName,
                FilePath = path,
                ReadIssues = readResult.Issues,
                ValidationIssues = validationIssues
            };
        }

        validationIssues.AddRange(RollingMillDatasetValidator.ValidateRecords(readResult.Records));
        var hasErrors = validationIssues.Any(i => i.Severity == "Error");

        var equipment = readResult.Records
            .Select(r => RollingMillRecordMapper.ToEquipmentRecord(r, _options.PlantDisplayName))
            .ToList();

        return new DatasetSnapshotDto
        {
            IsLoaded = true,
            IsValid = !hasErrors,
            SourceLabel = _options.PlantDisplayName,
            FilePath = path,
            Equipment = equipment,
            ReadIssues = readResult.Issues,
            ValidationIssues = validationIssues,
            QualityReport = hasErrors ? null : RollingMillDatasetValidator.BuildQualityReport(readResult.Records)
        };
    }
}
