using StardewAIFarmHelper.Core.Tasks;

namespace StardewAIFarmHelper.Core.Ai;

public sealed record FarmSummary(
    string Day,
    string Weather,
    int TimeOfDay,
    IReadOnlyList<TaskProposal> CandidateTasks,
    IReadOnlyList<TodoItemForPrompt> CurrentTodos,
    string PlayerInstruction);

public sealed record TodoItemForPrompt(
    string Key,
    TaskType Type,
    string Location,
    int X,
    int Y,
    string Status);
