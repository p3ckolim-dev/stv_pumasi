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
            "    \"pages\": [\n" +
            "      {\n" +
            "        \"title\": \"딸기\",\n" +
            "        \"revisions\": [\n" +
            "          { \"slots\": { \"main\": { \"content\": \"'''딸기'''는 [[딸기 씨앗]]에서 자랍니다. 봄 달걀 축제에서 씨앗을 살 수 있습니다.\" } } }\n" +
            "        ]\n" +
            "      }\n" +
            "    ]\n" +
            "  }\n" +
            "}");
        var client = new WikiClient(new HttpClient(handler), new WikiClientOptions());

        var result = await client.GetExtractAsync("딸기");

        Assert.True(result.Success);
        Assert.Equal("딸기", result.Value.Title);
        Assert.Contains("봄 달걀 축제", result.Value.Extract);
        Assert.NotNull(handler.Request);
        Assert.Equal("https://ko.stardewvalleywiki.com/mediawiki/api.php?action=query&prop=revisions&rvprop=content&rvslots=main&titles=%EB%94%B8%EA%B8%B0&format=json&formatversion=2&utf8=1", handler.Request.RequestUri?.AbsoluteUri);
    }

    [Fact]
    public async Task SearchAsync_TriesSimplifiedKoreanQueryWhenNaturalQuestionHasNoResults()
    {
        var handler = new RecordingHandler(
            "{ \"query\": { \"search\": [] } }",
            "{ \"query\": { \"search\": [ { \"title\": \"딸기 씨앗\", \"snippet\": \"\" } ] } }");
        var client = new WikiClient(new HttpClient(handler), new WikiClientOptions());

        var result = await client.SearchAsync("딸기 씨앗은 어디서 사?", 3);

        Assert.True(result.Success);
        var page = Assert.Single(result.Value);
        Assert.Equal("딸기 씨앗", page.Title);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Contains("srsearch=%EB%94%B8%EA%B8%B0%20%EC%94%A8%EC%95%97", handler.Requests[1].RequestUri?.AbsoluteUri);
    }

    [Fact]
    public async Task SearchAsync_AppliesCommonKoreanNameAliasFallbacks()
    {
        var handler = new RecordingHandler(
            "{ \"query\": { \"search\": [] } }",
            "{ \"query\": { \"search\": [] } }",
            "{ \"query\": { \"search\": [ { \"title\": \"애비게일\", \"snippet\": \"\" } ] } }");
        var client = new WikiClient(new HttpClient(handler), new WikiClientOptions());

        var result = await client.SearchAsync("아비게일 좋아하는 선물", 3);

        Assert.True(result.Success);
        var page = Assert.Single(result.Value);
        Assert.Equal("애비게일", page.Title);
        Assert.Equal(3, handler.Requests.Count);
        Assert.Contains("srsearch=%EC%95%A0%EB%B9%84%EA%B2%8C%EC%9D%BC", handler.Requests[2].RequestUri?.AbsoluteUri);
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
        private readonly HttpStatusCode statusCode;

        public RecordingHandler(string response, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            responses = new Queue<string>(new[] { response });
            this.statusCode = statusCode;
        }

        public HttpRequestMessage? Request { get; private set; }
        public IReadOnlyList<HttpRequestMessage> Requests => requests;

        private readonly List<HttpRequestMessage> requests = new();

        public RecordingHandler(params string[] responses)
        {
            this.responses = new Queue<string>(responses.Length == 0 ? new[] { "{}" } : responses);
            statusCode = HttpStatusCode.OK;
        }

        private readonly Queue<string> responses;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            requests.Add(request);
            var responseBody = responses.Count > 1 ? responses.Dequeue() : responses.Peek();
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody)
            });
        }
    }
}
