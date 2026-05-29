namespace Pumasi.Core.Ui;

public static class TodoOverlayText
{
    public static string FormatTitle(string helperName)
    {
        return string.IsNullOrWhiteSpace(helperName) ? "pumasi" : helperName.Trim();
    }
}
