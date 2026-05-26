using System.Text;

namespace Pumasi.Core.Knowledge;

public sealed record WikiContextOptions(int MaxPages = 3, int CharacterLimit = 8000);

public sealed record WikiSource(string Title, string Url);

public sealed record WikiContext(string ContextText, IReadOnlyList<WikiSource> Sources);

public sealed class WikiContextBuilder
{
    private readonly WikiContextOptions options;

    public WikiContextBuilder(WikiContextOptions options)
    {
        this.options = options;
    }

    public WikiContext Build(IEnumerable<WikiPageExtract> pages)
    {
        var selected = pages
            .Where(page => !string.IsNullOrWhiteSpace(page.Title))
            .Take(Math.Max(1, options.MaxPages))
            .ToArray();

        var sources = selected
            .Select(page => new WikiSource(page.Title, page.Url))
            .ToArray();

        var builder = new StringBuilder();
        foreach (var page in selected)
        {
            AppendWithinLimit(builder, $"# {page.Title}\nURL: {page.Url}\n{page.Extract.Trim()}\n\n");
            if (builder.Length >= options.CharacterLimit)
                break;
        }

        return new WikiContext(builder.ToString().Trim(), sources);
    }

    private void AppendWithinLimit(StringBuilder builder, string value)
    {
        var limit = Math.Max(1, options.CharacterLimit);
        var remaining = limit - builder.Length;
        if (remaining <= 0)
            return;

        builder.Append(value.Length <= remaining ? value : value[..remaining]);
    }
}
