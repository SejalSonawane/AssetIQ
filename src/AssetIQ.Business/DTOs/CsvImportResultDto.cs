namespace AssetIQ.Business.DTOs;

public sealed class CsvImportResultDto
{
    public bool Succeeded { get; init; }
    public string Message { get; init; } = string.Empty;
    public int TotalRowCount { get; init; }
    public int AcceptedRowCount { get; init; }
    public int RejectedRowCount { get; init; }
    public string SourceFileName { get; init; } = string.Empty;
    public string SourceFileSha256 { get; init; } = string.Empty;
    public IReadOnlyList<string> RejectedReasons { get; init; } = Array.Empty<string>();
}
