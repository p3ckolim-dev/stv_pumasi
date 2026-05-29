using Pumasi.Core.Ui;
using Pumasi.Core.Configuration;
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
            row => Assert.Equal(PumasiSettingsKey.Language, row.Key),
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
        Assert.All(rows, row => Assert.False(string.IsNullOrWhiteSpace(row.EnglishDescription)));
        Assert.All(rows, row => Assert.False(string.IsNullOrWhiteSpace(row.KoreanDescription)));
    }

    [Fact]
    public void FormatRowLabel_UsesSelectedLanguage()
    {
        var row = PumasiSettingsCatalog.CreateRows().Single(row => row.Key == PumasiSettingsKey.WorkAnimals);

        Assert.Equal("Animal work", row.FormatLabel(UiLanguage.English));
        Assert.Equal("동물 작업", row.FormatLabel(UiLanguage.Korean));
    }

    [Fact]
    public void FormatDescription_ExplainsImplementedWorkCategories()
    {
        var rows = PumasiSettingsCatalog.CreateRows();

        var crops = rows.Single(row => row.Key == PumasiSettingsKey.WorkCrops);
        var animals = rows.Single(row => row.Key == PumasiSettingsKey.WorkAnimals);
        var chests = rows.Single(row => row.Key == PumasiSettingsKey.WorkChests);

        Assert.Contains("물주기", crops.FormatDescription(UiLanguage.Korean));
        Assert.Contains("수확", crops.FormatDescription(UiLanguage.Korean));
        Assert.Contains("건초", animals.FormatDescription(UiLanguage.Korean));
        Assert.Contains("준비 중", chests.FormatDescription(UiLanguage.Korean));
        Assert.Contains("coming soon", chests.FormatDescription(UiLanguage.English));
    }
}
