using Pumasi.Core.Ai;
using Pumasi.Core.Tasks;
using Xunit;

namespace Pumasi.Core.Tests.Ai;

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

    [Fact]
    public void ParsePlan_ExtractsSprinklerTillingAndHayRefillTasks()
    {
        var result = GeminiPlanner.ParsePlan(
            "{\n" +
            "  \"message\": \"새 작업을 추가할게요.\",\n" +
            "  \"tasks\": [\n" +
            "    {\n" +
            "      \"type\": \"TillSprinklerSoil\",\n" +
            "      \"location\": \"Farm\",\n" +
            "      \"tile\": { \"x\": 40, \"y\": 18 },\n" +
            "      \"priority\": 55,\n" +
            "      \"reason\": \"스프링클러 주변 일반 땅\",\n" +
            "      \"source\": \"gemini\"\n" +
            "    },\n" +
            "    {\n" +
            "      \"type\": \"RefillHay\",\n" +
            "      \"location\": \"Barn\",\n" +
            "      \"tile\": { \"x\": 0, \"y\": 0 },\n" +
            "      \"priority\": 65,\n" +
            "      \"reason\": \"건초 리필\",\n" +
            "      \"source\": \"gemini\"\n" +
            "    }\n" +
            "  ]\n" +
            "}");

        Assert.True(result.Success);
        Assert.Collection(
            result.Tasks,
            task => Assert.Equal(TaskType.TillSprinklerSoil, task.Type),
            task => Assert.Equal(TaskType.RefillHay, task.Type));
    }

    [Fact]
    public void ParsePlan_ExtractsAnimalTasksWithTargets()
    {
        var result = GeminiPlanner.ParsePlan(
            "{\n" +
            "  \"message\": \"동물 일을 처리할게요.\",\n" +
            "  \"tasks\": [\n" +
            "    {\n" +
            "      \"type\": \"PetAnimal\",\n" +
            "      \"location\": \"Barn\",\n" +
            "      \"tile\": { \"x\": 12, \"y\": 8 },\n" +
            "      \"entityId\": \"123456789\",\n" +
            "      \"objectName\": \"Miso\",\n" +
            "      \"priority\": 72,\n" +
            "      \"reason\": \"아직 쓰다듬지 않은 동물\",\n" +
            "      \"source\": \"gemini\"\n" +
            "    },\n" +
            "    {\n" +
            "      \"type\": \"CollectAnimalProduct\",\n" +
            "      \"location\": \"Coop\",\n" +
            "      \"tile\": { \"x\": 6, \"y\": 4 },\n" +
            "      \"objectName\": \"Egg\",\n" +
            "      \"priority\": 82,\n" +
            "      \"reason\": \"바닥에 놓인 동물 생산품\",\n" +
            "      \"source\": \"gemini\"\n" +
            "    }\n" +
            "  ]\n" +
            "}");

        Assert.True(result.Success);
        Assert.Collection(
            result.Tasks,
            task =>
            {
                Assert.Equal(TaskType.PetAnimal, task.Type);
                Assert.Equal("123456789", task.Target.EntityId);
                Assert.Equal("Miso", task.Target.ObjectName);
            },
            task =>
            {
                Assert.Equal(TaskType.CollectAnimalProduct, task.Type);
                Assert.Equal("Egg", task.Target.ObjectName);
            });
    }

    [Fact]
    public void SelectCandidateTasks_RejectsPlannedTasksThatWereNotScanned()
    {
        var candidates = new[]
        {
            new TaskProposal(
                TaskType.CollectAnimalProduct,
                new TaskTarget("Coop", 6, 4, ObjectName: "Egg"),
                82,
                "Loose animal product Egg",
                "scan")
        };
        var planned = new[]
        {
            new TaskProposal(
                TaskType.CollectAnimalProduct,
                new TaskTarget("Coop", 6, 4, ObjectName: "Egg"),
                100,
                "Gemini chose the scanned egg",
                "gemini"),
            new TaskProposal(
                TaskType.PetAnimal,
                new TaskTarget("Barn", 12, 8, EntityId: "123456789", ObjectName: "Miso"),
                72,
                "Gemini invented an animal outside candidateTasks",
                "gemini")
        };

        var selected = GeminiPlanner.SelectCandidateTasks(planned, candidates);

        var task = Assert.Single(selected);
        Assert.Equal("CollectAnimalProduct:Coop:6,4:Egg", task.Key);
        Assert.Equal(100, task.Priority);
        Assert.Equal("gemini", task.Source);
    }
}
