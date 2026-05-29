using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pumasi.Core.Ai;
using Pumasi.Core.Chat;
using Pumasi.Core.Commands;
using Pumasi.Core.Configuration;
using Pumasi.Core.Knowledge;
using Pumasi.Core.Net;
using Pumasi.Core.Tasks;
using Pumasi.Core.Ui;
using Pumasi.Game;
using Pumasi.Multiplayer;
using Pumasi.Services;
using Pumasi.Tasks;
using Pumasi.UI;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace Pumasi;

internal enum CommandSurface
{
    Console,
    Chat
}

public sealed class ModEntry : Mod
{
    private const string ChatCommandName = "p3ckolim.pms_pms";
    private const string ConversationMemoryDataKey = "conversation-memory";
    private const int ConversationHistoryLimit = 12;
    private const int PumasiSettingsTabSize = 64;

    private ConfigService configService = null!;
    private TaskManager taskManager = null!;
    private HelperRuntimeState helperState = null!;
    private FarmTaskScanner scanner = null!;
    private FarmTaskExecutor executor = null!;
    private MultiplayerSyncService multiplayer = null!;
    private TodoOverlay overlay = null!;
    private WikiMemoryCache wikiCache = null!;
    private readonly ConversationMemory conversationMemory = new(ConversationHistoryLimit);
    private GameMenu? pumasiSettingsGameMenu;
    private int pumasiSettingsTabIndex = -1;
    private Rectangle pumasiSettingsTabBounds = Rectangle.Empty;
    private int executionCooldownTicks;
    private DateTimeOffset lastWikiQuestionAt = DateTimeOffset.MinValue;
    private bool chatCommandsRegistered;
    private bool conversationMemoryDirty;

    public override void Entry(IModHelper helper)
    {
        configService = new ConfigService(helper, Monitor, ModManifest);
        taskManager = new TaskManager();
        helperState = new HelperRuntimeState { Name = configService.Config.Assistant.Name };
        scanner = new FarmTaskScanner();
        executor = new FarmTaskExecutor(helperState, type => PumasiText.GetTaskType(Language, type));
        overlay = new TodoOverlay { Visible = configService.Config.Ui.ShowTodoOverlay, Language = configService.Config.Ui.Language };
        wikiCache = new WikiMemoryCache();
        multiplayer = new MultiplayerSyncService(helper, Monitor, ModManifest, HandleGuestCommand, () => Language);

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.GameLoop.DayStarted += OnDayStarted;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        helper.Events.GameLoop.Saving += OnSaving;
        helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
        helper.Events.Display.MenuChanged += OnMenuChanged;
        helper.Events.Display.RenderedHud += OnRenderedHud;
        helper.Events.Display.RenderedActiveMenu += OnRenderedActiveMenu;
        helper.Events.Input.ButtonPressed += OnButtonPressed;

        helper.ConsoleCommands.Add("pms_status", "Show pumasi (품앗이) status.", OnStatusCommand);
        helper.ConsoleCommands.Add("pms_scan", "Host only: scan farm tasks and enqueue safe todos.", OnScanCommand);
        helper.ConsoleCommands.Add("pms_ask", "Ask pumasi to answer a wiki question or plan safe farm work. Usage: pms_ask <instruction>", OnAskCommand);
        helper.ConsoleCommands.Add("pms_key", "Host local only: set Gemini API key. Usage: pms_key <key>", OnApiKeyCommand);
        helper.ConsoleCommands.Add("pms_todo", "Show or reorder current todo list. Usage: pms_todo [move <from> <to>|up <index>|down <index>|top <index>|bottom <index>]", OnTodoCommand);
        helper.ConsoleCommands.Add("pms_work", "Host only: toggle work categories. Usage: pms_work animals on|off", OnWorkCommand);
    }

    private ModConfig Config => configService.Config;
    private UiLanguage Language => Config.Ui.Language;
    private string T(PumasiTextKey key) => PumasiText.Get(Language, key);
    private string T(PumasiTextKey key, params object[] args) => PumasiText.Format(Language, key, args);

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        configService.RegisterGenericModConfigMenu();
        RegisterChatCommands();
    }

    private void RegisterChatCommands()
    {
        if (chatCommandsRegistered)
            return;

        if (ChatCommands.Exists(ChatCommandName))
        {
            Monitor.Log("pumasi chat command was already registered; skipping duplicate registration.", LogLevel.Trace);
            chatCommandsRegistered = true;
            return;
        }

        try
        {
            ChatCommands.Register(
                ChatCommandName,
                OnPmsChatCommand,
                _ => T(PumasiTextKey.HelpUsage),
                new[] { "pms", "pms_ask", "pms_status", "pms_scan", "pms_todo", "pms_work", "pms_key" },
                mainOnly: false,
                multiplayerOnly: false,
                cheatsOnly: false);

            chatCommandsRegistered = true;
            Monitor.Log("Registered in-game chat commands: /pms, /pms_ask, /pms_status, /pms_scan, /pms_todo.", LogLevel.Info);
        }
        catch (Exception ex)
        {
            Monitor.Log($"Could not register pumasi chat commands: {ex.Message}", LogLevel.Warn);
        }
    }

    private void OnPmsChatCommand(string[] command, ChatBox chat)
    {
        var parsed = PumasiCommandParser.ParseChatInput($"/{string.Join(" ", command)}");
        RunPumasiCommand(parsed, CommandSurface.Chat);
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        LoadConversationMemory();
        helperState.Name = Config.Assistant.Name;
        helperState.Status = Context.IsMainPlayer ? T(PumasiTextKey.HostIdle) : T(PumasiTextKey.GuestView);
        BroadcastState();
    }

    private void OnSaving(object? sender, SavingEventArgs e)
    {
        SaveConversationMemoryIfNeeded(force: true);
    }

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        conversationMemory.Clear();
        conversationMemoryDirty = false;
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        if (Context.IsMainPlayer && Config.Assistant.AutomationMode != AutomationMode.Off)
            EnqueueMorningTodos();
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        SaveConversationMemoryIfNeeded(force: false);

        if (!Context.IsMainPlayer)
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
        overlay.Language = Language;
        var snapshot = Context.IsMainPlayer ? taskManager.CreateSnapshot() : multiplayer.LatestSnapshot;
        overlay.Draw(e.SpriteBatch, snapshot, helperState, multiplayer.LatestHelperState);
    }

    private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
    {
        if (e.NewMenu is GameMenu gameMenu)
        {
            InjectPumasiSettingsPage(gameMenu);
            return;
        }

        pumasiSettingsGameMenu = null;
        pumasiSettingsTabIndex = -1;
        pumasiSettingsTabBounds = Rectangle.Empty;
    }

    private void OnRenderedActiveMenu(object? sender, RenderedActiveMenuEventArgs e)
    {
        if (Game1.activeClickableMenu is GameMenu gameMenu && IsInjectedPumasiSettingsMenu(gameMenu))
            DrawPumasiSettingsTab(e.SpriteBatch, gameMenu);
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        if (e.Button == SButton.MouseLeft && TryOpenPumasiSettingsTab(e))
            return;

        if (e.Button == SButton.MouseLeft)
        {
            var cursor = e.Cursor.GetScaledScreenPixels();
            if (overlay.TryTogglePopupClick((int)cursor.X, (int)cursor.Y))
            {
                Game1.playSound("smallSelect");
                Helper.Input.Suppress(e.Button);
                return;
            }
        }

        if (string.Equals(e.Button.ToString(), Config.Ui.ToggleOverlayButton, StringComparison.OrdinalIgnoreCase))
        {
            overlay.Visible = !overlay.Visible;
            return;
        }

        if (e.Button == SButton.MouseLeft && Context.IsMainPlayer)
        {
            var cursor = e.Cursor.GetScaledScreenPixels();
            if (overlay.TryResolveReorderClick((int)cursor.X, (int)cursor.Y, out var move))
            {
                MoveTodoFromOverlay(move);
                Helper.Input.Suppress(e.Button);
            }
        }
    }

    private void InjectPumasiSettingsPage(GameMenu gameMenu)
    {
        var pageIndex = gameMenu.pages.FindIndex(page => page is PumasiSettingsPage);
        if (pageIndex < 0)
        {
            pageIndex = gameMenu.pages.Count;
            gameMenu.pages.Add(new PumasiSettingsPage(
                gameMenu.xPositionOnScreen,
                gameMenu.yPositionOnScreen,
                gameMenu.width,
                gameMenu.height,
                Config,
                () => Context.IsMainPlayer,
                configService.Save,
                OnPumasiSettingsChanged));
        }

        pumasiSettingsGameMenu = gameMenu;
        pumasiSettingsTabIndex = pageIndex;
        pumasiSettingsTabBounds = CreatePumasiSettingsTabBounds(gameMenu);
    }

    private bool TryOpenPumasiSettingsTab(ButtonPressedEventArgs e)
    {
        if (Game1.activeClickableMenu is not GameMenu gameMenu || !IsInjectedPumasiSettingsMenu(gameMenu))
            return false;

        var cursor = e.Cursor.GetScaledScreenPixels();
        if (!pumasiSettingsTabBounds.Contains((int)cursor.X, (int)cursor.Y))
            return false;

        if (gameMenu.currentTab >= 0
            && gameMenu.currentTab < gameMenu.pages.Count
            && !gameMenu.pages[gameMenu.currentTab].readyToClose())
        {
            return false;
        }

        OpenPumasiSettingsTab(gameMenu);
        Helper.Input.Suppress(e.Button);
        return true;
    }

    private void OpenPumasiSettingsTab(GameMenu gameMenu)
    {
        gameMenu.currentTab = pumasiSettingsTabIndex;
        gameMenu.invisible = false;
        gameMenu.width = 800 + IClickableMenu.borderWidth * 2;
        gameMenu.initializeUpperRightCloseButton();
        gameMenu.pages[pumasiSettingsTabIndex].populateClickableComponentList();
        gameMenu.AddTabsToClickableComponents(gameMenu.pages[pumasiSettingsTabIndex]);
        gameMenu.setTabNeighborsForCurrentPage();
        if (Game1.options.SnappyMenus)
            gameMenu.snapToDefaultClickableComponent();

        Game1.playSound("smallSelect");
    }

    private void OnPumasiSettingsChanged()
    {
        overlay.Visible = Config.Ui.ShowTodoOverlay;
        overlay.Language = Language;
        helperState.Name = Config.Assistant.Name;
        BroadcastState();
    }

    private bool IsInjectedPumasiSettingsMenu(GameMenu gameMenu)
    {
        return ReferenceEquals(pumasiSettingsGameMenu, gameMenu)
            && pumasiSettingsTabIndex >= 0
            && pumasiSettingsTabIndex < gameMenu.pages.Count
            && gameMenu.pages[pumasiSettingsTabIndex] is PumasiSettingsPage;
    }

    private static Rectangle CreatePumasiSettingsTabBounds(GameMenu gameMenu)
    {
        var exitTab = gameMenu.tabs.FirstOrDefault(tab => tab.name == "exit");
        if (exitTab is not null)
            return new Rectangle(exitTab.bounds.Right, exitTab.bounds.Y, exitTab.bounds.Width, exitTab.bounds.Height);

        return new Rectangle(
            gameMenu.xPositionOnScreen + 704,
            gameMenu.yPositionOnScreen + IClickableMenu.tabYPositionRelativeToMenuY + PumasiSettingsTabSize,
            PumasiSettingsTabSize,
            PumasiSettingsTabSize);
    }

    private void DrawPumasiSettingsTab(SpriteBatch spriteBatch, GameMenu gameMenu)
    {
        var yOffset = gameMenu.currentTab == pumasiSettingsTabIndex ? 8 : 0;
        var position = new Vector2(pumasiSettingsTabBounds.X, pumasiSettingsTabBounds.Y + yOffset);
        spriteBatch.Draw(
            Game1.mouseCursors,
            position,
            new Rectangle(96, 368, 16, 16),
            Color.White,
            0f,
            Vector2.Zero,
            4f,
            SpriteEffects.None,
            0.0001f);

        var label = "P";
        var labelSize = Game1.smallFont.MeasureString(label);
        var labelPosition = new Vector2(
            pumasiSettingsTabBounds.X + (pumasiSettingsTabBounds.Width - labelSize.X) / 2f,
            pumasiSettingsTabBounds.Y + yOffset + 17);
        spriteBatch.DrawString(Game1.smallFont, label, labelPosition + new Vector2(1, 1), Color.White * 0.55f);
        spriteBatch.DrawString(Game1.smallFont, label, labelPosition, Color.DarkGreen);

        if (pumasiSettingsTabBounds.Contains(Game1.getMouseX(), Game1.getMouseY()))
            IClickableMenu.drawHoverText(spriteBatch, T(PumasiTextKey.SettingsTitle), Game1.smallFont);
    }

    private void OnStatusCommand(string command, string[] args)
    {
        RunPumasiCommand(new PumasiCommand(PumasiCommandKind.Status, string.Empty), CommandSurface.Console);
    }

    private void OnScanCommand(string command, string[] args)
    {
        RunPumasiCommand(new PumasiCommand(PumasiCommandKind.Scan, string.Empty), CommandSurface.Console);
    }

    private void OnAskCommand(string command, string[] args)
    {
        var instruction = args.Length == 0 ? "Plan safe farm chores." : string.Join(" ", args);
        RunPumasiCommand(new PumasiCommand(PumasiCommandKind.Ask, instruction), CommandSurface.Console);
    }

    private void OnApiKeyCommand(string command, string[] args)
    {
        if (!Context.IsMainPlayer)
        {
            Monitor.Log(T(PumasiTextKey.HostOnlyCommand), LogLevel.Warn);
            return;
        }

        if (args.Length != 1 || string.IsNullOrWhiteSpace(args[0]))
        {
            Monitor.Log(T(PumasiTextKey.ApiKeyUsage), LogLevel.Info);
            return;
        }

        configService.SetGeminiApiKey(args[0]);
        Monitor.Log(T(PumasiTextKey.ApiKeySaved), LogLevel.Info);
    }

    private void OnTodoCommand(string command, string[] args)
    {
        RunPumasiCommand(new PumasiCommand(PumasiCommandKind.Todo, string.Join(" ", args)), CommandSurface.Console);
    }

    private void OnWorkCommand(string command, string[] args)
    {
        RunPumasiCommand(new PumasiCommand(PumasiCommandKind.WorkCategory, string.Join(" ", args)), CommandSurface.Console);
    }

    private void RunPumasiCommand(PumasiCommand command, CommandSurface surface)
    {
        switch (command.Kind)
        {
            case PumasiCommandKind.Ask:
                AskPumasi(command.Argument, surface);
                break;

            case PumasiCommandKind.Status:
                SendCommandFeedback(BuildStatusMessage(), surface);
                break;

            case PumasiCommandKind.Scan:
                ScanForTasks(surface);
                break;

            case PumasiCommandKind.Todo:
                ShowTodoList(command.Argument, surface);
                break;

            case PumasiCommandKind.WorkCategory:
                SetWorkCategory(command.Argument, surface);
                break;

            case PumasiCommandKind.Help:
                SendCommandFeedback(T(PumasiTextKey.HelpUsage), surface);
                break;

            case PumasiCommandKind.ApiKeyRejected:
                SendCommandFeedback(T(PumasiTextKey.ApiKeyChatRejected), surface, LogLevel.Warn);
                break;

            case PumasiCommandKind.None:
            default:
                break;
        }
    }

    private void AskPumasi(string instruction, CommandSurface surface)
    {
        if (Context.IsMainPlayer)
        {
            _ = HandleAskAsync(instruction);
            return;
        }

        multiplayer.SendGuestCommand(instruction);
        SendCommandFeedback(T(PumasiTextKey.GuestRequestSent), surface);
    }

    private void ScanForTasks(CommandSurface surface)
    {
        if (!RequireHost(surface))
            return;

        var added = EnqueueScanResults();
        SendCommandFeedback(T(PumasiTextKey.ScanQueuedTasks, added), surface);
    }

    private void ShowTodoList(CommandSurface surface)
    {
        ShowTodoList(string.Empty, surface);
    }

    private void ShowTodoList(string argument, CommandSurface surface)
    {
        var snapshot = Context.IsMainPlayer ? taskManager.CreateSnapshot() : multiplayer.LatestSnapshot;
        var visibleItems = snapshot.Items
            .Where(item => item.Status is HelperTaskStatus.Queued or HelperTaskStatus.Claimed or HelperTaskStatus.InProgress)
            .ToArray();

        if (!string.IsNullOrWhiteSpace(argument))
        {
            if (!Context.IsMainPlayer)
            {
                SendCommandFeedback(T(PumasiTextKey.TodoReorderHostOnly), surface, LogLevel.Warn);
                return;
            }

            HandleTodoReorder(argument, visibleItems.Length, surface);
            snapshot = taskManager.CreateSnapshot();
            visibleItems = snapshot.Items
                .Where(item => item.Status is HelperTaskStatus.Queued or HelperTaskStatus.Claimed or HelperTaskStatus.InProgress)
                .ToArray();
        }

        if (visibleItems.Length == 0)
        {
            SendCommandFeedback(T(PumasiTextKey.TodoListEmpty), surface);
            return;
        }

        for (var i = 0; i < visibleItems.Length; i++)
        {
            var item = visibleItems[i];
            var status = PumasiText.GetTaskStatus(Language, item.Status);
            var type = PumasiText.GetTaskType(Language, item.Type);
            SendCommandFeedback($"#{i + 1} [{status}] {type} {item.Location}({item.X},{item.Y}) key={item.Key}", surface);
        }
    }

    private void HandleTodoReorder(string argument, int visibleCount, CommandSurface surface)
    {
        var parts = argument.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
            return;

        if (!TryResolveTodoMove(parts, visibleCount, out var from, out var to, out var error))
        {
            SendCommandFeedback(T(PumasiTextKey.TodoReorderUsage, error), surface, LogLevel.Warn);
            return;
        }

        var result = taskManager.MoveActiveTask(from, to);
        if (!result.Moved)
        {
            SendCommandFeedback(T(PumasiTextKey.TodoReorderFailed, result.Reason), surface, LogLevel.Warn);
            return;
        }

        SendCommandFeedback(result.Reason == "no-change" ? T(PumasiTextKey.TodoOrderUnchanged) : T(PumasiTextKey.TodoMoved, from, to), surface);
        BroadcastState();
    }

    private void MoveTodoFromOverlay(TodoReorderMove move)
    {
        var result = taskManager.MoveActiveTask(move.FromPosition, move.ToPosition);
        var message = result.Moved
            ? result.Reason == "no-change" ? T(PumasiTextKey.TodoOrderUnchanged) : T(PumasiTextKey.TodoMoved, move.FromPosition, move.ToPosition)
            : T(PumasiTextKey.TodoReorderFailed, result.Reason);

        helperState.Status = message;
        Monitor.Log(message, result.Moved ? LogLevel.Info : LogLevel.Warn);
        Game1.addHUDMessage(new HUDMessage(message));
        BroadcastState();
    }

    private void SetWorkCategory(string argument, CommandSurface surface)
    {
        if (!RequireHost(surface))
            return;

        var parts = argument.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            SendCommandFeedback(BuildWorkCategoryStatus(), surface);
            return;
        }

        if (!TryResolveWorkCategory(parts[0], out var categoryName, out var getter, out var setter))
        {
            SendCommandFeedback(T(PumasiTextKey.WorkCategoryUsage), surface, LogLevel.Warn);
            return;
        }

        if (parts.Length == 1)
        {
            SendCommandFeedback($"{categoryName}={FormatOnOff(getter())}", surface);
            return;
        }

        if (!TryParseOnOff(parts[1], out var enabled))
        {
            SendCommandFeedback(T(PumasiTextKey.OnOffUsage), surface, LogLevel.Warn);
            return;
        }

        setter(enabled);
        configService.Save();
        BroadcastState();
        SendCommandFeedback(T(PumasiTextKey.WorkCategorySaved, categoryName, FormatOnOff(enabled)), surface);
    }

    private bool TryResolveTodoMove(string[] parts, int visibleCount, out int from, out int to, out string error)
    {
        from = 0;
        to = 0;
        error = string.Empty;

        if (visibleCount == 0)
        {
            error = T(PumasiTextKey.NoActiveTodos);
            return false;
        }

        var action = parts[0].ToLowerInvariant();
        switch (action)
        {
            case "move" when parts.Length == 3 && TryParsePosition(parts[1], out from) && TryParsePosition(parts[2], out to):
                return true;

            case "up" when parts.Length == 2 && TryParsePosition(parts[1], out from):
                to = from - 1;
                return true;

            case "down" when parts.Length == 2 && TryParsePosition(parts[1], out from):
                to = from + 1;
                return true;

            case "top" when parts.Length == 2 && TryParsePosition(parts[1], out from):
                to = 1;
                return true;

            case "bottom" when parts.Length == 2 && TryParsePosition(parts[1], out from):
                to = visibleCount;
                return true;

            default:
                error = T(PumasiTextKey.UnknownReorderCommand);
                return false;
        }
    }

    private static bool TryParsePosition(string value, out int position)
    {
        return int.TryParse(value, out position) && position > 0;
    }

    private string BuildStatusMessage()
    {
        return T(PumasiTextKey.StatusMessage, Context.IsMainPlayer, Config.Assistant.AutomationMode, Config.Gemini.IsConfigured, taskManager.Tasks.Count, conversationMemory.Turns.Count);
    }

    private string BuildWorkCategoryStatus()
    {
        var categories = Config.Assistant.WorkCategories;
        return T(
            PumasiTextKey.WorkCategoryStatus,
            FormatOnOff(categories.Crops),
            FormatOnOff(categories.Machines),
            FormatOnOff(categories.Animals),
            FormatOnOff(categories.Chests),
            FormatOnOff(categories.Planting));
    }

    private bool TryResolveWorkCategory(
        string value,
        out string categoryName,
        out Func<bool> getter,
        out Action<bool> setter)
    {
        var categories = Config.Assistant.WorkCategories;
        switch (value.Trim().ToLowerInvariant())
        {
            case "crops":
            case "crop":
                categoryName = "crops";
                getter = () => categories.Crops;
                setter = enabled => categories.Crops = enabled;
                return true;

            case "machines":
            case "machine":
                categoryName = "machines";
                getter = () => categories.Machines;
                setter = enabled => categories.Machines = enabled;
                return true;

            case "animals":
            case "animal":
                categoryName = "animals";
                getter = () => categories.Animals;
                setter = enabled => categories.Animals = enabled;
                return true;

            case "chests":
            case "chest":
                categoryName = "chests";
                getter = () => categories.Chests;
                setter = enabled => categories.Chests = enabled;
                return true;

            case "planting":
            case "plant":
                categoryName = "planting";
                getter = () => categories.Planting;
                setter = enabled => categories.Planting = enabled;
                return true;

            default:
                categoryName = string.Empty;
                getter = () => false;
                setter = _ => { };
                return false;
        }
    }

    private static bool TryParseOnOff(string value, out bool enabled)
    {
        switch (value.Trim().ToLowerInvariant())
        {
            case "on":
            case "enable":
            case "enabled":
            case "true":
            case "1":
            case "켜":
            case "켜기":
                enabled = true;
                return true;

            case "off":
            case "disable":
            case "disabled":
            case "false":
            case "0":
            case "꺼":
            case "끄기":
                enabled = false;
                return true;

            default:
                enabled = false;
                return false;
        }
    }

    private string FormatOnOff(bool enabled)
    {
        return PumasiText.FormatOnOff(Language, enabled);
    }

    private bool RequireHost(CommandSurface surface = CommandSurface.Console)
    {
        if (Context.IsMainPlayer)
            return true;

        SendCommandFeedback(T(PumasiTextKey.HostOnlyCommand), surface, LogLevel.Warn);
        return false;
    }

    private void SendCommandFeedback(string message, CommandSurface surface, LogLevel level = LogLevel.Info)
    {
        Monitor.Log(message, level);

        if (surface != CommandSurface.Chat || !Context.IsWorldReady)
            return;

        Game1.chatBox?.addMessage(message, Color.LightGreen);
        Game1.addHUDMessage(new HUDMessage(message));
    }

    private void EnqueueMorningTodos()
    {
        var limit = Math.Max(0, Config.Assistant.MorningTodoLimit);
        if (limit == 0)
            return;

        var added = EnqueueScanResults(limit, broadcast: false);
        helperState.Status = added > 0
            ? T(PumasiTextKey.MorningScanQueued, added)
            : T(PumasiTextKey.MorningScanFoundNoTodos);
        Monitor.Log(helperState.Status, LogLevel.Info);
        BroadcastState();
    }

    private int EnqueueScanResults(int? limit = null, bool broadcast = true)
    {
        var proposals = scanner.Scan(Config);
        if (limit is > 0)
            proposals = proposals.Take(limit.Value).ToArray();

        var added = 0;
        foreach (var proposal in proposals)
        {
            var result = taskManager.Enqueue(proposal);
            if (result.Accepted)
                added++;
        }

        if (broadcast)
            BroadcastState();

        return added;
    }

    private async Task PlanWithGeminiAsync(string instruction)
    {
        if (!RequireHost())
            return;

        if (!Config.Gemini.IsConfigured)
        {
            Monitor.Log(T(PumasiTextKey.GeminiNotConfiguredForPlanning), LogLevel.Warn);
            return;
        }

        try
        {
            using var requestHttpClient = PumasiHttpClientFactory.Create(Config.Gemini.TimeoutSeconds);
            var client = new GeminiClient(requestHttpClient, Config.Gemini);
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

            helperState.Status = string.IsNullOrWhiteSpace(plan.Message) ? T(PumasiTextKey.GeminiQueuedTasks, accepted) : plan.Message;
            RememberConversation("assistant", helperState.Status);
            BroadcastState();
        }
        catch (Exception ex)
        {
            Monitor.Log($"Gemini planning error: {ex.Message}", LogLevel.Warn);
        }
    }

    private async Task HandleAskAsync(string instruction)
    {
        RememberConversation("user", instruction);

        var classifier = new KnowledgeIntentClassifier();
        switch (classifier.Classify(instruction))
        {
            case KnowledgeIntent.TaskPlanning:
                await PlanWithGeminiAsync(instruction).ConfigureAwait(false);
                break;

            case KnowledgeIntent.WikiAnswer:
                await AnswerWithWikiAsync(instruction).ConfigureAwait(false);
                break;

            case KnowledgeIntent.Ambiguous:
            default:
                await RouteAmbiguousInputWithGeminiAsync(instruction).ConfigureAwait(false);
                break;
        }
    }

    private async Task RouteAmbiguousInputWithGeminiAsync(string instruction)
    {
        if (!RequireHost())
            return;

        if (!Config.Gemini.IsConfigured)
        {
            PublishHelperAnswer(T(PumasiTextKey.ContextGeminiMissing), Array.Empty<string>());
            return;
        }

        try
        {
            using var requestHttpClient = PumasiHttpClientFactory.Create(Config.Gemini.TimeoutSeconds);
            var client = new GeminiClient(requestHttpClient, Config.Gemini);
            var currentTodos = taskManager.CreateSnapshot().Items
                .Where(item => item.Status is HelperTaskStatus.Queued or HelperTaskStatus.Claimed or HelperTaskStatus.InProgress)
                .Select((item, index) => $"#{index + 1} {item.Key} [{item.Status}] {item.Reason}")
                .ToArray();

            var prompt = ContextualIntentRouter.BuildPrompt(instruction, conversationMemory.Turns, currentTodos);
            var modelText = await client.GenerateTextAsync(prompt).ConfigureAwait(false);
            var routed = ContextualIntentRouter.ParseResponse(modelText);
            if (!routed.Success)
            {
                Monitor.Log($"Contextual intent routing failed: {routed.Error}", LogLevel.Warn);
                PublishHelperAnswer(T(PumasiTextKey.ContextRoutingUnreadable), Array.Empty<string>());
                return;
            }

            var rewritten = string.IsNullOrWhiteSpace(routed.RewrittenInput) ? instruction : routed.RewrittenInput;
            switch (routed.Intent)
            {
                case ContextualIntentKind.TaskPlanning:
                    await PlanWithGeminiAsync(rewritten).ConfigureAwait(false);
                    break;

                case ContextualIntentKind.WikiAnswer:
                    await AnswerWithWikiAsync(rewritten).ConfigureAwait(false);
                    break;

                case ContextualIntentKind.ChatAnswer:
                    PublishHelperAnswer(string.IsNullOrWhiteSpace(routed.Answer) ? T(PumasiTextKey.ChatAnswerFallback) : routed.Answer, Array.Empty<string>());
                    break;

                case ContextualIntentKind.Clarify:
                default:
                    PublishHelperAnswer(string.IsNullOrWhiteSpace(routed.Answer) ? T(PumasiTextKey.ClarifyFallback) : routed.Answer, Array.Empty<string>());
                    break;
            }
        }
        catch (Exception ex)
        {
            Monitor.Log($"Contextual intent routing error: {ex.Message}", LogLevel.Warn);
            PublishHelperAnswer(T(PumasiTextKey.ContextReadError), Array.Empty<string>());
        }
    }

    private async Task AnswerWithWikiAsync(string question)
    {
        if (!RequireHost())
            return;

        if (!Config.WikiAnswers.WikiAnswersEnabled)
        {
            PublishHelperAnswer(T(PumasiTextKey.WikiAnswersDisabled), Array.Empty<string>());
            return;
        }

        var cooldown = TimeSpan.FromSeconds(Math.Max(0, Config.WikiAnswers.WikiQuestionCooldownSeconds));
        var now = DateTimeOffset.UtcNow;
        if (cooldown > TimeSpan.Zero && now - lastWikiQuestionAt < cooldown)
        {
            PublishHelperAnswer(T(PumasiTextKey.WikiCooldown), Array.Empty<string>());
            return;
        }

        lastWikiQuestionAt = now;

        try
        {
            using var requestHttpClient = PumasiHttpClientFactory.Create(Config.Gemini.TimeoutSeconds);
            var wikiClient = new WikiClient(requestHttpClient, new WikiClientOptions(Config.WikiAnswers.WikiBaseUrl));
            IReadOnlyList<WikiSearchResult> searchResults;
            if (!wikiCache.TryGetSearch(question, out searchResults))
            {
                var search = await wikiClient.SearchAsync(question, Math.Max(1, Config.WikiAnswers.WikiMaxPages)).ConfigureAwait(false);
                if (!search.Success)
                {
                    PublishHelperAnswer(T(PumasiTextKey.WikiUnavailable), Array.Empty<string>());
                    return;
                }

                searchResults = search.Value;
                wikiCache.SetSearch(question, searchResults);
            }

            if (searchResults.Count == 0)
            {
                PublishHelperAnswer(T(PumasiTextKey.WikiNoResults), Array.Empty<string>());
                return;
            }

            var extracts = new List<WikiPageExtract>();
            foreach (var result in searchResults.Take(Math.Max(1, Config.WikiAnswers.WikiMaxPages)))
            {
                if (!wikiCache.TryGetExtract(result.Title, out var cachedExtract))
                {
                    var extract = await wikiClient.GetExtractAsync(result.Title).ConfigureAwait(false);
                    if (!extract.Success || string.IsNullOrWhiteSpace(extract.Value.Extract))
                        continue;

                    cachedExtract = extract.Value;
                    wikiCache.SetExtract(result.Title, cachedExtract);
                }

                extracts.Add(cachedExtract);
            }

            var pagesForContext = extracts.Count > 0
                ? extracts
                : searchResults.Select(result => new WikiPageExtract(result.Title, result.Url, result.Snippet)).ToList();

            var context = new WikiContextBuilder(new WikiContextOptions(
                    Math.Max(1, Config.WikiAnswers.WikiMaxPages),
                    Math.Max(1000, Config.WikiAnswers.WikiContextCharacterLimit)))
                .Build(pagesForContext);

            if (!Config.Gemini.IsConfigured)
            {
                PublishHelperAnswer(T(PumasiTextKey.WikiGeminiMissingSummary), FormatSources(context.Sources));
                return;
            }

            var gemini = new GeminiClient(requestHttpClient, Config.Gemini);
            var planner = new GroundedAnswerPlanner(Config.Assistant);
            var modelText = await gemini.GenerateTextAsync(planner.BuildPrompt(question, context)).ConfigureAwait(false);
            var answer = GroundedAnswerPlanner.ParseAnswer(modelText);
            if (!answer.Success)
            {
                PublishHelperAnswer(T(PumasiTextKey.WikiSummaryFailed), FormatSources(context.Sources));
                return;
            }

            PublishHelperAnswer(answer.Answer, FormatSources(answer.Sources.Count > 0 ? answer.Sources : context.Sources));
        }
        catch (Exception ex)
        {
            Monitor.Log($"Wiki grounded answer failed: {ex.Message}", LogLevel.Warn);
            PublishHelperAnswer(T(PumasiTextKey.WikiAnswerFailed), Array.Empty<string>());
        }
    }

    private void PublishHelperAnswer(string answer, IReadOnlyList<string> sources)
    {
        RememberConversation("assistant", answer);
        helperState.Status = answer.Length > 96 ? answer[..96] + "..." : answer;
        multiplayer.BroadcastHelperAnswer(new HelperAnswerMessage(answer, sources));
        Monitor.Log($"pumasi answer: {answer}", LogLevel.Info);
        if (sources.Count > 0)
            Monitor.Log($"sources: {string.Join(", ", sources)}", LogLevel.Info);
        PostHelperAnswerToChat(answer, sources);
        if (Context.IsWorldReady)
            Game1.addHUDMessage(new HUDMessage($"{helperState.Name}: {helperState.Status}"));
        BroadcastState();
    }

    private void RememberConversation(string role, string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        conversationMemory.Remember(role, text);
        conversationMemoryDirty = true;
    }

    private void LoadConversationMemory()
    {
        conversationMemory.Clear();
        conversationMemoryDirty = false;

        if (!Context.IsMainPlayer)
            return;

        try
        {
            var data = Helper.Data.ReadSaveData<ConversationMemorySaveData>(ConversationMemoryDataKey);
            conversationMemory.Restore(data);
            Monitor.Log($"Loaded {conversationMemory.Turns.Count} pumasi conversation turn(s) for this save.", LogLevel.Trace);
        }
        catch (Exception ex)
        {
            Monitor.Log($"Could not load pumasi conversation memory: {ex.Message}", LogLevel.Warn);
        }
    }

    private void SaveConversationMemoryIfNeeded(bool force)
    {
        if (!Context.IsWorldReady || !Context.IsMainPlayer)
            return;

        if (!force && !conversationMemoryDirty)
            return;

        try
        {
            Helper.Data.WriteSaveData(ConversationMemoryDataKey, conversationMemory.ToSaveData());
            conversationMemoryDirty = false;
        }
        catch (Exception ex)
        {
            Monitor.Log($"Could not save pumasi conversation memory: {ex.Message}", LogLevel.Warn);
        }
    }

    private void PostHelperAnswerToChat(string answer, IReadOnlyList<string> sources)
    {
        if (!Context.IsWorldReady)
            return;

        foreach (var line in HelperChatFormatter.FormatAnswer(helperState.Name, answer, sources, Language))
            Game1.chatBox?.addMessage(line, Color.LightGreen);
    }

    private static IReadOnlyList<string> FormatSources(IReadOnlyList<WikiSource> sources)
    {
        return sources
            .Where(source => !string.IsNullOrWhiteSpace(source.Title) || !string.IsNullOrWhiteSpace(source.Url))
            .Select(source => string.IsNullOrWhiteSpace(source.Url) ? source.Title : $"{source.Title} - {source.Url}")
            .ToArray();
    }

    private void ProcessNextTask()
    {
        var claimed = taskManager.ClaimNext();
        if (claimed is null)
        {
            helperState.Status = T(PumasiTextKey.TodoIdle);
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

        helperState.Status = PumasiText.GetExecutionReason(Language, result.Reason);
        helperState.CurrentTaskKey = null;
        BroadcastState();
    }

    private void HandleGuestCommand(string command, long playerId)
    {
        if (!Context.IsMainPlayer)
            return;

        Monitor.Log($"Received helper command from player {playerId}: {command}", LogLevel.Info);
        _ = HandleAskAsync(command);
    }

    private void BroadcastState()
    {
        if (!Context.IsWorldReady)
            return;

        helperState.Name = Config.Assistant.Name;
        multiplayer.Broadcast(taskManager.CreateSnapshot(), helperState, Config.ToSharedSnapshot());
    }
}
