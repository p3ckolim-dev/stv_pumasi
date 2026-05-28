using System.Text.Encodings.Web;
using System.Text.Json;

namespace Pumasi.Core.Knowledge;

public enum ContextualIntentKind
{
    TaskPlanning,
    WikiAnswer,
    ChatAnswer,
    Clarify
}

public sealed record ConversationTurn(string Role, string Text);

public sealed record ContextualIntentResult(
    bool Success,
    ContextualIntentKind Intent,
    string RewrittenInput,
    string Answer,
    string? Error)
{
    public static ContextualIntentResult Ok(ContextualIntentKind intent, string rewrittenInput, string answer)
    {
        return new ContextualIntentResult(true, intent, rewrittenInput, answer, null);
    }

    public static ContextualIntentResult Fail(string error)
    {
        return new ContextualIntentResult(false, ContextualIntentKind.Clarify, string.Empty, string.Empty, error);
    }
}

public static class ContextualIntentRouter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNameCaseInsensitive = true
    };

    public static string BuildPrompt(
        string instruction,
        IReadOnlyList<ConversationTurn> conversation,
        IReadOnlyList<string> currentTodos)
    {
        var payload = new
        {
            instruction,
            recentConversation = conversation.TakeLast(8),
            currentTodos = currentTodos.Take(12)
        };

        return
            "You are the conversation router for pumasi (품앗이), a Stardew Valley helper mod.\n" +
            "Use the recent conversation and current todo list to infer what the player means.\n" +
            "Return only JSON. Do not include markdown.\n" +
            "Choose exactly one intent:\n" +
            "- TaskPlanning: the player wants pumasi to do or plan farm work.\n" +
            "- WikiAnswer: the player asks for Stardew Valley information, advice, prices, locations, schedules, gifts, or recommendations.\n" +
            "- ChatAnswer: the player is greeting, thanking, reacting, or asking a casual non-world-changing question.\n" +
            "- Clarify: only when the request is truly impossible to infer or would be unsafe to execute.\n" +
            "Do not choose Clarify unless context is insufficient after considering pronouns like 'that', 'it', '그거', '저거', '응', and '그래'.\n" +
            "For TaskPlanning or WikiAnswer, rewrite vague input into a clear Korean instruction/question.\n" +
            "For ChatAnswer, provide a short friendly Korean answer.\n" +
            "Schema:\n" +
            "{\n" +
            "  \"intent\": \"TaskPlanning|WikiAnswer|ChatAnswer|Clarify\",\n" +
            "  \"rewrittenInput\": \"clear request or question\",\n" +
            "  \"answer\": \"short response for ChatAnswer or Clarify; empty for other intents\"\n" +
            "}\n" +
            "Routing context:\n" +
            JsonSerializer.Serialize(payload, JsonOptions);
    }

    public static ContextualIntentResult ParseResponse(string modelText)
    {
        var json = ExtractJsonObject(modelText);
        if (json is null)
            return ContextualIntentResult.Fail("contextual-router-response-did-not-contain-json-object");

        ContextualIntentDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<ContextualIntentDto>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            return ContextualIntentResult.Fail($"invalid-contextual-router-json: {ex.Message}");
        }

        if (dto is null || string.IsNullOrWhiteSpace(dto.Intent))
            return ContextualIntentResult.Fail("empty-contextual-router-intent");

        if (!Enum.TryParse<ContextualIntentKind>(dto.Intent, ignoreCase: true, out var intent))
            return ContextualIntentResult.Fail("unsupported-contextual-intent");

        return ContextualIntentResult.Ok(intent, dto.RewrittenInput?.Trim() ?? string.Empty, dto.Answer?.Trim() ?? string.Empty);
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

    private sealed class ContextualIntentDto
    {
        public string Intent { get; set; } = "";
        public string? RewrittenInput { get; set; }
        public string? Answer { get; set; }
    }
}
