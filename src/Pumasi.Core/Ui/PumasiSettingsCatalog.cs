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
    string KoreanLabel,
    string EnglishDescription,
    string KoreanDescription)
{
    public string FormatLabel(UiLanguage language)
    {
        return language == UiLanguage.English ? EnglishLabel : KoreanLabel;
    }

    public string FormatDescription(UiLanguage language)
    {
        return language == UiLanguage.English ? EnglishDescription : KoreanDescription;
    }
}

public static class PumasiSettingsCatalog
{
    public static IReadOnlyList<PumasiSettingsRow> CreateRows()
    {
        return new[]
        {
            new PumasiSettingsRow(
                PumasiSettingsKey.Language,
                "Language",
                "언어",
                "Switch Pumasi UI language.",
                "Pumasi UI 언어를 바꿔요."),
            new PumasiSettingsRow(
                PumasiSettingsKey.ShowTodoOverlay,
                "Show todo HUD icon",
                "투두 HUD 아이콘 표시",
                "Show the Pumasi todo icon on the HUD.",
                "화면의 Pumasi 투두 아이콘을 표시해요."),
            new PumasiSettingsRow(
                PumasiSettingsKey.ShowHelperStatusNotifications,
                "Show helper notifications",
                "도우미 알림 표시",
                "Show helper answers and task results on the HUD.",
                "답변과 작업 결과 알림을 HUD에 보여줘요."),
            new PumasiSettingsRow(
                PumasiSettingsKey.WorkCrops,
                "Crop work",
                "작물 작업",
                "Water dry crops, harvest ready crops, and till soil near sprinklers.",
                "마른 작물 물주기, 다 자란 작물 수확, 스프링클러 주변 땅 파기."),
            new PumasiSettingsRow(
                PumasiSettingsKey.WorkMachines,
                "Machine work",
                "기계 작업",
                "Collect ready machine output.",
                "완료된 기계 생산품을 수거해요."),
            new PumasiSettingsRow(
                PumasiSettingsKey.WorkAnimals,
                "Animal work",
                "동물 작업",
                "Refill hay, pet animals, and collect loose animal products.",
                "건초 리필, 동물 쓰다듬기, 바닥 생산품 수거."),
            new PumasiSettingsRow(
                PumasiSettingsKey.WorkChests,
                "Chest work",
                "상자 작업",
                "Chest sorting is coming soon.",
                "상자 정리는 준비 중이에요."),
            new PumasiSettingsRow(
                PumasiSettingsKey.WorkPlanting,
                "Planting work",
                "씨앗 심기 작업",
                "Planting is coming soon.",
                "씨앗 심기는 준비 중이에요."),
            new PumasiSettingsRow(
                PumasiSettingsKey.WikiAnswersEnabled,
                "Korean Wiki answers",
                "한국어 위키 답변",
                "Use Korean Wiki grounding for Stardew Valley questions.",
                "스타듀밸리 질문에 한국어 위키 기반 답변을 사용해요.")
        };
    }
}
