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
    public void CreateTaskKey_IncludesAnimalEntityId()
    {
        var key = TaskKeyFactory.Create(
            TaskType.PetAnimal,
            new TaskTarget("Barn", 12, 8, EntityId: "123456789"));

        Assert.Equal("PetAnimal:Barn:12,8:123456789", key);
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
    public void ClaimNext_ClaimsOnlyOneTaskAtATimeByTodoOrder()
    {
        var manager = new TaskManager(new FixedClock());
        manager.Enqueue(new TaskProposal(TaskType.WaterCrop, new TaskTarget("Farm", 10, 10), 10, "dry crop"));
        manager.Enqueue(new TaskProposal(TaskType.HarvestCrop, new TaskTarget("Farm", 11, 10), 80, "ready crop"));

        var first = manager.ClaimNext();
        var second = manager.ClaimNext();

        Assert.NotNull(first);
        Assert.Equal(TaskType.WaterCrop, first.Type);
        Assert.Equal(HelperTaskStatus.Claimed, first.Status);
        Assert.Null(second);
    }

    [Fact]
    public void MoveActiveTask_ReordersQueuedTasksByVisibleTodoPosition()
    {
        var manager = new TaskManager(new FixedClock());
        manager.Enqueue(new TaskProposal(TaskType.WaterCrop, new TaskTarget("Farm", 10, 10), 10, "dry crop"));
        manager.Enqueue(new TaskProposal(TaskType.HarvestCrop, new TaskTarget("Farm", 11, 10), 80, "ready crop"));
        manager.Enqueue(new TaskProposal(TaskType.CollectMachine, new TaskTarget("Farm", 12, 10, ObjectName: "Keg"), 60, "ready keg"));

        var result = manager.MoveActiveTask(3, 1);
        var claimed = manager.ClaimNext();

        Assert.True(result.Moved);
        Assert.NotNull(claimed);
        Assert.Equal(TaskType.CollectMachine, claimed.Type);
    }

    [Fact]
    public void MoveActiveTask_RejectsInProgressTask()
    {
        var manager = new TaskManager(new FixedClock());
        manager.Enqueue(new TaskProposal(TaskType.WaterCrop, new TaskTarget("Farm", 10, 10), 10, "dry crop"));
        manager.Enqueue(new TaskProposal(TaskType.HarvestCrop, new TaskTarget("Farm", 11, 10), 80, "ready crop"));
        var first = manager.ClaimNext();
        Assert.NotNull(first);
        manager.Start(first.Id);

        var result = manager.MoveActiveTask(1, 2);

        Assert.False(result.Moved);
        Assert.Equal("task-not-queued", result.Reason);
    }

    [Fact]
    public void CreateSnapshot_IncludesTaskSource()
    {
        var manager = new TaskManager(new FixedClock());
        manager.Enqueue(new TaskProposal(
            TaskType.WaterCrop,
            new TaskTarget("Farm", 10, 10),
            50,
            "dry crop",
            "scan"));

        var snapshot = manager.CreateSnapshot();

        var item = Assert.Single(snapshot.Items);
        Assert.Equal("scan", item.Source);
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset GetUtcNow() => new(2026, 5, 26, 12, 0, 0, TimeSpan.Zero);
    }
}
