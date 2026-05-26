using System.Text.RegularExpressions;

namespace Pumasi.Core.Knowledge;

public sealed class WikiMemoryCache
{
    private readonly Dictionary<string, IReadOnlyList<WikiSearchResult>> searches = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, WikiPageExtract> extracts = new(StringComparer.OrdinalIgnoreCase);

    public bool TryGetSearch(string query, out IReadOnlyList<WikiSearchResult> results)
    {
        return searches.TryGetValue(Normalize(query), out results!);
    }

    public void SetSearch(string query, IReadOnlyList<WikiSearchResult> results)
    {
        searches[Normalize(query)] = results;
    }

    public bool TryGetExtract(string title, out WikiPageExtract extract)
    {
        return extracts.TryGetValue(Normalize(title), out extract!);
    }

    public void SetExtract(string title, WikiPageExtract extract)
    {
        extracts[Normalize(title)] = extract;
    }

    private static string Normalize(string value)
    {
        return Regex.Replace(value.Trim(), "\\s+", " ");
    }
}
