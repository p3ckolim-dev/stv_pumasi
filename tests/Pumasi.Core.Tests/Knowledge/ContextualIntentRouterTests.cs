using Pumasi.Core.Knowledge;
using Xunit;

namespace Pumasi.Core.Tests.Knowledge;

public sealed class ContextualIntentRouterTests
{
    [Fact]
    public void BuildPrompt_IncludesConversationContextAndInstruction()
    {
        var prompt = ContextualIntentRouter.BuildPrompt(
            "그거 해줘",
            new[]
            {
                new ConversationTurn("user", "온실 수확할까?"),
                new ConversationTurn("assistant", "온실 작물이 준비되어 있어요.")
            },
            new[] { "HarvestCrop:Greenhouse:10,8 [Queued]" });

        Assert.Contains("그거 해줘", prompt);
        Assert.Contains("온실 수확할까?", prompt);
        Assert.Contains("HarvestCrop:Greenhouse:10,8", prompt);
        Assert.Contains("Do not choose Clarify unless", prompt);
    }

    [Theory]
    [InlineData("TaskPlanning", ContextualIntentKind.TaskPlanning)]
    [InlineData("WikiAnswer", ContextualIntentKind.WikiAnswer)]
    [InlineData("ChatAnswer", ContextualIntentKind.ChatAnswer)]
    [InlineData("Clarify", ContextualIntentKind.Clarify)]
    public void ParseResponse_ParsesSupportedIntentKinds(string intent, ContextualIntentKind expected)
    {
        var result = ContextualIntentRouter.ParseResponse(
            "{\n" +
            $"  \"intent\": \"{intent}\",\n" +
            "  \"rewrittenInput\": \"온실 수확해줘\",\n" +
            "  \"answer\": \"알겠어요.\"\n" +
            "}");

        Assert.True(result.Success);
        Assert.Equal(expected, result.Intent);
        Assert.Equal("온실 수확해줘", result.RewrittenInput);
        Assert.Equal("알겠어요.", result.Answer);
    }

    [Fact]
    public void ParseResponse_ReturnsFailureForUnsupportedIntent()
    {
        var result = ContextualIntentRouter.ParseResponse("{ \"intent\": \"Maybe\", \"answer\": \"?\" }");

        Assert.False(result.Success);
        Assert.Equal("unsupported-contextual-intent", result.Error);
    }
}
