namespace Pumasi.Core.Tasks;

public sealed record TodoSnapshot(IReadOnlyList<TodoItemSnapshot> Items);

public sealed record TodoItemSnapshot(
    Guid Id,
    string Key,
    TaskType Type,
    HelperTaskStatus Status,
    string Location,
    int X,
    int Y,
    int Priority,
    string Reason,
    string? StatusReason)
{
    public static TodoItemSnapshot FromTask(HelperTask task)
    {
        return new TodoItemSnapshot(
            task.Id,
            task.Key,
            task.Type,
            task.Status,
            task.Target.Location,
            task.Target.X,
            task.Target.Y,
            task.Priority,
            task.Reason,
            task.StatusReason);
    }
}
