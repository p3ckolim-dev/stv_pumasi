using Microsoft.Xna.Framework;
using Pumasi.Core.Chat;
using Pumasi.Core.Configuration;
using Pumasi.Core.Tasks;
using Pumasi.Services;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace Pumasi.Multiplayer;

internal sealed class MultiplayerSyncService
{
    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly IManifest manifest;
    private readonly Action<string, long> handleGuestCommand;
    private readonly Func<UiLanguage> getLanguage;
    private TodoSnapshot latestSnapshot = new(Array.Empty<TodoItemSnapshot>());

    public MultiplayerSyncService(
        IModHelper helper,
        IMonitor monitor,
        IManifest manifest,
        Action<string, long> handleGuestCommand,
        Func<UiLanguage> getLanguage)
    {
        this.helper = helper;
        this.monitor = monitor;
        this.manifest = manifest;
        this.handleGuestCommand = handleGuestCommand;
        this.getLanguage = getLanguage;
        helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;
    }

    public TodoSnapshot LatestSnapshot => latestSnapshot;
    public HelperStateMessage? LatestHelperState { get; private set; }
    public HelperAnswerMessage? LatestAnswer { get; private set; }

    public void Broadcast(TodoSnapshot snapshot, HelperRuntimeState state, SharedConfigSnapshot sharedConfig)
    {
        latestSnapshot = snapshot;

        if (!Context.IsMainPlayer)
            return;

        var helperStateMessage = new HelperStateMessage(
            state.Name,
            state.Location,
            state.X,
            state.Y,
            state.Status,
            state.CurrentTaskKey);

        helper.Multiplayer.SendMessage(new TodoSnapshotMessage(snapshot), MessageTypes.TodoSnapshot, new[] { manifest.UniqueID });
        helper.Multiplayer.SendMessage(helperStateMessage, MessageTypes.HelperState, new[] { manifest.UniqueID });
        helper.Multiplayer.SendMessage(new SharedConfigMessage(sharedConfig), MessageTypes.SharedConfig, new[] { manifest.UniqueID });
    }

    public void BroadcastHelperAnswer(HelperAnswerMessage answer)
    {
        LatestAnswer = answer;

        if (!Context.IsMainPlayer)
            return;

        helper.Multiplayer.SendMessage(answer, MessageTypes.HelperAnswer, new[] { manifest.UniqueID });
    }

    public void SendGuestCommand(string command)
    {
        if (Context.IsMainPlayer)
        {
            handleGuestCommand(command, Game1.player.UniqueMultiplayerID);
            return;
        }

        helper.Multiplayer.SendMessage(
            new GuestCommandMessage(command, Game1.player.UniqueMultiplayerID),
            MessageTypes.GuestCommand,
            new[] { manifest.UniqueID },
            playerIDs: new[] { Game1.MasterPlayer.UniqueMultiplayerID });
    }

    private void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
    {
        if (e.FromModID != manifest.UniqueID)
            return;

        switch (e.Type)
        {
            case MessageTypes.TodoSnapshot when !Context.IsMainPlayer:
                latestSnapshot = e.ReadAs<TodoSnapshotMessage>().Snapshot;
                break;

            case MessageTypes.HelperState when !Context.IsMainPlayer:
                LatestHelperState = e.ReadAs<HelperStateMessage>();
                break;

            case MessageTypes.HelperAnswer when !Context.IsMainPlayer:
                LatestAnswer = e.ReadAs<HelperAnswerMessage>();
                monitor.Log($"pumasi answer: {LatestAnswer.Answer}", LogLevel.Info);
                if (LatestAnswer.Sources.Count > 0)
                    monitor.Log($"sources: {string.Join(", ", LatestAnswer.Sources)}", LogLevel.Info);
                if (Context.IsWorldReady)
                {
                    PostHelperAnswerToChat(LatestAnswer);
                    var preview = LatestAnswer.Answer.Length > 96 ? LatestAnswer.Answer[..96] + "..." : LatestAnswer.Answer;
                    Game1.addHUDMessage(new HUDMessage(preview));
                }
                break;

            case MessageTypes.GuestCommand when Context.IsMainPlayer:
                var message = e.ReadAs<GuestCommandMessage>();
                handleGuestCommand(message.Command, message.PlayerId);
                break;

            case MessageTypes.SharedConfig when !Context.IsMainPlayer:
                break;

            default:
                monitor.Log($"Ignored unsupported pumasi message type '{e.Type}'.", LogLevel.Trace);
                break;
        }
    }

    private void PostHelperAnswerToChat(HelperAnswerMessage answer)
    {
        var helperName = LatestHelperState?.Name ?? "pumasi";
        foreach (var line in HelperChatFormatter.FormatAnswer(helperName, answer.Answer, answer.Sources, getLanguage()))
            Game1.chatBox?.addMessage(line, Color.LightGreen);
    }
}
