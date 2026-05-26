using Microsoft.Xna.Framework;
using StardewAIFarmHelper.Core.Configuration;
using StardewAIFarmHelper.Core.Tasks;
using StardewValley;
using StardewValley.TerrainFeatures;
using SObject = StardewValley.Object;

namespace StardewAIFarmHelper.Game;

internal sealed class FarmTaskScanner
{
    public IReadOnlyList<TaskProposal> Scan(ModConfig config)
    {
        var proposals = new List<TaskProposal>();
        foreach (var location in GetSupportedLocations())
        {
            if (config.Assistant.WorkCategories.Crops)
                ScanCrops(location, proposals);

            if (config.Assistant.WorkCategories.Machines)
                ScanMachines(location, proposals);
        }

        return proposals
            .OrderByDescending(task => task.Priority)
            .Take(config.Assistant.MaxTasksPerDay)
            .ToArray();
    }

    private static IEnumerable<GameLocation> GetSupportedLocations()
    {
        var farm = Game1.getFarm();
        if (farm is not null)
            yield return farm;

        var greenhouse = Game1.getLocationFromName("Greenhouse");
        if (greenhouse is not null)
            yield return greenhouse;
    }

    private static void ScanCrops(GameLocation location, List<TaskProposal> proposals)
    {
        foreach (var pair in location.terrainFeatures.Pairs)
        {
            if (pair.Value is not HoeDirt dirt || dirt.crop is null)
                continue;

            var tile = pair.Key;
            if (IsReadyToHarvest(dirt))
            {
                proposals.Add(new TaskProposal(
                    TaskType.HarvestCrop,
                    new TaskTarget(location.NameOrUniqueName, (int)tile.X, (int)tile.Y),
                    90,
                    "Ready crop",
                    "scan"));
            }
            else if (dirt.state.Value != HoeDirt.watered)
            {
                proposals.Add(new TaskProposal(
                    TaskType.WaterCrop,
                    new TaskTarget(location.NameOrUniqueName, (int)tile.X, (int)tile.Y),
                    50,
                    "Dry crop",
                    "scan"));
            }
        }
    }

    private static void ScanMachines(GameLocation location, List<TaskProposal> proposals)
    {
        foreach (var pair in location.objects.Pairs)
        {
            if (pair.Value is not SObject obj || !obj.readyForHarvest.Value)
                continue;

            var tile = pair.Key;
            proposals.Add(new TaskProposal(
                TaskType.CollectMachine,
                new TaskTarget(location.NameOrUniqueName, (int)tile.X, (int)tile.Y, ObjectName: obj.Name),
                75,
                $"Ready {obj.Name}",
                "scan"));
        }
    }

    private static bool IsReadyToHarvest(HoeDirt dirt)
    {
        var crop = dirt.crop;
        if (crop is null)
            return false;

        try
        {
            return !crop.dead.Value
                && crop.currentPhase.Value >= crop.phaseDays.Count - 1
                && crop.dayOfCurrentPhase.Value <= 0;
        }
        catch
        {
            return false;
        }
    }
}
