using StardewAIFarmHelper.Core.Configuration;
using StardewAIFarmHelper.Core.Tasks;

namespace StardewAIFarmHelper.Multiplayer;

public static class MessageTypes
{
    public const string TodoSnapshot = "TodoSnapshot";
    public const string HelperState = "HelperState";
    public const string GuestCommand = "GuestCommand";
    public const string SharedConfig = "SharedConfig";
}

public sealed record HelperStateMessage(
    string Name,
    string Location,
    int X,
    int Y,
    string Status,
    string? CurrentTaskKey);

public sealed record GuestCommandMessage(string Command, long PlayerId);

public sealed record TodoSnapshotMessage(TodoSnapshot Snapshot);

public sealed record SharedConfigMessage(SharedConfigSnapshot Config);
