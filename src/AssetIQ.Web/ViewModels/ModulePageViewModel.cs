namespace AssetIQ.Web.ViewModels;

public class ModulePageViewModel
{
    public string ModuleKey { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string PageHeading { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public IReadOnlyList<string> PlannedCapabilities { get; init; } = Array.Empty<string>();
}
