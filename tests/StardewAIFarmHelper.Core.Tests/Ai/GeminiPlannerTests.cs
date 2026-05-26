using StardewAIFarmHelper.Core.Ai;
using StardewAIFarmHelper.Core.Tasks;
using Xunit;

namespace StardewAIFarmHelper.Core.Tests.Ai;

public sealed class GeminiPlannerTests
{
    [Fact]
    public void ParsePlan_ExtractsTasksFromJsonText()
    {
        var result = GeminiPlanner.ParsePlan(
            "```json\n" +
            "{\n" +
            "  \"message\": \"온실부터 처리할게요.\",\n" +
            "  \"tasks\": [\n" +
            "    {\n" +
            "      \"type\": \"HarvestCrop\",\n" +
            "      \"location\": \"Greenhouse\",\n" +
            "      \"tile\": { \"x\": 10, \"y\": 8 },\n" +
            "      \"priority\": 90,\n" +
            "      \"reason\": \"수확 가능\",\n" +
            "      \"source\": \"gemini\"\n" +
            "    }\n" +
            "  ]\n" +
            "}\n" +
            "```");

        Assert.True(result.Success);
        Assert.Equal("온실부터 처리할게요.", result.Message);
        var task = Assert.Single(result.Tasks);
        Assert.Equal(TaskType.HarvestCrop, task.Type);
        Assert.Equal("Greenhouse", task.Target.Location);
        Assert.Equal(10, task.Target.X);
        Assert.Equal(8, task.Target.Y);
        Assert.Equal("HarvestCrop:Greenhouse:10,8", task.Key);
    }

    [Fact]
    public void ParsePlan_ReturnsFailureForMalformedText()
    {
        var result = GeminiPlanner.ParsePlan("not json at all");

        Assert.False(result.Success);
        Assert.Equal("gemini-response-did-not-contain-json-object", result.Error);
    }
}
