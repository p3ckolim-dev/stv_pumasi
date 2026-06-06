using System.Globalization;
using Microsoft.Xna.Framework;
using Pumasi.Core.Configuration;
using Pumasi.Core.Tasks;
using StardewValley;
using StardewValley.TerrainFeatures;
using SObject = StardewValley.Object;

namespace Pumasi.Game;

internal sealed class FarmTaskScanner
{
    private static readonly HashSet<string> LooseAnimalProductItemIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "107", // Dinosaur Egg
        "174", // Large Egg
        "176", // Egg
        "180", // Brown Egg
        "182", // Large Brown Egg
        "184", // Milk
        "186", // Large Milk
        "289", // Ostrich Egg
        "305", // Void Egg
        "436", // Goat Milk
        "438", // Large Goat Milk
        "440", // Wool
        "442", // Duck Egg
        "444", // Duck Feather
        "446", // Rabbit's Foot
        "928" // Golden Egg
    };

    public IReadOnlyList<TaskProposal> Scan(ModConfig config)
    {
        var proposals = new List<TaskProposal>();
        foreach (var location in GetSupportedLocations())
        {
            if (config.Assistant.WorkCategories.Crops)
            {
                ScanCrops(location, proposals);
                ScanSprinklerTilling(location, proposals);
            }

            if (config.Assistant.WorkCategories.Machines)
                ScanMachines(location, proposals);
        }

        if (config.Assistant.WorkCategories.Animals)
            ScanAnimalWork(proposals);

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

    private static void ScanSprinklerTilling(GameLocation location, List<TaskProposal> proposals)
    {
        foreach (var pair in location.objects.Pairs)
        {
            if (pair.Value is not SObject obj || !IsSprinkler(obj))
                continue;

            foreach (var offset in GetSprinklerOffsets(obj))
            {
                var tile = pair.Key + offset;
                if (!IsPlainTillableTile(location, tile))
                    continue;

                proposals.Add(new TaskProposal(
                    TaskType.TillSprinklerSoil,
                    new TaskTarget(location.NameOrUniqueName, (int)tile.X, (int)tile.Y, ObjectName: obj.Name),
                    45,
                    $"Untilled soil near {obj.Name}",
                    "scan"));
            }
        }
    }

    private static void ScanAnimalWork(List<TaskProposal> proposals)
    {
        foreach (var location in Game1.locations.Where(IsAnimalHouse))
        {
            ScanAnimalPetting(location, proposals);
            ScanLooseAnimalProducts(location, proposals);
        }

        ScanHayRefills(proposals);
    }

    private static void ScanAnimalPetting(GameLocation location, List<TaskProposal> proposals)
    {
        if (location is not AnimalHouse animalHouse)
            return;

        foreach (var animal in GetAnimalsForHouse(animalHouse))
        {
            if (animal.wasPet.Value)
                continue;

            var tile = animal.TilePoint;
            proposals.Add(new TaskProposal(
                TaskType.PetAnimal,
                new TaskTarget(
                    location.NameOrUniqueName,
                    tile.X,
                    tile.Y,
                    EntityId: animal.myID.Value.ToString(CultureInfo.InvariantCulture),
                    ObjectName: animal.displayName),
                72,
                "Unpetted animal",
                "scan"));
        }
    }

    private static void ScanLooseAnimalProducts(GameLocation location, List<TaskProposal> proposals)
    {
        foreach (var pair in location.objects.Pairs)
        {
            if (pair.Value is not SObject obj || !IsSafeLooseAnimalProduct(obj))
                continue;

            var tile = pair.Key;
            proposals.Add(new TaskProposal(
                TaskType.CollectAnimalProduct,
                new TaskTarget(location.NameOrUniqueName, (int)tile.X, (int)tile.Y, ObjectName: obj.DisplayName),
                82,
                $"Loose animal product {obj.DisplayName}",
                "scan"));
        }
    }

    private static void ScanHayRefills(List<TaskProposal> proposals)
    {
        foreach (var location in Game1.locations.Where(IsAnimalHouse))
        {
            proposals.Add(new TaskProposal(
                TaskType.RefillHay,
                new TaskTarget(location.NameOrUniqueName, 0, 0, ObjectName: "Hay"),
                65,
                "Refill animal hay",
                "scan"));
        }
    }

    private static bool IsSprinkler(SObject obj)
    {
        return obj.Name.Contains("Sprinkler", StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<Vector2> GetSprinklerOffsets(SObject sprinkler)
    {
        var radius = sprinkler.Name.Contains("Iridium", StringComparison.OrdinalIgnoreCase) ? 2 : 1;
        for (var x = -radius; x <= radius; x++)
        {
            for (var y = -radius; y <= radius; y++)
            {
                if (x == 0 && y == 0)
                    continue;

                if (radius == 1 && sprinkler.Name.Equals("Sprinkler", StringComparison.OrdinalIgnoreCase) && Math.Abs(x) + Math.Abs(y) != 1)
                    continue;

                yield return new Vector2(x, y);
            }
        }
    }

    private static bool IsPlainTillableTile(GameLocation location, Vector2 tile)
    {
        if (location.objects.ContainsKey(tile) || location.terrainFeatures.ContainsKey(tile))
            return false;

        var diggable = location.doesTileHaveProperty((int)tile.X, (int)tile.Y, "Diggable", "Back");
        return !string.IsNullOrWhiteSpace(diggable);
    }

    private static bool IsAnimalHouse(GameLocation location)
    {
        return AnimalTaskSafety.IsAnimalHouseTypeName(location.GetType().Name);
    }

    private static bool IsSafeLooseAnimalProduct(SObject obj)
    {
        return obj.CanBeGrabbed
            && !obj.bigCraftable.Value
            && LooseAnimalProductItemIds.Contains(obj.ItemId);
    }

    private static IEnumerable<FarmAnimal> GetAnimalsForHouse(AnimalHouse animalHouse)
    {
        var farm = Game1.getFarm();
        foreach (var animalId in animalHouse.animalsThatLiveHere)
        {
            if (farm.animals.TryGetValue(animalId, out var animal))
                yield return animal;
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
