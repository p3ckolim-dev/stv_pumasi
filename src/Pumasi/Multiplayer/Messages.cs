using Pumasi.Core.Configuration;
using Pumasi.Core.Tasks;

namespace Pumasi.Multiplayer;

public static class MessageTypes
{
    public const string TodoSnapshot = "TodoSnapshot";
    public const string HelperState = "HelperState";
    public const string HelperAnswer = "HelperAnswer";
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

public sealed record HelperAnswerMessage(string Answer, IReadOnlyList<string> Sources);
