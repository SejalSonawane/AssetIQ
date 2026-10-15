namespace AssetIQ.Business.Validation;

public sealed class DatasetValidationIssue
{
    public int? RowNumber { get; init; }
    public string Field { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Severity { get; init; } = "Error";
}
