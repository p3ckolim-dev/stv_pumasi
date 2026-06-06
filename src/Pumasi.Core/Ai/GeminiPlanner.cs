using System.Text.Json;
using System.Text.Json.Serialization;
using Pumasi.Core.Configuration;
using Pumasi.Core.Tasks;

namespace Pumasi.Core.Ai;

public sealed class GeminiPlanner
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true
    };

    private readonly GeminiClient client;
    private readonly AssistantConfig assistant;

    public GeminiPlanner(GeminiClient client, AssistantConfig assistant)
    {
        this.client = client;
        this.assistant = assistant;
    }

    public async Task<AiPlanResult> PlanAsync(FarmSummary summary, CancellationToken cancellationToken = default)
    {
        try
        {
            var responseText = await client.GenerateTextAsync(BuildPrompt(summary), cancellationToken).ConfigureAwait(false);
            return ParsePlan(responseText);
        }
        catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException or TaskCanceledException or JsonException)
        {
            return AiPlanResult.Fail(ex.Message);
        }
    }

    public string BuildPrompt(FarmSummary summary)
    {
        var payload = new
        {
            helperName = assistant.Name,
            personality = assistant.Personality,
            behaviorRules = assistant.BehaviorRules,
            automationMode = assistant.AutomationMode.ToString(),
            summary.Day,
            summary.Weather,
            summary.TimeOfDay,
            summary.PlayerInstruction,
            candidateTasks = summary.CandidateTasks.Select(task => new
            {
                type = task.Type.ToString(),
                location = task.Target.Location,
                tile = new { x = task.Target.X, y = task.Target.Y },
                task.Target.EntityId,
                task.Target.ObjectName,
                task.Priority,
                task.Reason,
                task.Source,
                key = task.Key
            }),
            currentTodos = summary.CurrentTodos
        };

        return
            "You are the planning layer for pumasi (품앗이), a Stardew Valley farm helper mod.\n" +
            "Return only JSON. Do not include markdown.\n" +
            "Choose from the provided candidateTasks only. Never invent tiles or task types.\n" +
            "The host will reject any task whose key does not match a candidateTasks key.\n" +
            "Supported task types include HarvestCrop, WaterCrop, TillSprinklerSoil, CollectMachine, RefillHay, PetAnimal, and CollectAnimalProduct when those tasks appear in candidateTasks.\n" +
            "Schema:\n" +
            "{\n" +
            "  \"message\": \"short public helper message\",\n" +
            "  \"tasks\": [\n" +
            "    {\n" +
            "      \"type\": \"HarvestCrop\",\n" +
            "      \"location\": \"Farm\",\n" +
            "      \"tile\": { \"x\": 64, \"y\": 22 },\n" +
            "      \"priority\": 90,\n" +
            "      \"reason\": \"ready crop\",\n" +
            "      \"source\": \"gemini\"\n" +
            "    }\n" +
            "  ]\n" +
            "}\n" +
            "Game summary:\n" +
            JsonSerializer.Serialize(payload, JsonOptions);
    }

    public static AiPlanResult ParsePlan(string modelText)
    {
        var json = ExtractJsonObject(modelText);
        if (json is null)
            return AiPlanResult.Fail("gemini-response-did-not-contain-json-object");

        AiPlanDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<AiPlanDto>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            return AiPlanResult.Fail($"invalid-gemini-json: {ex.Message}");
        }

        if (dto is null)
            return AiPlanResult.Fail("empty-gemini-plan");

        var tasks = new List<TaskProposal>();
        foreach (var task in dto.Tasks)
        {
            if (string.IsNullOrWhiteSpace(task.Location))
                return AiPlanResult.Fail("task-location-required");

            tasks.Add(new TaskProposal(
                task.Type,
                new TaskTarget(task.Location, task.Tile.X, task.Tile.Y, task.EntityId, task.ObjectName),
                Math.Clamp(task.Priority, 0, 100),
                string.IsNullOrWhiteSpace(task.Reason) ? "Gemini planned task" : task.Reason,
                string.IsNullOrWhiteSpace(task.Source) ? "gemini" : task.Source));
        }

        return AiPlanResult.Ok(dto.Message ?? "", tasks);
    }

    public static IReadOnlyList<TaskProposal> SelectCandidateTasks(
        IReadOnlyList<TaskProposal> plannedTasks,
        IReadOnlyList<TaskProposal> candidateTasks)
    {
        if (plannedTasks.Count == 0 || candidateTasks.Count == 0)
            return Array.Empty<TaskProposal>();

        var candidateKeys = candidateTasks
            .Select(task => task.Key)
            .ToHashSet(StringComparer.Ordinal);

        return plannedTasks
            .Where(task => candidateKeys.Contains(task.Key))
            .ToArray();
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

    private sealed class AiPlanDto
    {
        public string? Message { get; set; }
        public List<AiTaskDto> Tasks { get; set; } = new();
    }

    private sealed class AiTaskDto
    {
        public TaskType Type { get; set; }
        public string Location { get; set; } = "";
        public TileDto Tile { get; set; } = new();
        public int Priority { get; set; }
        public string Reason { get; set; } = "";
        public string Source { get; set; } = "gemini";
        public string? EntityId { get; set; }
        public string? ObjectName { get; set; }
    }

    private sealed class TileDto
    {
        public int X { get; set; }
        public int Y { get; set; }
    }
}
