using Microsoft.Xna.Framework;
using Pumasi.Core.Ai;
using Pumasi.Core.Chat;
using Pumasi.Core.Commands;
using Pumasi.Core.Configuration;
using Pumasi.Core.Knowledge;
using Pumasi.Core.Tasks;
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

    private ConfigService configService = null!;
    private TaskManager taskManager = null!;
    private HelperRuntimeState helperState = null!;
    private FarmTaskScanner scanner = null!;
    private FarmTaskExecutor executor = null!;
    private MultiplayerSyncService multiplayer = null!;
    private TodoOverlay overlay = null!;
    private HttpClient httpClient = null!;
    private WikiMemoryCache wikiCache = null!;
    private int executionCooldownTicks;
    private DateTimeOffset lastWikiQuestionAt = DateTimeOffset.MinValue;
    private bool chatCommandsRegistered;

    public override void Entry(IModHelper helper)
    {
        configService = new ConfigService(helper, Monitor, ModManifest);
        taskManager = new TaskManager();
        helperState = new HelperRuntimeState { Name = configService.Config.Assistant.Name };
        scanner = new FarmTaskScanner();
        executor = new FarmTaskExecutor(helperState);
        overlay = new TodoOverlay { Visible = configService.Config.Ui.ShowTodoOverlay };
        httpClient = new HttpClient();
        wikiCache = new WikiMemoryCache();
        multiplayer = new MultiplayerSyncService(helper, Monitor, ModManifest, HandleGuestCommand);

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.GameLoop.DayStarted += OnDayStarted;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        helper.Events.Display.RenderedHud += OnRenderedHud;
        helper.Events.Input.ButtonPressed += OnButtonPressed;

        helper.ConsoleCommands.Add("pms_status", "Show pumasi (품앗이) status.", OnStatusCommand);
        helper.ConsoleCommands.Add("pms_scan", "Host only: scan farm tasks and enqueue safe todos.", OnScanCommand);
        helper.ConsoleCommands.Add("pms_ask", "Ask pumasi to answer a wiki question or plan safe farm work. Usage: pms_ask <instruction>", OnAskCommand);
        helper.ConsoleCommands.Add("pms_key", "Host local only: set Gemini API key. Usage: pms_key <key>", OnApiKeyCommand);
        helper.ConsoleCommands.Add("pms_todo", "Show or reorder current todo list. Usage: pms_todo [move <from> <to>|up <index>|down <index>|top <index>|bottom <index>]", OnTodoCommand);
    }

    private ModConfig Config => configService.Config;

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
                _ => "/pms status | /pms scan | /pms todo [move/up/down] | /pms ask <질문/작업> | /pms <질문/작업>",
                new[] { "pms", "pms_ask", "pms_status", "pms_scan", "pms_todo", "pms_key" },
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
        helperState.Name = Config.Assistant.Name;
        helperState.Status = Context.IsMainPlayer ? "Host idle" : "Guest view";
        BroadcastState();
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        if (Context.IsMainPlayer && Config.Assistant.AutomationMode != AutomationMode.Off)
            EnqueueMorningTodos();
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
            Monitor.Log("Only the host should configure the Gemini API key.", LogLevel.Warn);
            return;
        }

        if (args.Length != 1 || string.IsNullOrWhiteSpace(args[0]))
        {
            Monitor.Log("Usage: pms_key <gemini-api-key>", LogLevel.Info);
            return;
        }

        configService.SetGeminiApiKey(args[0]);
        Monitor.Log("Gemini API key saved locally. It will not be synced to guests.", LogLevel.Info);
    }

    private void OnTodoCommand(string command, string[] args)
    {
        RunPumasiCommand(new PumasiCommand(PumasiCommandKind.Todo, string.Join(" ", args)), CommandSurface.Console);
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

            case PumasiCommandKind.Help:
                SendCommandFeedback("사용법: /pms status, /pms scan, /pms todo, /pms todo move 3 1, /pms ask <질문/작업>, /pms <질문/작업>", surface);
                break;

            case PumasiCommandKind.ApiKeyRejected:
                SendCommandFeedback("API KEY는 인게임 채팅에 입력하지 말고 SMAPI 콘솔의 pms_key <key> 또는 config.json에서 설정해줘요.", surface, LogLevel.Warn);
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
        SendCommandFeedback("호스트에게 pumasi 요청을 보냈어요.", surface);
    }

    private void ScanForTasks(CommandSurface surface)
    {
        if (!RequireHost(surface))
            return;

        var added = EnqueueScanResults();
        SendCommandFeedback($"Queued {added} scanned task(s).", surface);
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
                SendCommandFeedback("Todo reorder is host-only. Guests can view the synced todo list.", surface, LogLevel.Warn);
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
            SendCommandFeedback("Todo list is empty.", surface);
            return;
        }

        for (var i = 0; i < visibleItems.Length; i++)
        {
            var item = visibleItems[i];
            SendCommandFeedback($"#{i + 1} [{item.Status}] {item.Type} {item.Location}({item.X},{item.Y}) key={item.Key}", surface);
        }
    }

    private void HandleTodoReorder(string argument, int visibleCount, CommandSurface surface)
    {
        var parts = argument.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
            return;

        if (!TryResolveTodoMove(parts, visibleCount, out var from, out var to, out var error))
        {
                SendCommandFeedback($"Todo reorder usage: /pms todo move <from> <to>, /pms todo up <index>, /pms todo down <index>, /pms todo top <index>, /pms todo bottom <index>. {error}", surface, LogLevel.Warn);
            return;
        }

        var result = taskManager.MoveActiveTask(from, to);
        if (!result.Moved)
        {
            SendCommandFeedback($"Todo reorder failed: {result.Reason}", surface, LogLevel.Warn);
            return;
        }

        SendCommandFeedback(result.Reason == "no-change" ? "Todo order unchanged." : $"Moved todo #{from} to #{to}.", surface);
        BroadcastState();
    }

    private static bool TryResolveTodoMove(string[] parts, int visibleCount, out int from, out int to, out string error)
    {
        from = 0;
        to = 0;
        error = string.Empty;

        if (visibleCount == 0)
        {
            error = "There are no active todos.";
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
                error = "Unknown reorder command.";
                return false;
        }
    }

    private static bool TryParsePosition(string value, out int position)
    {
        return int.TryParse(value, out position) && position > 0;
    }

    private string BuildStatusMessage()
    {
        return $"pumasi (품앗이): host={Context.IsMainPlayer}, mode={Config.Assistant.AutomationMode}, geminiConfigured={Config.Gemini.IsConfigured}, todos={taskManager.Tasks.Count}";
    }

    private bool RequireHost(CommandSurface surface = CommandSurface.Console)
    {
        if (Context.IsMainPlayer)
            return true;

        SendCommandFeedback("This command is host-only. Guests send commands through /pms ask.", surface, LogLevel.Warn);
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
            ? $"Morning scan queued {added} todo(s)"
            : "Morning scan found no new todos";
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
            Monitor.Log("Gemini is not configured. Run pms_key <key> and check config.json for model/base URL settings.", LogLevel.Warn);
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

    private async Task HandleAskAsync(string instruction)
    {
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
                PublishHelperAnswer("작업으로 실행할지, 위키 정보로 답할지 조금 더 구체적으로 말해줘요.", Array.Empty<string>());
                break;
        }
    }

    private async Task AnswerWithWikiAsync(string question)
    {
        if (!RequireHost())
            return;

        if (!Config.WikiAnswers.WikiAnswersEnabled)
        {
            PublishHelperAnswer("위키 기반 답변 기능이 꺼져 있어요.", Array.Empty<string>());
            return;
        }

        var cooldown = TimeSpan.FromSeconds(Math.Max(0, Config.WikiAnswers.WikiQuestionCooldownSeconds));
        var now = DateTimeOffset.UtcNow;
        if (cooldown > TimeSpan.Zero && now - lastWikiQuestionAt < cooldown)
        {
            PublishHelperAnswer("위키 질문은 잠깐 쉬었다가 다시 물어봐 주세요.", Array.Empty<string>());
            return;
        }

        lastWikiQuestionAt = now;

        try
        {
            httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(5, Config.Gemini.TimeoutSeconds));
            var wikiClient = new WikiClient(httpClient, new WikiClientOptions(Config.WikiAnswers.WikiBaseUrl));
            IReadOnlyList<WikiSearchResult> searchResults;
            if (!wikiCache.TryGetSearch(question, out searchResults))
            {
                var search = await wikiClient.SearchAsync(question, Math.Max(1, Config.WikiAnswers.WikiMaxPages)).ConfigureAwait(false);
                if (!search.Success)
                {
                    PublishHelperAnswer("지금은 위키에 접속할 수 없어서 확인하지 못했어요.", Array.Empty<string>());
                    return;
                }

                searchResults = search.Value;
                wikiCache.SetSearch(question, searchResults);
            }

            if (searchResults.Count == 0)
            {
                PublishHelperAnswer("한국어 위키에서 관련 내용을 찾지 못했어요.", Array.Empty<string>());
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
                PublishHelperAnswer("위키 페이지는 찾았지만 Gemini가 설정되어 있지 않아 요약할 수 없어요.", FormatSources(context.Sources));
                return;
            }

            var gemini = new GeminiClient(httpClient, Config.Gemini);
            var planner = new GroundedAnswerPlanner(Config.Assistant);
            var modelText = await gemini.GenerateTextAsync(planner.BuildPrompt(question, context)).ConfigureAwait(false);
            var answer = GroundedAnswerPlanner.ParseAnswer(modelText);
            if (!answer.Success)
            {
                PublishHelperAnswer("위키 자료는 찾았지만 답변 요약을 만들지 못했어요. 출처를 확인해 주세요.", FormatSources(context.Sources));
                return;
            }

            PublishHelperAnswer(answer.Answer, FormatSources(answer.Sources.Count > 0 ? answer.Sources : context.Sources));
        }
        catch (Exception ex)
        {
            Monitor.Log($"Wiki grounded answer failed: {ex.Message}", LogLevel.Warn);
            PublishHelperAnswer("지금은 위키 기반 답변을 만들 수 없어요.", Array.Empty<string>());
        }
    }

    private void PublishHelperAnswer(string answer, IReadOnlyList<string> sources)
    {
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

    private void PostHelperAnswerToChat(string answer, IReadOnlyList<string> sources)
    {
        if (!Context.IsWorldReady)
            return;

        foreach (var line in HelperChatFormatter.FormatAnswer(helperState.Name, answer, sources))
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
