namespace Pumasi.Core.Tasks;

public sealed record TaskEnqueueResult(bool Accepted, HelperTask? Task, string Reason)
{
    public static TaskEnqueueResult Added(HelperTask task) => new(true, task, "queued");
    public static TaskEnqueueResult Duplicate(HelperTask task) => new(false, task, "duplicate-active-task");
    public static TaskEnqueueResult Rejected(string reason) => new(false, null, reason);
}
