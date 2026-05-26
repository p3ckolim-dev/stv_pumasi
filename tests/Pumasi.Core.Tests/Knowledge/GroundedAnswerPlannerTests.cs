using Pumasi.Core.Configuration;
using Pumasi.Core.Knowledge;
using Xunit;

namespace Pumasi.Core.Tests.Knowledge;

public sealed class GroundedAnswerPlannerTests
{
    [Fact]
    public void BuildPrompt_InstructsModelToUseOnlyKoreanWikiContext()
    {
        var planner = new GroundedAnswerPlanner(new AssistantConfig { Name = "품앗이" });
        var context = new WikiContext(
            "# 딸기\nURL: https://ko.stardewvalleywiki.com/딸기\n딸기는 봄 달걀 축제에서 씨앗을 살 수 있습니다.",
            new[] { new WikiSource("딸기", "https://ko.stardewvalleywiki.com/딸기") });

        var prompt = planner.BuildPrompt("딸기 씨앗은 어디서 사?", context);

        Assert.Contains("Use only the Korean Stardew Valley Wiki context", prompt);
        Assert.Contains("Answer in Korean", prompt);
        Assert.Contains("딸기 씨앗은 어디서 사?", prompt);
        Assert.Contains("봄 달걀 축제", prompt);
    }

    [Fact]
    public void ParseAnswer_ReturnsGroundedAnswerWithSources()
    {
        var result = GroundedAnswerPlanner.ParseAnswer(
            "{\n" +
            "  \"answer\": \"딸기 씨앗은 봄 달걀 축제에서 살 수 있어요.\",\n" +
            "  \"sources\": [ { \"title\": \"딸기\", \"url\": \"https://ko.stardewvalleywiki.com/딸기\" } ],\n" +
            "  \"confidence\": \"grounded\"\n" +
            "}");

        Assert.True(result.Success);
        Assert.Equal("딸기 씨앗은 봄 달걀 축제에서 살 수 있어요.", result.Answer);
        var source = Assert.Single(result.Sources);
        Assert.Equal("딸기", source.Title);
        Assert.Equal("https://ko.stardewvalleywiki.com/딸기", source.Url);
    }

    [Fact]
    public void ParseAnswer_RejectsMissingAnswerText()
    {
        var result = GroundedAnswerPlanner.ParseAnswer("{ \"sources\": [] }");

        Assert.False(result.Success);
        Assert.Equal("grounded-answer-missing-answer", result.Error);
    }
}
