using Pumasi.Core.Tasks;
using Pumasi.Core.Ui;
using Xunit;

namespace Pumasi.Core.Tests.Ui;

public sealed class TodoDisplayFilterTests
{
    [Fact]
    public void SelectVisibleItems_ShowsActiveFirstThenNewestFinished()
    {
        var oldCompleted = Item("old", HelperTaskStatus.Completed);
        var queued = Item("queued", HelperTaskStatus.Queued);
        var failed = Item("failed", HelperTaskStatus.Failed);
        var skipped = Item("skipped", HelperTaskStatus.Skipped);

        var selected = TodoDisplayFilter.SelectVisibleItems(
            new[] { oldCompleted, queued, failed, skipped },
            capacity: 3);

        Assert.Collection(
            selected,
            item => Assert.Equal("queued", item.Key),
            item => Assert.Equal("skipped", item.Key),
            item => Assert.Equal("failed", item.Key));
    }

    [Fact]
    public void SelectVisibleItems_TruncatesActiveItemsBeforeFinishedItems()
    {
        var selected = TodoDisplayFilter.SelectVisibleItems(
            new[]
            {
                Item("active-1", HelperTaskStatus.Queued),
                Item("active-2", HelperTaskStatus.Claimed),
                Item("active-3", HelperTaskStatus.InProgress),
                Item("done", HelperTaskStatus.Completed)
            },
            capacity: 2);

        Assert.Collection(
            selected,
            item => Assert.Equal("active-1", item.Key),
            item => Assert.Equal("active-2", item.Key));
    }

    [Fact]
    public void CountActive_IgnoresFinishedItems()
    {
        var activeCount = TodoDisplayFilter.CountActive(new[]
        {
            Item("queued", HelperTaskStatus.Queued),
            Item("done", HelperTaskStatus.Completed),
            Item("failed", HelperTaskStatus.Failed)
        });

        Assert.Equal(1, activeCount);
    }

    private static TodoItemSnapshot Item(string key, HelperTaskStatus status)
    {
        return new TodoItemSnapshot(
            Guid.NewGuid(),
            key,
            TaskType.WaterCrop,
            status,
            "Farm",
            10,
            10,
            50,
            "Dry crop",
            "scan",
            status is HelperTaskStatus.Completed or HelperTaskStatus.Skipped or HelperTaskStatus.Failed
                ? "crop-already-watered"
                : null);
    }
}
