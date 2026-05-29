using Pumasi.Core.Knowledge;
using Xunit;

namespace Pumasi.Core.Tests.Knowledge;

public sealed class ContextualChatResponderTests
{
    [Fact]
    public void BuildPrompt_AsksForDirectContextualAnswerInsteadOfRoutingJson()
    {
        var prompt = ContextualChatResponder.BuildPrompt(
            "그거 어떻게 생각해?",
            new[]
            {
                new ConversationTurn("user", "온실에 딸기 심을까?"),
                new ConversationTurn("assistant", "봄에는 딸기가 좋아요.")
            },
            new[] { "#1 HarvestCrop:Greenhouse:10,8 [Queued]" });

        Assert.Contains("그거 어떻게 생각해?", prompt);
        Assert.Contains("온실에 딸기 심을까?", prompt);
        Assert.Contains("HarvestCrop:Greenhouse:10,8", prompt);
        Assert.Contains("Answer naturally", prompt);
        Assert.Contains("Stardew Valley", prompt);
        Assert.Contains("pumasi", prompt);
        Assert.DoesNotContain("\"intent\"", prompt);
        Assert.DoesNotContain("Return only JSON", prompt);
    }

    [Fact]
    public void CleanAnswer_RemovesMarkdownFenceAndWhitespace()
    {
        var answer = ContextualChatResponder.CleanAnswer("```text\n농장 일부터 도와줄게요.\n```");

        Assert.Equal("농장 일부터 도와줄게요.", answer);
    }
}
