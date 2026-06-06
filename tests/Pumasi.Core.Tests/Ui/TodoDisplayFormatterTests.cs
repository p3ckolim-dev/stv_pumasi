using Pumasi.Core.Configuration;
using Pumasi.Core.Tasks;
using Pumasi.Core.Ui;
using Xunit;

namespace Pumasi.Core.Tests.Ui;

public sealed class TodoDisplayFormatterTests
{
    [Fact]
    public void FormatRow_IncludesActiveTaskDetails()
    {
        var item = new TodoItemSnapshot(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            "HarvestCrop:Greenhouse:10,8",
            TaskType.HarvestCrop,
            HelperTaskStatus.Queued,
            "Greenhouse",
            10,
            8,
            90,
            "Ready crop",
            "scan",
            null);

        var row = TodoDisplayFormatter.FormatRow(UiLanguage.English, 1, item);

        Assert.Equal("#1 [Queued] Harvest crop Greenhouse(10,8) P90 source=scan - Ready crop", row);
    }

    [Fact]
    public void FormatRow_LocalizesFinalStatusReason()
    {
        var item = new TodoItemSnapshot(
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            "WaterCrop:Farm:4,5",
            TaskType.WaterCrop,
            HelperTaskStatus.Skipped,
            "Farm",
            4,
            5,
            50,
            "Dry crop",
            "scan",
            "crop-already-watered");

        var row = TodoDisplayFormatter.FormatRow(UiLanguage.Korean, 2, item);

        Assert.Equal("#2 [건너뜀] 작물 물주기 Farm(4,5) P50 source=scan - 작물에 이미 물이 있어요", row);
    }
}
