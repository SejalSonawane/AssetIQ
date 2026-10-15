using AssetIQ.Business.Validation;
using AssetIQ.Data.Import;

namespace AssetIQ.Business.DTOs;

public sealed class DatasetSnapshotDto
{
    public bool IsLoaded { get; init; }
    public bool IsValid { get; init; }
    public string SourceLabel { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
    public IReadOnlyList<EquipmentRecordDto> Equipment { get; init; } = Array.Empty<EquipmentRecordDto>();
    public IReadOnlyList<DatasetValidationIssue> ValidationIssues { get; init; } = Array.Empty<DatasetValidationIssue>();
    public IReadOnlyList<CsvReadIssue> ReadIssues { get; init; } = Array.Empty<CsvReadIssue>();
    public DatasetQualityReport? QualityReport { get; init; }
}
