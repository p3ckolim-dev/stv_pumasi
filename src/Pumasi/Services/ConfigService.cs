using Pumasi.Core.Configuration;
using Pumasi.Integrations;
using StardewModdingAPI;

namespace Pumasi.Services;

internal sealed class ConfigService
{
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
        api.AddSectionTitle(manifest, () => "Helper");
        api.AddTextOption(manifest, () => Config.Assistant.Name, value => Config.Assistant.Name = value, () => "Name");
        api.AddTextOption(manifest, () => Config.Assistant.Personality, value => Config.Assistant.Personality = value, () => "Personality");
        api.AddTextOption(manifest, () => Config.Assistant.BehaviorRules, value => Config.Assistant.BehaviorRules = value, () => "Behavior rules");
        api.AddNumberOption(manifest, () => Config.Assistant.MaxTasksPerDay, value => Config.Assistant.MaxTasksPerDay = value, () => "Max tasks per day", min: 1, max: 500);

        api.AddSectionTitle(manifest, () => "Work Categories");
        api.AddBoolOption(manifest, () => Config.Assistant.WorkCategories.Crops, value => Config.Assistant.WorkCategories.Crops = value, () => "Crops");
        api.AddBoolOption(manifest, () => Config.Assistant.WorkCategories.Machines, value => Config.Assistant.WorkCategories.Machines = value, () => "Machines");
        api.AddBoolOption(manifest, () => Config.Assistant.WorkCategories.Animals, value => Config.Assistant.WorkCategories.Animals = value, () => "Animals");
        api.AddBoolOption(manifest, () => Config.Assistant.WorkCategories.Chests, value => Config.Assistant.WorkCategories.Chests = value, () => "Chests");
        api.AddBoolOption(manifest, () => Config.Assistant.WorkCategories.Planting, value => Config.Assistant.WorkCategories.Planting = value, () => "Planting");

        api.AddSectionTitle(manifest, () => "Gemini");
        api.AddTextOption(manifest, () => Config.Gemini.BaseUrl, value => Config.Gemini.BaseUrl = value, () => "Base URL");
        api.AddTextOption(manifest, () => Config.Gemini.Model, value => Config.Gemini.Model = value, () => "Model");
        api.AddTextOption(
            manifest,
            () => string.IsNullOrWhiteSpace(Config.Gemini.ApiKey) ? "" : "********",
            value =>
            {
                if (!string.IsNullOrWhiteSpace(value) && value != "********")
                    Config.Gemini.ApiKey = value.Trim();
            },
            () => "Gemini API key",
            () => "Stored only in this host's local config. Enter a new key to replace the existing one.");
        api.AddNumberOption(manifest, () => Config.Gemini.MaxCallsPerDay, value => Config.Gemini.MaxCallsPerDay = value, () => "Max calls per day", min: 0, max: 500);

        api.AddSectionTitle(manifest, () => "Wiki Answers");
        api.AddBoolOption(manifest, () => Config.WikiAnswers.WikiAnswersEnabled, value => Config.WikiAnswers.WikiAnswersEnabled = value, () => "Use Korean Wiki answers");
        api.AddTextOption(manifest, () => Config.WikiAnswers.WikiBaseUrl, value => Config.WikiAnswers.WikiBaseUrl = value, () => "Wiki base URL");
        api.AddNumberOption(manifest, () => Config.WikiAnswers.WikiMaxPages, value => Config.WikiAnswers.WikiMaxPages = value, () => "Max wiki pages", min: 1, max: 5);
        api.AddNumberOption(manifest, () => Config.WikiAnswers.WikiContextCharacterLimit, value => Config.WikiAnswers.WikiContextCharacterLimit = value, () => "Wiki context character limit", min: 1000, max: 20000);
        api.AddNumberOption(manifest, () => Config.WikiAnswers.WikiQuestionCooldownSeconds, value => Config.WikiAnswers.WikiQuestionCooldownSeconds = value, () => "Wiki question cooldown seconds", min: 0, max: 120);
    }
}
