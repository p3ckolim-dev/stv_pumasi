namespace Pumasi.Core.Knowledge;

public sealed class KnowledgeIntentClassifier
{
    private static readonly string[] TaskKeywords =
    {
        "수확",
        "물줘",
        "물 주",
        "수거",
        "모아",
        "정리해",
        "실행",
        "collect",
        "harvest",
        "water",
        "scan"
    };

    private static readonly string[] WikiKeywords =
    {
        "무엇",
        "어디",
        "언제",
        "어떻게",
        "추천",
        "좋아하는",
        "가격",
        "조건",
        "얻는 법",
        "위치",
        "where",
        "when",
        "what",
        "how"
    };

    private static readonly string[] AssistantConversationKeywords =
    {
        "너",
        "넌",
        "너는",
        "품앗이",
        "pumasi",
        "봇",
        "도우미",
        "할 수 있어",
        "누구"
    };

    public KnowledgeIntent Classify(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return KnowledgeIntent.Ambiguous;

        var normalized = input.Trim().ToLowerInvariant();
        if (normalized.Contains("어떻게 할까", StringComparison.OrdinalIgnoreCase))
            return KnowledgeIntent.Ambiguous;

        var hasTaskSignal = TaskKeywords.Any(keyword => normalized.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        var hasWikiSignal = WikiKeywords.Any(keyword => normalized.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        var hasAssistantConversationSignal = AssistantConversationKeywords.Any(keyword => normalized.Contains(keyword, StringComparison.OrdinalIgnoreCase));

        if (hasAssistantConversationSignal && !hasTaskSignal)
            return KnowledgeIntent.Ambiguous;

        if (hasTaskSignal && !hasWikiSignal)
            return KnowledgeIntent.TaskPlanning;

        if (hasWikiSignal && !hasTaskSignal)
            return KnowledgeIntent.WikiAnswer;

        if (hasTaskSignal && hasWikiSignal)
        {
            if (normalized.Contains("추천", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("뭐", StringComparison.OrdinalIgnoreCase)
                || normalized.Contains("어떻게", StringComparison.OrdinalIgnoreCase))
                return KnowledgeIntent.Ambiguous;

            return KnowledgeIntent.TaskPlanning;
        }

        return KnowledgeIntent.Ambiguous;
    }
}
