using System.Text.RegularExpressions;
using Pumasi.Core.Configuration;
using Pumasi.Core.Ui;

namespace Pumasi.Core.Chat;

public static class HelperChatFormatter
{
    private const int MaxSourcesInChat = 3;
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

        var lines = new List<string> { $"{name}: {Normalize(answer)}" };
        var cleanSources = sources
            .Where(source => !string.IsNullOrWhiteSpace(source))
            .Select(Normalize)
            .Where(source => source.Length > 0)
            .ToArray();

        if (cleanSources.Length > 0)
        {
            var shown = cleanSources.Take(MaxSourcesInChat).ToArray();
            var suffix = FormatMoreSources(language, cleanSources.Length - shown.Length);
            lines.Add($"{PumasiText.Get(language, PumasiTextKey.SourcePrefix)}: {string.Join(", ", shown)}{suffix}");
        }

        return lines;
    }

    private static string FormatMoreSources(UiLanguage language, int count)
    {
        if (count <= 0)
            return string.Empty;

        return language == UiLanguage.English ? $" and {count} more" : $" 외 {count}개";
    }

    private static string Normalize(string value)
    {
        return WhitespaceRegex.Replace(value.Trim(), " ");
    }
}
