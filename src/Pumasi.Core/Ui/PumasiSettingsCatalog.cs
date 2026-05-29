using Pumasi.Core.Configuration;

namespace Pumasi.Core.Ui;

public enum PumasiSettingsKey
{
    Language,
    ShowTodoOverlay,
    ShowHelperStatusNotifications,
    WorkCrops,
    WorkMachines,
    WorkAnimals,
    WorkChests,
    WorkPlanting,
    WikiAnswersEnabled
}

public sealed record PumasiSettingsRow(
    PumasiSettingsKey Key,
    string EnglishLabel,
    string KoreanLabel)
{
    public string FormatLabel(UiLanguage language)
    {
        return language == UiLanguage.English ? EnglishLabel : KoreanLabel;
    }
}

public static class PumasiSettingsCatalog
{
    public static IReadOnlyList<PumasiSettingsRow> CreateRows()
    {
        return new[]
        {
            new PumasiSettingsRow(PumasiSettingsKey.Language, "Language", "언어"),
            new PumasiSettingsRow(PumasiSettingsKey.ShowTodoOverlay, "Show todo HUD icon", "투두 HUD 아이콘 표시"),
            new PumasiSettingsRow(PumasiSettingsKey.ShowHelperStatusNotifications, "Show helper notifications", "도우미 알림 표시"),
            new PumasiSettingsRow(PumasiSettingsKey.WorkCrops, "Crop work", "작물 작업"),
            new PumasiSettingsRow(PumasiSettingsKey.WorkMachines, "Machine work", "기계 작업"),
            new PumasiSettingsRow(PumasiSettingsKey.WorkAnimals, "Animal work", "동물 작업"),
            new PumasiSettingsRow(PumasiSettingsKey.WorkChests, "Chest work", "상자 작업"),
            new PumasiSettingsRow(PumasiSettingsKey.WorkPlanting, "Planting work", "씨앗 심기 작업"),
            new PumasiSettingsRow(PumasiSettingsKey.WikiAnswersEnabled, "Korean Wiki answers", "한국어 위키 답변")
        };
    }
}
