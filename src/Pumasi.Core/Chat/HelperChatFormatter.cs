using System.Text.RegularExpressions;

namespace Pumasi.Core.Chat;

public static class HelperChatFormatter
{
    private const int MaxSourcesInChat = 3;
    private static readonly Regex WhitespaceRegex = new(@"\s+", RegexOptions.Compiled);

    public static IReadOnlyList<string> FormatAnswer(string helperName, string answer, IReadOnlyList<string> sources)
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
            var suffix = cleanSources.Length > shown.Length ? $" 외 {cleanSources.Length - shown.Length}개" : string.Empty;
            lines.Add($"출처: {string.Join(", ", shown)}{suffix}");
        }

        return lines;
    }

    private static string Normalize(string value)
    {
        return WhitespaceRegex.Replace(value.Trim(), " ");
    }
}
