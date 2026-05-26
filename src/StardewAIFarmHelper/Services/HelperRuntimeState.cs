namespace StardewAIFarmHelper.Services;

public sealed class HelperRuntimeState
{
    public string Name { get; set; } = "Lumi";
    public string Location { get; set; } = "Farm";
    public int X { get; set; } = 64;
    public int Y { get; set; } = 15;
    public string Status { get; set; } = "Idle";
    public string? CurrentTaskKey { get; set; }
}
