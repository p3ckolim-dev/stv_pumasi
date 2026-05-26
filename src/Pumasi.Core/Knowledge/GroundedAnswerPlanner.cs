using System.Text.Json;
using System.Text.Json.Serialization;
using Pumasi.Core.Configuration;

namespace Pumasi.Core.Knowledge;

public sealed class GroundedAnswerPlanner
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly AssistantConfig assistant;

    public GroundedAnswerPlanner(AssistantConfig assistant)
    {
        this.assistant = assistant;
    }

    public string BuildPrompt(string question, WikiContext context)
    {
        var sources = context.Sources.Select(source => new { source.Title, source.Url }).ToArray();
        return
            "You are pumasi (품앗이), a Stardew Valley helper NPC.\n" +
            "Answer in Korean.\n" +
            "Use only the Korean Stardew Valley Wiki context below.\n" +
            "If the context is insufficient, say you could not confirm it from the wiki.\n" +
            "Do not invent facts.\n" +
            "Keep the answer concise and practical.\n" +
            "Include source page titles at the end.\n" +
            "Do not enqueue or execute game tasks.\n" +
            "Return only JSON with this schema: { \"answer\": string, \"sources\": [{ \"title\": string, \"url\": string }], \"confidence\": \"grounded\" | \"insufficient\" }.\n" +
            $"Helper name: {assistant.Name}\n" +
            $"Question: {question}\n" +
            $"Available sources: {JsonSerializer.Serialize(sources, JsonOptions)}\n" +
            "Wiki context:\n" +
            context.ContextText;
    }

    public static GroundedAnswerResult ParseAnswer(string modelText)
    {
        var json = ExtractJsonObject(modelText);
        if (json is null)
            return GroundedAnswerResult.Fail("grounded-answer-missing-json");

        GroundedAnswerDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<GroundedAnswerDto>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            return GroundedAnswerResult.Fail($"grounded-answer-invalid-json: {ex.Message}");
        }

        if (dto is null || string.IsNullOrWhiteSpace(dto.Answer))
            return GroundedAnswerResult.Fail("grounded-answer-missing-answer");

        var sources = dto.Sources
            .Where(source => !string.IsNullOrWhiteSpace(source.Title) || !string.IsNullOrWhiteSpace(source.Url))
            .Select(source => new WikiSource(source.Title ?? "", source.Url ?? ""))
            .ToArray();

        return GroundedAnswerResult.Ok(dto.Answer.Trim(), sources, dto.Confidence ?? "grounded");
    }

    private static string? ExtractJsonObject(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewline = trimmed.IndexOf('\n');
            var lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            if (firstNewline >= 0 && lastFence > firstNewline)
                trimmed = trimmed[(firstNewline + 1)..lastFence].Trim();
        }

        var start = trimmed.IndexOf('{');
        var end = trimmed.LastIndexOf('}');
        return start >= 0 && end > start ? trimmed[start..(end + 1)] : null;
    }

    private sealed class GroundedAnswerDto
    {
        public string? Answer { get; set; }
        public List<GroundedSourceDto> Sources { get; set; } = new();
        public string? Confidence { get; set; }
    }

    private sealed class GroundedSourceDto
    {
        public string? Title { get; set; }
        public string? Url { get; set; }
    }
}

public sealed record GroundedAnswerResult(
    bool Success,
    string Answer,
    IReadOnlyList<WikiSource> Sources,
    string Confidence,
    string? Error)
{
    public static GroundedAnswerResult Ok(string answer, IReadOnlyList<WikiSource> sources, string confidence)
    {
        return new GroundedAnswerResult(true, answer, sources, confidence, null);
    }

    public static GroundedAnswerResult Fail(string error)
    {
        return new GroundedAnswerResult(false, "", Array.Empty<WikiSource>(), "insufficient", error);
    }
}
