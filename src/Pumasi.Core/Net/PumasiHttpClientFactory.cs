namespace Pumasi.Core.Net;

public static class PumasiHttpClientFactory
{
    private const int MinimumTimeoutSeconds = 5;

    public static HttpClient Create(int timeoutSeconds)
    {
        return new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(Math.Max(MinimumTimeoutSeconds, timeoutSeconds))
        };
    }
}
