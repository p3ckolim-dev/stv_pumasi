using Pumasi.Core.Ui;
using Xunit;

namespace Pumasi.Core.Tests.Ui;

public sealed class PumasiSettingsCatalogTests
{
    [Fact]
    public void CreateRows_ReturnsMenuSettingsInExpectedOrder()
    {
        var rows = PumasiSettingsCatalog.CreateRows();

        Assert.Collection(
            rows,
            row => Assert.Equal(PumasiSettingsKey.ShowTodoOverlay, row.Key),
            row => Assert.Equal(PumasiSettingsKey.ShowHelperStatusNotifications, row.Key),
            row => Assert.Equal(PumasiSettingsKey.WorkCrops, row.Key),
            row => Assert.Equal(PumasiSettingsKey.WorkMachines, row.Key),
            row => Assert.Equal(PumasiSettingsKey.WorkAnimals, row.Key),
            row => Assert.Equal(PumasiSettingsKey.WorkChests, row.Key),
            row => Assert.Equal(PumasiSettingsKey.WorkPlanting, row.Key),
            row => Assert.Equal(PumasiSettingsKey.WikiAnswersEnabled, row.Key));
    }

    [Fact]
    public void CreateRows_HasEnglishAndKoreanLabelsForEverySetting()
    {
        var rows = PumasiSettingsCatalog.CreateRows();

        Assert.All(rows, row => Assert.False(string.IsNullOrWhiteSpace(row.EnglishLabel)));
        Assert.All(rows, row => Assert.False(string.IsNullOrWhiteSpace(row.KoreanLabel)));
    }
}
