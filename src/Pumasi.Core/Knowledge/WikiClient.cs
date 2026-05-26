using System.Text.Json;
using System.Text.RegularExpressions;

namespace Pumasi.Core.Knowledge;

public sealed class WikiClient
{
    private readonly HttpClient httpClient;
    private readonly WikiClientOptions options;

    public WikiClient(HttpClient httpClient, WikiClientOptions options)
    {
        this.httpClient = httpClient;
        this.options = options;
    }

    public async Task<WikiResult<IReadOnlyList<WikiSearchResult>>> SearchAsync(
        string query,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var requestUri = BuildSearchUri(query, limit);
        try
        {
            using var response = await httpClient.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return WikiResult<IReadOnlyList<WikiSearchResult>>.Fail($"wiki-http-{(int)response.StatusCode}", Array.Empty<WikiSearchResult>());

            using var document = JsonDocument.Parse(body);
            var results = new List<WikiSearchResult>();
            if (document.RootElement.TryGetProperty("query", out var queryElement)
                && queryElement.TryGetProperty("search", out var searchElement)
                && searchElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in searchElement.EnumerateArray())
                {
                    var title = item.TryGetProperty("title", out var titleElement) ? titleElement.GetString() ?? "" : "";
                    var snippet = item.TryGetProperty("snippet", out var snippetElement) ? StripHtml(snippetElement.GetString() ?? "") : "";
                    if (!string.IsNullOrWhiteSpace(title))
                        results.Add(new WikiSearchResult(title, BuildPageUrl(title), snippet));
                }
            }

            return WikiResult<IReadOnlyList<WikiSearchResult>>.Ok(results);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return WikiResult<IReadOnlyList<WikiSearchResult>>.Fail("wiki-search-failed", Array.Empty<WikiSearchResult>());
        }
    }

    public async Task<WikiResult<WikiPageExtract>> GetExtractAsync(
        string title,
        CancellationToken cancellationToken = default)
    {
        var fallback = new WikiPageExtract(title, BuildPageUrl(title), "");
        var requestUri = BuildExtractUri(title);
        try
        {
            using var response = await httpClient.GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return WikiResult<WikiPageExtract>.Fail($"wiki-http-{(int)response.StatusCode}", fallback);

            using var document = JsonDocument.Parse(body);
            if (!document.RootElement.TryGetProperty("query", out var queryElement)
                || !queryElement.TryGetProperty("pages", out var pagesElement)
                || pagesElement.ValueKind != JsonValueKind.Object)
            {
                return WikiResult<WikiPageExtract>.Fail("wiki-extract-missing-pages", fallback);
            }

            foreach (var page in pagesElement.EnumerateObject())
            {
                var pageElement = page.Value;
                var pageTitle = pageElement.TryGetProperty("title", out var titleElement)
                    ? titleElement.GetString() ?? title
                    : title;
                var extract = pageElement.TryGetProperty("extract", out var extractElement)
                    ? extractElement.GetString() ?? ""
                    : "";

                return WikiResult<WikiPageExtract>.Ok(new WikiPageExtract(pageTitle, BuildPageUrl(pageTitle), extract.Trim()));
            }

            return WikiResult<WikiPageExtract>.Fail("wiki-extract-empty", fallback);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return WikiResult<WikiPageExtract>.Fail("wiki-extract-failed", fallback);
        }
    }

    internal string BuildSearchUri(string query, int limit)
    {
        return $"{ApiEndpoint()}?action=query&list=search&srsearch={Uri.EscapeDataString(query)}&format=json&utf8=1&srlimit={Math.Max(1, limit)}";
    }

    internal string BuildExtractUri(string title)
    {
        return $"{ApiEndpoint()}?action=query&prop=extracts&explaintext=1&exintro=0&titles={Uri.EscapeDataString(title)}&format=json&utf8=1";
    }

    private string ApiEndpoint() => $"{options.BaseUrl.TrimEnd('/')}/mediawiki/api.php";

    private string BuildPageUrl(string title)
    {
        return $"{options.BaseUrl.TrimEnd('/')}/{Uri.EscapeDataString(title).Replace("%20", "_", StringComparison.Ordinal)}";
    }

    private static string StripHtml(string value)
    {
        return Regex.Replace(value, "<.*?>", "").Trim();
    }
}
