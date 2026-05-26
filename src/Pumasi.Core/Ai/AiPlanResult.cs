using Pumasi.Core.Tasks;

namespace Pumasi.Core.Ai;

public sealed record AiPlanResult(bool Success, string Message, IReadOnlyList<TaskProposal> Tasks, string? Error)
{
    public static AiPlanResult Ok(string message, IReadOnlyList<TaskProposal> tasks) => new(true, message, tasks, null);
    public static AiPlanResult Fail(string error) => new(false, "", Array.Empty<TaskProposal>(), error);
}
