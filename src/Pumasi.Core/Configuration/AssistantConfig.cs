namespace Pumasi.Core.Configuration;

public sealed class AssistantConfig
{
    public string Name { get; set; } = "Lumi";
    public string Personality { get; set; } = "Calm, practical, and concise.";
    public string BehaviorRules { get; set; } = "Do safe repetitive farm work first. Ask before planting, selling, destroying, or moving rare items.";
    public AutomationMode AutomationMode { get; set; } = AutomationMode.Confirm;
    public WorkCategoryConfig WorkCategories { get; set; } = new();
    public int MaxTasksPerDay { get; set; } = 60;
}
