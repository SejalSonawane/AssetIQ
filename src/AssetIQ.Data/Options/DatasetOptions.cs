namespace AssetIQ.Data.Options;

public class DatasetOptions
{
    public const string SectionName = "AssetIQ:Dataset";

    /// <summary>Absolute path to the Rolling Mill CSV file.</summary>
    public string RollingMillCsvPath { get; set; } = string.Empty;

    /// <summary>Display label for the plant / dataset source (not a domain hardcode).</summary>
    public string PlantDisplayName { get; set; } = "Rolling Mill (dataset)";
}
