using StardewAIFarmHelper.Core.Clock;

namespace StardewAIFarmHelper.Core.Tasks;

public sealed class TaskManager
{
    private readonly List<HelperTask> tasks = new();
    private readonly IClock clock;

    public TaskManager(IClock? clock = null)
    {
        this.clock = clock ?? SystemClock.Instance;
    }

    public IReadOnlyList<HelperTask> Tasks => tasks;

    public TaskEnqueueResult Enqueue(TaskProposal proposal)
    {
        if (proposal.Priority < 0 || proposal.Priority > 100)
            return TaskEnqueueResult.Rejected("priority-out-of-range");

        if (string.IsNullOrWhiteSpace(proposal.Target.Location))
            return TaskEnqueueResult.Rejected("missing-location");

        var existing = tasks.FirstOrDefault(task =>
            task.Key == proposal.Key &&
            task.Status is HelperTaskStatus.Queued or HelperTaskStatus.Claimed or HelperTaskStatus.InProgress);

        if (existing is not null)
            return TaskEnqueueResult.Duplicate(existing);

        var helperTask = new HelperTask(proposal, clock.GetUtcNow());
        tasks.Add(helperTask);
        return TaskEnqueueResult.Added(helperTask);
    }

    public HelperTask? ClaimNext()
    {
        if (tasks.Any(task => task.Status is HelperTaskStatus.Claimed or HelperTaskStatus.InProgress))
            return null;

        var next = tasks
            .Where(task => task.Status == HelperTaskStatus.Queued)
            .OrderByDescending(task => task.Priority)
            .ThenBy(task => task.CreatedAt)
            .FirstOrDefault();

        next?.Claim(clock.GetUtcNow());
        return next;
    }

    public bool Start(Guid id) => Update(id, task => task.Start(clock.GetUtcNow()));
    public bool Complete(Guid id, string? reason = null) => Update(id, task => task.Complete(clock.GetUtcNow(), reason));
    public bool Skip(Guid id, string reason) => Update(id, task => task.Skip(clock.GetUtcNow(), reason));
    public bool Fail(Guid id, string reason) => Update(id, task => task.Fail(clock.GetUtcNow(), reason));
    public bool Cancel(Guid id, string reason) => Update(id, task => task.Cancel(clock.GetUtcNow(), reason));

    public TodoSnapshot CreateSnapshot()
    {
        return new TodoSnapshot(tasks.Select(TodoItemSnapshot.FromTask).ToArray());
    }

    private bool Update(Guid id, Action<HelperTask> update)
    {
        var task = tasks.FirstOrDefault(candidate => candidate.Id == id);
        if (task is null)
            return false;

        update(task);
        return true;
    }
}
