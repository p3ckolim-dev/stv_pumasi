using Pumasi.Core.Configuration;
using Pumasi.Core.Tasks;

namespace Pumasi.Core.Ui;

public static class TodoDisplayFormatter
{
    public static string FormatRow(UiLanguage language, int position, TodoItemSnapshot item)
    {
        var status = PumasiText.GetTaskStatus(language, item.Status);
        var type = PumasiText.GetTaskType(language, item.Type);
        var reason = string.IsNullOrWhiteSpace(item.StatusReason)
            ? item.Reason
            : PumasiText.GetExecutionReason(language, item.StatusReason);
        var source = string.IsNullOrWhiteSpace(item.Source)
            ? "source=unknown"
            : $"source={item.Source}";

        return $"#{position} [{status}] {type} {item.Location}({item.X},{item.Y}) P{item.Priority} {source} - {reason}";
    }
}
