using System.Reflection;
using Microsoft.Xna.Framework;
using Pumasi.Core.Tasks;
using Pumasi.Services;
using StardewValley;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using SObject = StardewValley.Object;

namespace Pumasi.Tasks;

internal sealed class FarmTaskExecutor
{
    private static readonly HashSet<string> LooseAnimalProductItemIds = new(StringComparer.OrdinalIgnoreCase)
    {
        "107",
        "174",
        "176",
        "180",
        "182",
        "184",
        "186",
        "289",
        "305",
        "436",
        "438",
        "440",
        "442",
        "444",
        "446",
        "928"
    };

    private readonly HelperRuntimeState helperState;
    private readonly Func<TaskType, string> formatTaskType;

    public FarmTaskExecutor(HelperRuntimeState helperState, Func<TaskType, string>? formatTaskType = null)
    {
        this.helperState = helperState;
        this.formatTaskType = formatTaskType ?? (type => type.ToString());
    }

    public TaskExecutionResult Execute(HelperTask task)
    {
        var location = ResolveLocation(task.Target.Location);
        if (location is null)
            return TaskExecutionResult.Skip("location-not-loaded");

        helperState.Location = location.NameOrUniqueName;
        helperState.X = task.Target.X;
        helperState.Y = task.Target.Y;
        helperState.Status = formatTaskType(task.Type);
        helperState.CurrentTaskKey = task.Key;

        return task.Type switch
        {
            TaskType.WaterCrop => WaterCrop(location, task.Target),
            TaskType.HarvestCrop => HarvestCrop(location, task.Target),
            TaskType.TillSprinklerSoil => TillSprinklerSoil(location, task.Target),
            TaskType.CollectMachine => CollectMachine(location, task.Target),
            TaskType.RefillHay => RefillHay(location),
            TaskType.PetAnimal => PetAnimal(location, task.Target),
            TaskType.CollectAnimalProduct => CollectAnimalProduct(location, task.Target),
            _ => TaskExecutionResult.Skip("task-type-not-implemented-in-mvp")
        };
    }

    private static GameLocation? ResolveLocation(string name)
    {
        if (string.Equals(name, "Farm", StringComparison.OrdinalIgnoreCase))
            return Game1.getFarm();

        return Game1.getLocationFromName(name);
    }

    private static TaskExecutionResult WaterCrop(GameLocation location, TaskTarget target)
    {
        if (!TryGetDirt(location, target, out var dirt))
            return TaskExecutionResult.Skip("crop-tile-not-found");

        if (dirt.crop is null)
            return TaskExecutionResult.Skip("crop-already-gone");

        if (dirt.state.Value == HoeDirt.watered)
            return TaskExecutionResult.Skip("crop-already-watered");

        dirt.state.Value = HoeDirt.watered;
        return TaskExecutionResult.Complete("watered-crop");
    }

    private static TaskExecutionResult HarvestCrop(GameLocation location, TaskTarget target)
    {
        if (!TryGetDirt(location, target, out var dirt))
            return TaskExecutionResult.Skip("crop-tile-not-found");

        if (dirt.crop is null)
            return TaskExecutionResult.Skip("crop-already-gone");

        if (!TryHarvestCropByReflection(dirt, target, location))
            return TaskExecutionResult.Fail("crop-harvest-method-failed");

        return TaskExecutionResult.Complete("harvested-crop");
    }

    private static TaskExecutionResult CollectMachine(GameLocation location, TaskTarget target)
    {
        var tile = new Vector2(target.X, target.Y);
        if (!location.objects.TryGetValue(tile, out var obj) || obj is not SObject machine)
            return TaskExecutionResult.Skip("machine-not-found");

        if (!machine.readyForHarvest.Value)
            return TaskExecutionResult.Skip("machine-not-ready");

        var collected = machine.checkForAction(Game1.player, justCheckingForActivity: false);
        return collected
            ? TaskExecutionResult.Complete("collected-machine")
            : TaskExecutionResult.Fail("machine-check-action-returned-false");
    }

    private static TaskExecutionResult TillSprinklerSoil(GameLocation location, TaskTarget target)
    {
        var tile = new Vector2(target.X, target.Y);
        if (location.objects.ContainsKey(tile))
            return TaskExecutionResult.Skip("tile-has-object");

        if (location.terrainFeatures.TryGetValue(tile, out var feature))
            return feature is HoeDirt
                ? TaskExecutionResult.Skip("soil-already-tilled")
                : TaskExecutionResult.Skip("tile-has-terrain-feature");

        var diggable = location.doesTileHaveProperty(target.X, target.Y, "Diggable", "Back");
        if (string.IsNullOrWhiteSpace(diggable))
            return TaskExecutionResult.Skip("tile-not-diggable");

        location.terrainFeatures.Add(tile, new HoeDirt());
        return TaskExecutionResult.Complete("tilled-sprinkler-soil");
    }

    private static TaskExecutionResult RefillHay(GameLocation location)
    {
        if (!string.Equals(location.GetType().Name, "AnimalHouse", StringComparison.Ordinal))
            return TaskExecutionResult.Skip("not-an-animal-house");

        var method = location.GetType().GetMethod("feedAllAnimals", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
        if (method is null)
            return TaskExecutionResult.Skip("hay-refill-method-not-found");

        try
        {
            method.Invoke(location, null);
            return TaskExecutionResult.Complete("refilled-hay");
        }
        catch (TargetInvocationException ex)
        {
            return TaskExecutionResult.Fail($"hay-refill-failed: {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    private static TaskExecutionResult PetAnimal(GameLocation location, TaskTarget target)
    {
        if (location is not AnimalHouse animalHouse)
            return TaskExecutionResult.Skip("not-an-animal-house");

        if (string.IsNullOrWhiteSpace(target.EntityId) || !long.TryParse(target.EntityId, out var animalId))
            return TaskExecutionResult.Skip("animal-id-required");

        if (!TryFindAnimal(animalHouse, animalId, out var animal))
            return TaskExecutionResult.Skip("animal-not-found");

        if (animal.wasPet.Value)
            return TaskExecutionResult.Skip("animal-already-petted");

        if (!TryInvokePet(animal))
            return TaskExecutionResult.Skip("animal-pet-method-not-found");

        return animal.wasPet.Value
            ? TaskExecutionResult.Complete("petted-animal")
            : TaskExecutionResult.Fail("animal-pet-failed");
    }

    private static TaskExecutionResult CollectAnimalProduct(GameLocation location, TaskTarget target)
    {
        if (!AnimalTaskSafety.IsAnimalHouseTypeName(location.GetType().Name))
            return TaskExecutionResult.Skip("not-an-animal-house");

        var tile = new Vector2(target.X, target.Y);
        if (!location.objects.TryGetValue(tile, out var obj) || obj is not SObject product)
            return TaskExecutionResult.Skip("animal-product-not-found");

        if (!IsSafeLooseAnimalProduct(product))
            return TaskExecutionResult.Skip("not-animal-product");

        if (TryStoreInMatchingChest(location, tile, product))
        {
            location.objects.Remove(tile);
            return TaskExecutionResult.Complete("stored-animal-product-in-chest");
        }

        if (TryAddProductToHostInventory(product))
        {
            location.objects.Remove(tile);
            return TaskExecutionResult.Complete("collected-animal-product");
        }

        return TaskExecutionResult.Skip("inventory-full");
    }

    private static bool TryGetDirt(GameLocation location, TaskTarget target, out HoeDirt dirt)
    {
        var tile = new Vector2(target.X, target.Y);
        if (location.terrainFeatures.TryGetValue(tile, out var feature) && feature is HoeDirt found)
        {
            dirt = found;
            return true;
        }

        dirt = null!;
        return false;
    }

    private static bool TryHarvestCropByReflection(HoeDirt dirt, TaskTarget target, GameLocation location)
    {
        var crop = dirt.crop;
        if (crop is null)
            return false;

        var methods = crop.GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(method => method.Name == "harvest")
            .OrderBy(method => method.GetParameters().Length);

        foreach (var method in methods)
        {
            var parameters = method.GetParameters();
            if (parameters.Length < 3)
                continue;

            if (parameters[0].ParameterType != typeof(int) || parameters[1].ParameterType != typeof(int))
                continue;

            if (!parameters[2].ParameterType.IsAssignableFrom(typeof(HoeDirt)))
                continue;

            var args = new object?[parameters.Length];
            args[0] = target.X;
            args[1] = target.Y;
            args[2] = dirt;

            for (var i = 3; i < parameters.Length; i++)
                args[i] = parameters[i].HasDefaultValue ? parameters[i].DefaultValue : null;

            var result = method.Invoke(crop, args);
            return result is not bool boolResult || boolResult;
        }

        return location.checkAction(new xTile.Dimensions.Location(target.X * Game1.tileSize, target.Y * Game1.tileSize), Game1.viewport, Game1.player);
    }

    private static bool TryFindAnimal(AnimalHouse animalHouse, long animalId, out FarmAnimal animal)
    {
        var farm = Game1.getFarm();
        foreach (var id in animalHouse.animalsThatLiveHere)
        {
            if (id != animalId || !farm.animals.TryGetValue(id, out var found))
                continue;

            animal = found;
            return true;
        }

        animal = null!;
        return false;
    }

    private static bool TryInvokePet(FarmAnimal animal)
    {
        var methods = animal.GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(method => method.Name == "pet")
            .OrderBy(method => method.GetParameters().Length);

        foreach (var method in methods)
        {
            var parameters = method.GetParameters();
            var args = new object?[parameters.Length];
            var supported = true;

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameterType = parameters[i].ParameterType;
                if (parameterType.IsAssignableFrom(typeof(Farmer)))
                    args[i] = Game1.player;
                else if (parameterType == typeof(bool))
                    args[i] = false;
                else if (parameters[i].HasDefaultValue)
                    args[i] = parameters[i].DefaultValue;
                else
                    supported = false;
            }

            if (!supported)
                continue;

            try
            {
                method.Invoke(animal, args);
                return true;
            }
            catch (TargetInvocationException)
            {
                return false;
            }
        }

        return false;
    }

    private static bool IsSafeLooseAnimalProduct(SObject obj)
    {
        return obj.CanBeGrabbed
            && !obj.bigCraftable.Value
            && LooseAnimalProductItemIds.Contains(obj.ItemId);
    }

    private static bool TryStoreInMatchingChest(GameLocation sourceLocation, Vector2 productTile, SObject product)
    {
        var item = CreateItemStack(product);
        var candidates = new List<ChestCandidate>();

        foreach (var location in Game1.locations)
        {
            foreach (var pair in location.objects.Pairs)
            {
                if (pair.Value is not Chest chest || !IsNormalChest(chest))
                    continue;

                if (!CanFullyAddToMatchingChest(chest, item))
                    continue;

                var sameLocation = ReferenceEquals(location, sourceLocation);
                var distance = sameLocation ? Vector2.DistanceSquared(productTile, pair.Key) : float.MaxValue;
                candidates.Add(new ChestCandidate(location, chest, sameLocation, distance));
            }
        }

        foreach (var candidate in candidates
            .OrderByDescending(candidate => candidate.SameLocation)
            .ThenBy(candidate => candidate.DistanceSquared))
        {
            var itemForChest = CreateItemStack(product);
            if (TryAddToMatchingChest(candidate.Chest, itemForChest))
                return true;
        }

        return false;
    }

    private static bool TryAddToMatchingChest(Chest chest, Item item)
    {
        if (!CanFullyAddToMatchingChest(chest, item))
            return false;

        var remainder = Utility.addItemToThisInventoryList(item, chest.Items, chest.GetActualCapacity());
        return remainder is null || remainder.Stack <= 0;
    }

    private static bool CanFullyAddToMatchingChest(Chest chest, Item item)
    {
        if (!chest.Items.Any(stored => stored is not null && IsSameStackKind(stored, item)))
            return false;

        var itemCopy = item.getOne();
        itemCopy.Stack = item.Stack;
        var chestCopy = new List<Item>();
        foreach (var stored in chest.Items)
        {
            if (stored is null)
            {
                chestCopy.Add(null!);
                continue;
            }

            var storedCopy = stored.getOne();
            storedCopy.Stack = stored.Stack;
            chestCopy.Add(storedCopy);
        }

        var remainder = Utility.addItemToThisInventoryList(itemCopy, chestCopy, chest.GetActualCapacity());
        return remainder is null || remainder.Stack <= 0;
    }

    private static bool TryAddProductToHostInventory(SObject product)
    {
        var item = CreateItemStack(product);
        if (!Game1.player.couldInventoryAcceptThisItem(item))
            return false;

        var remainder = Game1.player.addItemToInventory(item);
        return remainder is null || remainder.Stack <= 0;
    }

    private static Item CreateItemStack(SObject product)
    {
        var item = product.getOne();
        item.Stack = product.Stack;
        return item;
    }

    private static bool IsSameStackKind(Item left, Item right)
    {
        return string.Equals(left.QualifiedItemId, right.QualifiedItemId, StringComparison.OrdinalIgnoreCase)
            && left.Quality == right.Quality;
    }

    private static bool IsNormalChest(Chest chest)
    {
        return !chest.giftbox.Value && string.IsNullOrWhiteSpace(chest.GlobalInventoryId);
    }

    private sealed record ChestCandidate(GameLocation Location, Chest Chest, bool SameLocation, float DistanceSquared);
}

internal sealed record TaskExecutionResult(bool Completed, bool Skipped, string Reason)
{
    public static TaskExecutionResult Complete(string reason) => new(true, false, reason);
    public static TaskExecutionResult Skip(string reason) => new(false, true, reason);
    public static TaskExecutionResult Fail(string reason) => new(false, false, reason);
}
