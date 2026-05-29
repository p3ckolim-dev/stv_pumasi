namespace Pumasi.Core.Configuration;

public sealed class UiConfig
{
    public UiLanguage Language { get; set; } = UiLanguage.Korean;
    public bool ShowTodoOverlay { get; set; } = true;
    public string ToggleOverlayButton { get; set; } = "F8";
    public bool ShowHelperStatusNotifications { get; set; } = true;
}
