namespace AssetIQ.Web.ViewModels;

public class NavigationItem
{
    public string Label { get; init; } = string.Empty;
    public string Controller { get; init; } = string.Empty;
    public string Action { get; init; } = "Index";
    public string Icon { get; init; } = string.Empty;
    public string ModuleKey { get; init; } = string.Empty;
}
