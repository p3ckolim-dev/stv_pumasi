namespace Pumasi.Core.Tasks;

public sealed class HelperTask
{
    public HelperTask(TaskProposal proposal, DateTimeOffset createdAt)
    {
        Id = Guid.NewGuid();
        Type = proposal.Type;
        Target = proposal.Target;
        Priority = Math.Clamp(proposal.Priority, 0, 100);
        Reason = proposal.Reason;
        Source = proposal.Source;
        Key = proposal.Key;
        Status = HelperTaskStatus.Queued;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; }
    public TaskType Type { get; }
    public TaskTarget Target { get; }
    public int Priority { get; }
    public string Reason { get; }
    public string Source { get; }
    public string Key { get; }
    public HelperTaskStatus Status { get; private set; }
    public string? StatusReason { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void Claim(DateTimeOffset now) => Transition(HelperTaskStatus.Claimed, now);
    public void Start(DateTimeOffset now) => Transition(HelperTaskStatus.InProgress, now);
    public void Complete(DateTimeOffset now, string? reason = null) => Transition(HelperTaskStatus.Completed, now, reason);
    public void Skip(DateTimeOffset now, string reason) => Transition(HelperTaskStatus.Skipped, now, reason);
    public void Fail(DateTimeOffset now, string reason) => Transition(HelperTaskStatus.Failed, now, reason);
    public void Cancel(DateTimeOffset now, string reason) => Transition(HelperTaskStatus.Cancelled, now, reason);

    private void Transition(HelperTaskStatus status, DateTimeOffset now, string? reason = null)
    {
        Status = status;
        StatusReason = reason;
        UpdatedAt = now;
    }
}
