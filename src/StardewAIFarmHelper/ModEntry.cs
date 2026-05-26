using StardewAIFarmHelper.Core.Ai;
using StardewAIFarmHelper.Core.Configuration;
using StardewAIFarmHelper.Core.Tasks;
using StardewAIFarmHelper.Game;
using StardewAIFarmHelper.Multiplayer;
using StardewAIFarmHelper.Services;
using StardewAIFarmHelper.Tasks;
using StardewAIFarmHelper.UI;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace StardewAIFarmHelper;

public sealed class ModEntry : Mod
{
    private ConfigService configService = null!;
    private TaskManager taskManager = null!;
    private HelperRuntimeState helperState = null!;
    private FarmTaskScanner scanner = null!;
    private FarmTaskExecutor executor = null!;
    private MultiplayerSyncService multiplayer = null!;
    private TodoOverlay overlay = null!;
    private HttpClient httpClient = null!;
    private int executionCooldownTicks;

    public override void Entry(IModHelper helper)
    {
        configService = new ConfigService(helper, Monitor, ModManifest);
        taskManager = new TaskManager();
        helperState = new HelperRuntimeState { Name = configService.Config.Assistant.Name };
        scanner = new FarmTaskScanner();
        executor = new FarmTaskExecutor(helperState);
        overlay = new TodoOverlay { Visible = configService.Config.Ui.ShowTodoOverlay };
        httpClient = new HttpClient();
        multiplayer = new MultiplayerSyncService(helper, Monitor, ModManifest, HandleGuestCommand);

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.GameLoop.DayStarted += OnDayStarted;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        helper.Events.Display.RenderedHud += OnRenderedHud;
        helper.Events.Input.ButtonPressed += OnButtonPressed;

        helper.ConsoleCommands.Add("aih_status", "Show AI Farm Helper status.", OnStatusCommand);
        helper.ConsoleCommands.Add("aih_scan", "Host only: scan farm tasks and enqueue safe todos.", OnScanCommand);
        helper.ConsoleCommands.Add("aih_ask", "Host only: ask Gemini to plan from current scan candidates. Usage: aih_ask <instruction>", OnAskCommand);
        helper.ConsoleCommands.Add("aih_key", "Host local only: set Gemini API key. Usage: aih_key <key>", OnApiKeyCommand);
        helper.ConsoleCommands.Add("aih_todo", "Show current todo list in the SMAPI console.", OnTodoCommand);
    }

    private ModConfig Config => configService.Config;

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        configService.RegisterGenericModConfigMenu();
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        helperState.Name = Config.Assistant.Name;
        helperState.Status = Context.IsMainPlayer ? "Host idle" : "Guest view";
        BroadcastState();
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        if (Context.IsMainPlayer && Config.Assistant.AutomationMode is AutomationMode.Confirm or AutomationMode.Auto)
            EnqueueScanResults();
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
            return;

        if (executionCooldownTicks > 0)
        {
            executionCooldownTicks--;
            return;
        }

        if (Config.Assistant.AutomationMode == AutomationMode.Off)
            return;

        ProcessNextTask();
        executionCooldownTicks = 30;
    }

    private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
    {
        var snapshot = Context.IsMainPlayer ? taskManager.CreateSnapshot() : multiplayer.LatestSnapshot;
        overlay.Draw(e.SpriteBatch, snapshot, helperState, multiplayer.LatestHelperState);
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        if (string.Equals(e.Button.ToString(), Config.Ui.ToggleOverlayButton, StringComparison.OrdinalIgnoreCase))
            overlay.Visible = !overlay.Visible;
    }

    private void OnStatusCommand(string command, string[] args)
    {
        Monitor.Log($"AI Farm Helper: host={Context.IsMainPlayer}, mode={Config.Assistant.AutomationMode}, geminiConfigured={Config.Gemini.IsConfigured}, todos={taskManager.Tasks.Count}", LogLevel.Info);
    }

    private void OnScanCommand(string command, string[] args)
    {
        if (!RequireHost())
            return;

        var added = EnqueueScanResults();
        Monitor.Log($"Queued {added} scanned task(s).", LogLevel.Info);
    }

    private void OnAskCommand(string command, string[] args)
    {
        var instruction = args.Length == 0 ? "Plan safe farm chores." : string.Join(" ", args);
        if (Context.IsMainPlayer)
            _ = PlanWithGeminiAsync(instruction);
        else
            multiplayer.SendGuestCommand(instruction);
    }

    private void OnApiKeyCommand(string command, string[] args)
    {
        if (!Context.IsMainPlayer)
        {
            Monitor.Log("Only the host should configure the Gemini API key.", LogLevel.Warn);
            return;
        }

        if (args.Length != 1 || string.IsNullOrWhiteSpace(args[0]))
        {
            Monitor.Log("Usage: aih_key <gemini-api-key>", LogLevel.Info);
            return;
        }

        configService.SetGeminiApiKey(args[0]);
        Monitor.Log("Gemini API key saved locally. It will not be synced to guests.", LogLevel.Info);
    }

    private void OnTodoCommand(string command, string[] args)
    {
        var snapshot = Context.IsMainPlayer ? taskManager.CreateSnapshot() : multiplayer.LatestSnapshot;
        if (snapshot.Items.Count == 0)
        {
            Monitor.Log("Todo list is empty.", LogLevel.Info);
            return;
        }

        foreach (var item in snapshot.Items)
            Monitor.Log($"[{item.Status}] {item.Type} {item.Location}({item.X},{item.Y}) key={item.Key}", LogLevel.Info);
    }

    private bool RequireHost()
    {
        if (Context.IsMainPlayer)
            return true;

        Monitor.Log("This command is host-only. Guests send commands through aih_ask.", LogLevel.Warn);
        return false;
    }

    private int EnqueueScanResults()
    {
        var proposals = scanner.Scan(Config);
        var added = 0;
        foreach (var proposal in proposals)
        {
            var result = taskManager.Enqueue(proposal);
            if (result.Accepted)
                added++;
        }

        BroadcastState();
        return added;
    }

    private async Task PlanWithGeminiAsync(string instruction)
    {
        if (!RequireHost())
            return;

        if (!Config.Gemini.IsConfigured)
        {
            Monitor.Log("Gemini is not configured. Run aih_key <key> and check config.json for model/base URL settings.", LogLevel.Warn);
            return;
        }

        try
        {
            httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(5, Config.Gemini.TimeoutSeconds));
            var client = new GeminiClient(httpClient, Config.Gemini);
            var planner = new GeminiPlanner(client, Config.Assistant);
            var candidates = scanner.Scan(Config);
            var summary = new FarmSummary(
                $"{Game1.currentSeason} {Game1.dayOfMonth}",
                Game1.isRaining ? "rain" : "clear",
                Game1.timeOfDay,
                candidates,
                taskManager.CreateSnapshot().Items.Select(item => new TodoItemForPrompt(item.Key, item.Type, item.Location, item.X, item.Y, item.Status.ToString())).ToArray(),
                instruction);

            var plan = await planner.PlanAsync(summary).ConfigureAwait(false);
            if (!plan.Success)
            {
                Monitor.Log($"Gemini planning failed: {plan.Error}", LogLevel.Warn);
                return;
            }

            var accepted = 0;
            foreach (var proposal in plan.Tasks)
            {
                var result = taskManager.Enqueue(proposal);
                if (result.Accepted)
                    accepted++;
            }

            helperState.Status = string.IsNullOrWhiteSpace(plan.Message) ? $"Gemini queued {accepted} task(s)" : plan.Message;
            BroadcastState();
        }
        catch (Exception ex)
        {
            Monitor.Log($"Gemini planning error: {ex.Message}", LogLevel.Warn);
        }
    }

    private void ProcessNextTask()
    {
        var claimed = taskManager.ClaimNext();
        if (claimed is null)
        {
            helperState.Status = "Idle";
            helperState.CurrentTaskKey = null;
            return;
        }

        taskManager.Start(claimed.Id);
        var result = executor.Execute(claimed);

        if (result.Completed)
            taskManager.Complete(claimed.Id, result.Reason);
        else if (result.Skipped)
            taskManager.Skip(claimed.Id, result.Reason);
        else
            taskManager.Fail(claimed.Id, result.Reason);

        helperState.Status = result.Reason;
        helperState.CurrentTaskKey = null;
        BroadcastState();
    }

    private void HandleGuestCommand(string command, long playerId)
    {
        if (!Context.IsMainPlayer)
            return;

        Monitor.Log($"Received helper command from player {playerId}: {command}", LogLevel.Info);
        _ = PlanWithGeminiAsync(command);
    }

    private void BroadcastState()
    {
        if (!Context.IsWorldReady)
            return;

        helperState.Name = Config.Assistant.Name;
        multiplayer.Broadcast(taskManager.CreateSnapshot(), helperState, Config.ToSharedSnapshot());
    }
}
