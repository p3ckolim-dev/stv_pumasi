namespace Pumasi.Core.Knowledge;

public static class WikiSearchQueryBuilder
{
    private static readonly Dictionary<string, string> Aliases = new(StringComparer.Ordinal)
    {
        ["아비게일"] = "애비게일",
        ["에비게일"] = "애비게일"
    };

    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
    {
        "어디",
        "어디서",
        "언제",
        "어떻게",
        "무엇",
        "뭐",
        "뭐야",
        "무슨",
        "추천",
        "좋아하는",
        "좋아해",
        "사는",
        "사",
        "살",
        "얻는",
        "얻어",
        "얻나요",
        "법",
        "방법",
        "알려줘",
        "알려주세요",
        "있어",
        "있나요",
        "수",
        "할"
    };

    private static readonly char[] TrimChars =
    {
        '?',
        '!',
        '.',
        ',',
        ';',
        ':',
        '"',
        '\'',
        '(',
        ')',
        '[',
        ']',
        '{',
        '}'
    };

    public static IReadOnlyList<string> CreateCandidates(string query)
    {
        var normalized = Normalize(query);
        var candidates = new List<string>();
        AddCandidate(candidates, normalized);

        var aliased = ApplyAliases(normalized);
        AddCandidate(candidates, aliased);

        var tokens = Tokenize(aliased).ToArray();
        if (tokens.Length == 0)
            return candidates;

        AddCandidate(candidates, string.Join(" ", tokens));
        for (var length = Math.Min(3, tokens.Length); length >= 1; length--)
            AddCandidate(candidates, string.Join(" ", tokens.Take(length)));

        return candidates;
    }

    private static IEnumerable<string> Tokenize(string query)
    {
        return query
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(StripParticle)
            .Select(ApplyAliases)
            .Where(token => !string.IsNullOrWhiteSpace(token))
            .Where(token => !StopWords.Contains(token));
    }

    private static string Normalize(string query)
    {
        var normalized = query.Trim();
        foreach (var trimChar in TrimChars)
            normalized = normalized.Replace(trimChar, ' ');

        return string.Join(' ', normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    private static string StripParticle(string token)
    {
        var value = token.Trim();
        foreach (var suffix in new[] { "에서는", "에게는", "으로는", "에서는", "에서", "에게", "으로", "로", "은", "는", "이", "가", "을", "를", "의", "도", "만" })
        {
            if (value.Length > suffix.Length + 1 && value.EndsWith(suffix, StringComparison.Ordinal))
                return value[..^suffix.Length];
        }

        return value;
    }

    private static string ApplyAliases(string value)
    {
        foreach (var alias in Aliases)
            value = value.Replace(alias.Key, alias.Value, StringComparison.Ordinal);

        return value;
    }

    private static void AddCandidate(List<string> candidates, string candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
            return;

        candidate = candidate.Trim();
        if (!candidates.Contains(candidate, StringComparer.Ordinal))
            candidates.Add(candidate);
    }
}
