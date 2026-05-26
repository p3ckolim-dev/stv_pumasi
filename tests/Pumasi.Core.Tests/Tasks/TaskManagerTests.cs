using Pumasi.Core.Clock;
using Pumasi.Core.Tasks;
using Xunit;

namespace Pumasi.Core.Tests.Tasks;

public sealed class TaskManagerTests
{
    [Fact]
    public void CreateTaskKey_UsesTypeLocationTileAndObject()
    {
        var key = TaskKeyFactory.Create(
            TaskType.CollectMachine,
            new TaskTarget("Farm", 71, 19, ObjectName: "Keg"));

        Assert.Equal("CollectMachine:Farm:71,19:Keg", key);
    }

    [Fact]
    public void Enqueue_RejectsDuplicateQueuedTask()
    {
        var manager = new TaskManager(new FixedClock());
        var proposal = new TaskProposal(TaskType.HarvestCrop, new TaskTarget("Farm", 64, 22), 90, "ready crop");

        var first = manager.Enqueue(proposal);
        var second = manager.Enqueue(proposal);

        Assert.True(first.Accepted);
        Assert.False(second.Accepted);
        Assert.Equal("duplicate-active-task", second.Reason);
        Assert.Single(manager.Tasks);
    }

    [Fact]
    public void Enqueue_AllowsSameKeyAfterCompleted()
    {
        var manager = new TaskManager(new FixedClock());
        var proposal = new TaskProposal(TaskType.HarvestCrop, new TaskTarget("Farm", 64, 22), 90, "ready crop");

        var first = manager.Enqueue(proposal);
        Assert.NotNull(first.Task);
        manager.Complete(first.Task.Id);

        var second = manager.Enqueue(proposal);

        Assert.True(second.Accepted);
        Assert.Equal(2, manager.Tasks.Count);
    }

    [Fact]
    public void ClaimNext_ClaimsOnlyOneTaskAtATimeByPriority()
    {
        var manager = new TaskManager(new FixedClock());
        manager.Enqueue(new TaskProposal(TaskType.WaterCrop, new TaskTarget("Farm", 10, 10), 10, "dry crop"));
        manager.Enqueue(new TaskProposal(TaskType.HarvestCrop, new TaskTarget("Farm", 11, 10), 80, "ready crop"));

        var first = manager.ClaimNext();
        var second = manager.ClaimNext();

        Assert.NotNull(first);
        Assert.Equal(TaskType.HarvestCrop, first.Type);
        Assert.Equal(HelperTaskStatus.Claimed, first.Status);
        Assert.Null(second);
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset GetUtcNow() => new(2026, 5, 26, 12, 0, 0, TimeSpan.Zero);
    }
}
