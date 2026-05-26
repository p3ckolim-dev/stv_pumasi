namespace Pumasi.Core.Configuration;

public sealed class ModConfig
{
    public AssistantConfig Assistant { get; set; } = new();
    public GeminiConfig Gemini { get; set; } = new();
    public UiConfig Ui { get; set; } = new();
    public WikiAnswerConfig WikiAnswers { get; set; } = new();

    public SharedConfigSnapshot ToSharedSnapshot()
    {
        return new SharedConfigSnapshot(
            Assistant.Name,
            Assistant.Personality,
            Assistant.BehaviorRules,
            Assistant.AutomationMode,
            Assistant.WorkCategories.Crops,
            Assistant.WorkCategories.Machines,
            Assistant.WorkCategories.Animals,
            Assistant.WorkCategories.Chests,
            Assistant.WorkCategories.Planting);
    }

    public override string ToString()
    {
        return $"ModConfig(AssistantName={Assistant.Name}, AutomationMode={Assistant.AutomationMode}, Gemini={Gemini})";
    }
}
