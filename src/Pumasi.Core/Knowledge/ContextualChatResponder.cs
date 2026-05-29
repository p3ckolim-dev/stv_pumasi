using System.Text.Encodings.Web;
using System.Text.Json;

namespace Pumasi.Core.Knowledge;

public static class ContextualChatResponder
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
            "You are Pumasi (pumasi, 품앗이), a friendly Stardew Valley farm helper.\n" +
            "Answer naturally in Korean using the recent conversation and current todo list as context.\n" +
            "Stay strictly within Stardew Valley, the current farm, multiplayer farm play, farm chores, todos, and Pumasi helper conversation.\n" +
            "If the player asks about unrelated real-world or general topics, do not answer that topic; briefly guide them back to Stardew Valley or farm help.\n" +
            "Do not output JSON, routing labels, intent names, markdown, citations, or source lists.\n" +
            "If the player is casually chatting, respond casually.\n" +
            "If the player asks about a vague previous topic, infer it from context when safe.\n" +
            "If a vague request would change the world or spend resources and context is not enough, ask one short natural follow-up question.\n" +
            "Context:\n" +
            JsonSerializer.Serialize(payload, JsonOptions);
    }

    public static string CleanAnswer(string modelText)
    {
        if (string.IsNullOrWhiteSpace(modelText))
            return string.Empty;

        var answer = modelText.Trim();
        if (answer.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewline = answer.IndexOf('\n');
            var lastFence = answer.LastIndexOf("```", StringComparison.Ordinal);
            if (firstNewline >= 0 && lastFence > firstNewline)
                answer = answer[(firstNewline + 1)..lastFence].Trim();
        }

        return answer.Trim();
    }
}
