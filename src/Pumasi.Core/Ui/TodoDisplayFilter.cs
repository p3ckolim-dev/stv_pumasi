using Pumasi.Core.Tasks;

namespace Pumasi.Core.Ui;

public static class TodoDisplayFilter
{
    private static readonly HelperTaskStatus[] ActiveStatuses =
    {
        HelperTaskStatus.Queued,
        HelperTaskStatus.Claimed,
        HelperTaskStatus.InProgress
    };

    private static readonly HelperTaskStatus[] FinishedStatuses =
    {
        HelperTaskStatus.Completed,
        HelperTaskStatus.Skipped,
        HelperTaskStatus.Failed
    };

    public static int CountActive(IReadOnlyList<TodoItemSnapshot> items)
    {
        return items.Count(IsActive);
    }

    public static bool IsActive(TodoItemSnapshot item)
    {
        return ActiveStatuses.Contains(item.Status);
    }

    public static IReadOnlyList<TodoItemSnapshot> SelectVisibleItems(IReadOnlyList<TodoItemSnapshot> items, int capacity)
    {
        if (capacity <= 0)
            return Array.Empty<TodoItemSnapshot>();

        var active = items
            .Where(IsActive)
            .ToArray();

        var finished = items
            .Where(item => FinishedStatuses.Contains(item.Status))
            .Reverse()
            .ToArray();

        return active.Concat(finished).Take(capacity).ToArray();
    }
}
