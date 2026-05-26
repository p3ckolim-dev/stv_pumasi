namespace Pumasi.Core.Knowledge;

public sealed record WikiClientOptions(string BaseUrl = "https://ko.stardewvalleywiki.com");

public sealed record WikiSearchResult(string Title, string Url, string Snippet);

public sealed record WikiPageExtract(string Title, string Url, string Extract);

public sealed record WikiResult<T>(bool Success, T Value, string? Error)
{
    public static WikiResult<T> Ok(T value) => new(true, value, null);
    public static WikiResult<T> Fail(string error, T fallback) => new(false, fallback, error);
}
