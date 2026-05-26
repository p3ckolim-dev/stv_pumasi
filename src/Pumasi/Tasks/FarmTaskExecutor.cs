using System.Reflection;
using Microsoft.Xna.Framework;
using Pumasi.Core.Tasks;
using Pumasi.Services;
using StardewValley;
using StardewValley.TerrainFeatures;
using SObject = StardewValley.Object;

namespace Pumasi.Tasks;

internal sealed class FarmTaskExecutor
{
    private readonly HelperRuntimeState helperState;

    public FarmTaskExecutor(HelperRuntimeState helperState)
    {
        this.helperState = helperState;
    }

    public TaskExecutionResult Execute(HelperTask task)
    {
        var location = ResolveLocation(task.Target.Location);
        if (location is null)
            return TaskExecutionResult.Skip("location-not-loaded");

        helperState.Location = location.NameOrUniqueName;
        helperState.X = task.Target.X;
        helperState.Y = task.Target.Y;
        helperState.Status = task.Type.ToString();
        helperState.CurrentTaskKey = task.Key;

        return task.Type switch
        {
            TaskType.WaterCrop => WaterCrop(location, task.Target),
            TaskType.HarvestCrop => HarvestCrop(location, task.Target),
            TaskType.CollectMachine => CollectMachine(location, task.Target),
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
}

internal sealed record TaskExecutionResult(bool Completed, bool Skipped, string Reason)
{
    public static TaskExecutionResult Complete(string reason) => new(true, false, reason);
    public static TaskExecutionResult Skip(string reason) => new(false, true, reason);
    public static TaskExecutionResult Fail(string reason) => new(false, false, reason);
}
