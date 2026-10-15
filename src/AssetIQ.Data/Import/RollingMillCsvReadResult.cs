namespace AssetIQ.Data.Import;

public sealed class RollingMillCsvReadResult
{
    public required string FilePath { get; init; }
    public bool FileFound { get; init; }
    public IReadOnlyList<RollingMillCsvRecord> Records { get; init; } = Array.Empty<RollingMillCsvRecord>();
    public IReadOnlyList<CsvReadIssue> Issues { get; init; } = Array.Empty<CsvReadIssue>();

    public bool IsSuccess => FileFound && Issues.All(i => i.Severity != "Error") && Records.Count > 0;
}
