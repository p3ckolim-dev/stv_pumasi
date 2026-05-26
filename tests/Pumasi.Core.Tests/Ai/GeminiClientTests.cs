using System.Net;
using Pumasi.Core.Ai;
using Pumasi.Core.Configuration;
using Xunit;

namespace Pumasi.Core.Tests.Ai;

public sealed class GeminiClientTests
{
    [Fact]
    public async Task GenerateTextAsync_SendsApiKeyHeaderAndGeminiRequestBody()
    {
        var handler = new RecordingHandler(
            "{\n" +
            "  \"candidates\": [\n" +
            "    { \"content\": { \"parts\": [ { \"text\": \"{\\\"message\\\":\\\"ok\\\",\\\"tasks\\\":[]}\" } ] } }\n" +
            "  ]\n" +
            "}");
        var config = new GeminiConfig
        {
            ApiKey = "gemini-key",
            BaseUrl = "https://generativelanguage.googleapis.com/v1beta",
            Model = "gemini-2.5-flash"
        };
        var client = new GeminiClient(new HttpClient(handler), config);

        var text = await client.GenerateTextAsync("plan this farm");

        Assert.Equal("{\"message\":\"ok\",\"tasks\":[]}", text);
        Assert.NotNull(handler.Request);
        Assert.Equal(HttpMethod.Post, handler.Request.Method);
        Assert.Equal("https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent", handler.Request.RequestUri?.ToString());
        Assert.True(handler.Request.Headers.TryGetValues("x-goog-api-key", out var values));
        Assert.Equal("gemini-key", Assert.Single(values));
        Assert.Contains("plan this farm", handler.Body);
        Assert.Contains("responseMimeType", handler.Body);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly string response;

        public RecordingHandler(string response)
        {
            this.response = response;
        }

        public HttpRequestMessage? Request { get; private set; }
        public string Body { get; private set; } = "";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Request = request;
            Body = request.Content is null
                ? ""
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response)
            };
        }
    }
}
