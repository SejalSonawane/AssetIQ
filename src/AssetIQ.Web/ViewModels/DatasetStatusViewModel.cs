using AssetIQ.Business.DTOs;
using AssetIQ.Business.Validation;

namespace AssetIQ.Web.ViewModels;

public class DatasetStatusViewModel
{
    public bool IsValid { get; init; }
    public string SourceLabel { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
    public IReadOnlyList<DatasetValidationIssue> ValidationIssues { get; init; } = Array.Empty<DatasetValidationIssue>();
    public DatasetQualityReport? QualityReport { get; init; }

    public static DatasetStatusViewModel FromSnapshot(DatasetSnapshotDto snapshot) =>
        new()
        {
            IsValid = snapshot.IsValid,
            SourceLabel = snapshot.SourceLabel,
            FilePath = snapshot.FilePath,
            ValidationIssues = snapshot.ValidationIssues,
            QualityReport = snapshot.QualityReport
        };
}
