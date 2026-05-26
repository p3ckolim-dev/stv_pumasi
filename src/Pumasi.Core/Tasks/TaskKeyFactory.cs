using System.Globalization;

namespace Pumasi.Core.Tasks;

public static class TaskKeyFactory
{
    public static string Create(TaskType type, TaskTarget target)
    {
        var location = TaskTarget.Normalize(target.Location);
        var key = string.Create(
            CultureInfo.InvariantCulture,
            $"{type}:{location}:{target.X},{target.Y}");

        if (!string.IsNullOrWhiteSpace(target.EntityId))
            key += $":{target.EntityId.Trim()}";

        if (!string.IsNullOrWhiteSpace(target.ObjectName))
            key += $":{TaskTarget.Normalize(target.ObjectName)}";

        return key;
    }
}
