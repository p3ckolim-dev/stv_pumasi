using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using StardewAIFarmHelper.Core.Configuration;

namespace StardewAIFarmHelper.Core.Ai;

public sealed class GeminiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly HttpClient httpClient;
    private readonly GeminiConfig config;

    public GeminiClient(HttpClient httpClient, GeminiConfig config)
    {
        this.httpClient = httpClient;
        this.config = config;
    }

    public async Task<string> GenerateTextAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (!config.IsConfigured)
            throw new InvalidOperationException("Gemini API key and model are required.");

        using var request = CreateRequest(prompt);
        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Gemini request failed with {(int)response.StatusCode}: {responseText}");

        var parsed = JsonSerializer.Deserialize<GeminiGenerateContentResponse>(responseText, JsonOptions);
        var text = parsed?.Candidates
            .SelectMany(candidate => candidate.Content?.Parts ?? new List<GeminiPart>())
            .Select(part => part.Text)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        return text ?? "";
    }

    internal HttpRequestMessage CreateRequest(string prompt)
    {
        var endpoint = $"{config.BaseUrl.TrimEnd('/')}/models/{Uri.EscapeDataString(config.Model)}:generateContent";
        var body = new
        {
            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = new
            {
                responseMimeType = "application/json"
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("x-goog-api-key", config.ApiKey);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }
}
