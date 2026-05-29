using System.Text.RegularExpressions;
using Pumasi.Core.Configuration;

namespace Pumasi.Core.Chat;

public static class HelperChatFormatter
{
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

    public static IReadOnlyList<string> FormatAnswer(
        string helperName,
        string answer,
        IReadOnlyList<string> sources,
        UiLanguage language = UiLanguage.Korean)
    {
        var name = Normalize(helperName);
        if (string.IsNullOrWhiteSpace(name))
            name = "pumasi";

        return new[] { $"{name}: {Normalize(answer)}" };
    }

    private static string Normalize(string value)
    {
        return WhitespaceRegex.Replace(value.Trim(), " ");
    }
}
