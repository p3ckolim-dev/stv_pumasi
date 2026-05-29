using Pumasi.Core.Configuration;
using Pumasi.Core.Tasks;
using Pumasi.Core.Ui;
using Xunit;

namespace Pumasi.Core.Tests.Ui;

public sealed class PumasiTextTests
{
    [Fact]
    public void Get_ReturnsConfiguredLanguageText()
    {
        Assert.Equal("품앗이 설정", PumasiText.Get(UiLanguage.Korean, PumasiTextKey.SettingsTitle));
        Assert.Equal("Pumasi Settings", PumasiText.Get(UiLanguage.English, PumasiTextKey.SettingsTitle));
    }

    [Theory]
    [InlineData(UiLanguage.Korean, UiLanguage.Korean, "한국어")]
    [InlineData(UiLanguage.Korean, UiLanguage.English, "영어")]
    [InlineData(UiLanguage.English, UiLanguage.Korean, "Korean")]
    [InlineData(UiLanguage.English, UiLanguage.English, "English")]
    public void GetLanguageName_UsesDisplayLanguage(UiLanguage displayLanguage, UiLanguage value, string expected)
    {
        Assert.Equal(expected, PumasiText.GetLanguageName(displayLanguage, value));
    }

    [Fact]
    public void GetTaskType_ReturnsLocalizedTaskName()
    {
        Assert.Equal("건초 리필", PumasiText.GetTaskType(UiLanguage.Korean, TaskType.RefillHay));
        Assert.Equal("Refill hay", PumasiText.GetTaskType(UiLanguage.English, TaskType.RefillHay));
    }

    [Fact]
    public void GetTaskStatus_ReturnsLocalizedStatusName()
    {
        Assert.Equal("대기", PumasiText.GetTaskStatus(UiLanguage.Korean, HelperTaskStatus.Queued));
        Assert.Equal("Queued", PumasiText.GetTaskStatus(UiLanguage.English, HelperTaskStatus.Queued));
    }
}
