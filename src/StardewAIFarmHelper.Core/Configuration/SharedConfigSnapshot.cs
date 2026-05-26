namespace StardewAIFarmHelper.Core.Configuration;

public sealed record SharedConfigSnapshot(
    string HelperName,
    string Personality,
    string BehaviorRules,
    AutomationMode AutomationMode,
    bool CropsEnabled,
    bool MachinesEnabled,
    bool AnimalsEnabled,
    bool ChestsEnabled,
    bool PlantingEnabled);
