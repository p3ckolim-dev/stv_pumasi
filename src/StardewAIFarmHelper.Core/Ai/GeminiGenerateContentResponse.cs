using System.Text.Json.Serialization;

namespace StardewAIFarmHelper.Core.Ai;

public sealed class GeminiGenerateContentResponse
{
    [JsonPropertyName("candidates")]
    public List<GeminiCandidate> Candidates { get; set; } = new();
}

public sealed class GeminiCandidate
{
    [JsonPropertyName("content")]
    public GeminiContent? Content { get; set; }
}

public sealed class GeminiContent
{
    [JsonPropertyName("parts")]
    public List<GeminiPart> Parts { get; set; } = new();
}

public sealed class GeminiPart
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}
