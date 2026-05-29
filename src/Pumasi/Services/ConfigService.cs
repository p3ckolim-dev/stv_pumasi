using Pumasi.Core.Configuration;
using Pumasi.Core.Ui;
using Pumasi.Integrations;
using StardewModdingAPI;

namespace Pumasi.Services;

internal sealed class ConfigService
{
    private static readonly string[] LanguageAllowedValues = { nameof(UiLanguage.Korean), nameof(UiLanguage.English) };

    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly IManifest manifest;

    public ConfigService(IModHelper helper, IMonitor monitor, IManifest manifest)
    {
        this.helper = helper;
        this.monitor = monitor;
        this.manifest = manifest;
        Config = helper.ReadConfig<ModConfig>();
    }

    public ModConfig Config { get; private set; }

    public void Save()
    {
        helper.WriteConfig(Config);
    }

    public void Reset()
    {
        Config = new ModConfig();
        Save();
    }

    public void SetGeminiApiKey(string key)
    {
        Config.Gemini.ApiKey = key.Trim();
        Save();
    }

    public void RegisterGenericModConfigMenu()
    {
        var api = helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (api is null)
        {
            monitor.Log("Generic Mod Config Menu not found; pumasi (품앗이) will use config.json, SMAPI console commands, and /pms chat commands.", LogLevel.Info);
            return;
        }

        api.Register(manifest, Reset, Save);
        api.AddSectionTitle(manifest, () => T(PumasiTextKey.GmcmUiSection));
        api.AddTextOption(
            manifest,
            () => Config.Ui.Language.ToString(),
            value => Config.Ui.Language = ParseLanguage(value),
            () => T(PumasiTextKey.SettingsLanguage),
            allowedValues: LanguageAllowedValues,
            formatAllowedValue: value => PumasiText.GetLanguageName(Config.Ui.Language, ParseLanguage(value)));

        api.AddSectionTitle(manifest, () => T(PumasiTextKey.GmcmHelperSection));
        api.AddTextOption(manifest, () => Config.Assistant.Name, value => Config.Assistant.Name = value, () => T(PumasiTextKey.GmcmName));
        api.AddTextOption(manifest, () => Config.Assistant.Personality, value => Config.Assistant.Personality = value, () => T(PumasiTextKey.GmcmPersonality));
        api.AddTextOption(manifest, () => Config.Assistant.BehaviorRules, value => Config.Assistant.BehaviorRules = value, () => T(PumasiTextKey.GmcmBehaviorRules));
        api.AddNumberOption(manifest, () => Config.Assistant.MaxTasksPerDay, value => Config.Assistant.MaxTasksPerDay = value, () => T(PumasiTextKey.GmcmMaxTasksPerDay), min: 1, max: 500);
        api.AddNumberOption(manifest, () => Config.Assistant.MorningTodoLimit, value => Config.Assistant.MorningTodoLimit = value, () => T(PumasiTextKey.GmcmMorningTodos), min: 0, max: 20);

        api.AddSectionTitle(manifest, () => T(PumasiTextKey.GmcmWorkCategoriesSection));
        api.AddBoolOption(manifest, () => Config.Assistant.WorkCategories.Crops, value => Config.Assistant.WorkCategories.Crops = value, () => T(PumasiTextKey.GmcmCrops));
        api.AddBoolOption(manifest, () => Config.Assistant.WorkCategories.Machines, value => Config.Assistant.WorkCategories.Machines = value, () => T(PumasiTextKey.GmcmMachines));
        api.AddBoolOption(manifest, () => Config.Assistant.WorkCategories.Animals, value => Config.Assistant.WorkCategories.Animals = value, () => T(PumasiTextKey.GmcmAnimals));
        api.AddBoolOption(manifest, () => Config.Assistant.WorkCategories.Chests, value => Config.Assistant.WorkCategories.Chests = value, () => T(PumasiTextKey.GmcmChests));
        api.AddBoolOption(manifest, () => Config.Assistant.WorkCategories.Planting, value => Config.Assistant.WorkCategories.Planting = value, () => T(PumasiTextKey.GmcmPlanting));

        api.AddSectionTitle(manifest, () => T(PumasiTextKey.GmcmGeminiSection));
        api.AddTextOption(manifest, () => Config.Gemini.BaseUrl, value => Config.Gemini.BaseUrl = value, () => T(PumasiTextKey.GmcmBaseUrl));
        api.AddTextOption(manifest, () => Config.Gemini.Model, value => Config.Gemini.Model = value, () => T(PumasiTextKey.GmcmModel));
        api.AddTextOption(
            manifest,
            () => string.IsNullOrWhiteSpace(Config.Gemini.ApiKey) ? "" : "********",
            value =>
            {
                if (!string.IsNullOrWhiteSpace(value) && value != "********")
                    Config.Gemini.ApiKey = value.Trim();
            },
            () => T(PumasiTextKey.GmcmGeminiApiKey),
            () => T(PumasiTextKey.GmcmGeminiApiKeyTooltip));
        api.AddNumberOption(manifest, () => Config.Gemini.MaxCallsPerDay, value => Config.Gemini.MaxCallsPerDay = value, () => T(PumasiTextKey.GmcmMaxCallsPerDay), min: 0, max: 500);

        api.AddSectionTitle(manifest, () => T(PumasiTextKey.GmcmWikiAnswersSection));
        api.AddBoolOption(manifest, () => Config.WikiAnswers.WikiAnswersEnabled, value => Config.WikiAnswers.WikiAnswersEnabled = value, () => T(PumasiTextKey.GmcmUseKoreanWikiAnswers));
        api.AddTextOption(manifest, () => Config.WikiAnswers.WikiBaseUrl, value => Config.WikiAnswers.WikiBaseUrl = value, () => T(PumasiTextKey.GmcmWikiBaseUrl));
        api.AddNumberOption(manifest, () => Config.WikiAnswers.WikiMaxPages, value => Config.WikiAnswers.WikiMaxPages = value, () => T(PumasiTextKey.GmcmMaxWikiPages), min: 1, max: 5);
        api.AddNumberOption(manifest, () => Config.WikiAnswers.WikiContextCharacterLimit, value => Config.WikiAnswers.WikiContextCharacterLimit = value, () => T(PumasiTextKey.GmcmWikiContextCharacterLimit), min: 1000, max: 20000);
        api.AddNumberOption(manifest, () => Config.WikiAnswers.WikiQuestionCooldownSeconds, value => Config.WikiAnswers.WikiQuestionCooldownSeconds = value, () => T(PumasiTextKey.GmcmWikiQuestionCooldownSeconds), min: 0, max: 120);
    }

    private string T(PumasiTextKey key) => PumasiText.Get(Config.Ui.Language, key);

    private static UiLanguage ParseLanguage(string value)
    {
        return Enum.TryParse<UiLanguage>(value, ignoreCase: true, out var language) ? language : UiLanguage.Korean;
    }
}
