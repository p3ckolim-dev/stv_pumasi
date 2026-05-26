using System.Net;
using Pumasi.Core.Knowledge;
using Xunit;

namespace Pumasi.Core.Tests.Knowledge;

public sealed class WikiClientTests
{
    [Fact]
    public async Task SearchAsync_UsesKoreanMediaWikiSearchEndpoint()
    {
        var handler = new RecordingHandler(
            "{\n" +
            "  \"query\": {\n" +
            "    \"search\": [\n" +
            "      { \"title\": \"딸기\", \"snippet\": \"딸기는 봄 작물입니다.\" }\n" +
            "    ]\n" +
            "  }\n" +
            "}");
        var client = new WikiClient(new HttpClient(handler), new WikiClientOptions());

        var result = await client.SearchAsync("딸기 씨앗", 3);

        Assert.True(result.Success);
        var page = Assert.Single(result.Value);
        Assert.Equal("딸기", page.Title);
        Assert.Equal("https://ko.stardewvalleywiki.com/%EB%94%B8%EA%B8%B0", page.Url);
        Assert.NotNull(handler.Request);
        Assert.Equal("https://ko.stardewvalleywiki.com/mediawiki/api.php?action=query&list=search&srsearch=%EB%94%B8%EA%B8%B0%20%EC%94%A8%EC%95%97&format=json&utf8=1&srlimit=3", handler.Request.RequestUri?.AbsoluteUri);
    }

    [Fact]
    public async Task GetExtractAsync_UsesKoreanMediaWikiExtractEndpoint()
    {
        var handler = new RecordingHandler(
            "{\n" +
            "  \"query\": {\n" +
            "    \"pages\": {\n" +
            "      \"123\": { \"title\": \"딸기\", \"extract\": \"딸기는 봄 달걀 축제에서 씨앗을 살 수 있습니다.\" }\n" +
            "    }\n" +
            "  }\n" +
            "}");
        var client = new WikiClient(new HttpClient(handler), new WikiClientOptions());

        var result = await client.GetExtractAsync("딸기");

        Assert.True(result.Success);
        Assert.Equal("딸기", result.Value.Title);
        Assert.Contains("봄 달걀 축제", result.Value.Extract);
        Assert.NotNull(handler.Request);
        Assert.Equal("https://ko.stardewvalleywiki.com/mediawiki/api.php?action=query&prop=extracts&explaintext=1&exintro=0&titles=%EB%94%B8%EA%B8%B0&format=json&utf8=1", handler.Request.RequestUri?.AbsoluteUri);
    }

    [Fact]
    public async Task SearchAsync_ReturnsFailureForHttpError()
    {
        var handler = new RecordingHandler("server down", HttpStatusCode.ServiceUnavailable);
        var client = new WikiClient(new HttpClient(handler), new WikiClientOptions());

        var result = await client.SearchAsync("딸기", 3);

        Assert.False(result.Success);
        Assert.Equal("wiki-http-503", result.Error);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly string response;
        private readonly HttpStatusCode statusCode;

        public RecordingHandler(string response, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            this.response = response;
            this.statusCode = statusCode;
        }

        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(response)
            });
        }
    }
}
