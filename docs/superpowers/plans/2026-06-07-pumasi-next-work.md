# pumasi v0.1.21 Execution Observability Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make Pumasi's todo board and `/pms todo` output clearly show what each task is doing, why it was queued, and why it completed, skipped, or failed.

**Architecture:** Keep the current host-authoritative model. Add all testable todo display and filtering logic to `Pumasi.Core`, then have SMAPI-facing code in `src/Pumasi` consume those helpers for the in-game board, chat commands, and multiplayer broadcasts. Do not change task dedupe, host-only execution, or guest world mutation behavior in this version.

**Tech Stack:** C#/.NET 6, SMAPI 4.x, Stardew Valley 1.6, xUnit, Gemini REST API, Korean/English Pumasi UI strings.

---

## Scope

This plan targets **v0.1.21** only.

Implement:

- Todo rows include status, type, location, tile, priority, source, and a readable reason.
- Completed, skipped, and failed tasks remain visible briefly in the board/list so the player can see the result.
- The todo board icon badge counts only active tasks.
- `/pms todo` and the board use the same localized row wording.
- The host broadcasts one state update when a task starts and one when it finishes.
- SMAPI debug logs show task start/result diagnostics.

Do not implement in v0.1.21:

- New animal task behavior.
- New planting/chest behavior.
- A visible walking NPC sprite.
- Any API key or prompt redesign.

## Current State Notes

- `TodoItemSnapshot` currently exposes `Reason` and `StatusReason`, but not `Source`.
- `HelperTask` already stores `Source`, so snapshot expansion is a small Core change.
- `TodoOverlay.Draw` currently filters only active rows inline and formats row text inline.
- `ModEntry.ShowTodoList` currently has a second, different row format.
- `TaskManager.MoveActiveTask` already uses active tasks only; keep that behavior.
- Local untracked sprite drafts exist under `src/Pumasi/assets/pumasi-bot-joseon-farmer-*`. Do not stage or delete them during this work.

## File Structure

- `src/Pumasi.Core/Tasks/TodoSnapshot.cs`
  - Add `Source` to `TodoItemSnapshot`.
- `src/Pumasi.Core/Ui/TodoDisplayFormatter.cs`
  - New pure formatter for board and `/pms todo` rows.
- `src/Pumasi.Core/Ui/TodoDisplayFilter.cs`
  - New pure selector for active rows plus recent finished rows.
- `src/Pumasi/UI/TodoOverlay.cs`
  - Consume Core filter/formatter, keep badge active-only, keep reorder controls active-only.
- `src/Pumasi/ModEntry.cs`
  - Consume Core filter/formatter in `/pms todo`.
  - Broadcast state at task start and task finish.
  - Add debug logs around execution.
- `tests/Pumasi.Core.Tests/Tasks/TaskManagerTests.cs`
  - Add snapshot source coverage.
- `tests/Pumasi.Core.Tests/Ui/TodoDisplayFormatterTests.cs`
  - New formatter tests.
- `tests/Pumasi.Core.Tests/Ui/TodoDisplayFilterTests.cs`
  - New filter tests.
- Version/docs files after implementation:
  - `src/Pumasi/manifest.json`
  - `README.md`
  - `docs/user-en.md`
  - `docs/user-ko.md`
  - `docs/developer-en.md`
  - `docs/developer-ko.md`

## Acceptance Criteria

- Opening the Pumasi board shows rows like:

```text
#1 [Queued] Harvest crop Greenhouse(10,8) P90 source=scan - Ready crop
```

- Korean UI shows localized status/type/result reason, while source remains a compact technical label:

```text
#1 [건너뜀] 작물 물주기 Farm(4,5) P50 source=scan - 작물에 이미 물이 있어요
```

- `/pms todo` prints the same row format as the board.
- Finished rows appear after active rows until the board capacity is filled.
- The icon badge does not count completed/skipped/failed rows.
- Host todo reorder continues to operate only on active task positions.
- A guest receives synced state after task start and after task finish.
- Tests pass with `./.dotnet/dotnet test Pumasi.sln --no-restore`.

## Task 1: Add Source To Todo Snapshots

**Files:**
- Modify: `src/Pumasi.Core/Tasks/TodoSnapshot.cs`
- Modify: `tests/Pumasi.Core.Tests/Tasks/TaskManagerTests.cs`

- [ ] **Step 1: Write the failing test**

Append this test to `tests/Pumasi.Core.Tests/Tasks/TaskManagerTests.cs`.

```csharp
[Fact]
public void CreateSnapshot_IncludesTaskSource()
{
    var manager = new TaskManager(new FixedClock());
    manager.Enqueue(new TaskProposal(
        TaskType.WaterCrop,
        new TaskTarget("Farm", 10, 10),
        50,
        "dry crop",
        "scan"));

    var snapshot = manager.CreateSnapshot();

    var item = Assert.Single(snapshot.Items);
    Assert.Equal("scan", item.Source);
}
```

- [ ] **Step 2: Run tests and verify the failure**

Run:

```bash
./.dotnet/dotnet test tests/Pumasi.Core.Tests/Pumasi.Core.Tests.csproj --filter CreateSnapshot_IncludesTaskSource --no-restore
```

Expected: compile failure because `TodoItemSnapshot.Source` does not exist.

- [ ] **Step 3: Add `Source` to `TodoItemSnapshot`**

Change `src/Pumasi.Core/Tasks/TodoSnapshot.cs` to this shape.

```csharp
namespace Pumasi.Core.Tasks;

public sealed record TodoSnapshot(IReadOnlyList<TodoItemSnapshot> Items);

public sealed record TodoItemSnapshot(
    Guid Id,
    string Key,
    TaskType Type,
    HelperTaskStatus Status,
    string Location,
    int X,
    int Y,
    int Priority,
    string Reason,
    string Source,
    string? StatusReason)
{
    public static TodoItemSnapshot FromTask(HelperTask task)
    {
        return new TodoItemSnapshot(
            task.Id,
            task.Key,
            task.Type,
            task.Status,
            task.Target.Location,
            task.Target.X,
            task.Target.Y,
            task.Priority,
            task.Reason,
            task.Source,
            task.StatusReason);
    }
}
```

- [ ] **Step 4: Verify**

Run:

```bash
./.dotnet/dotnet test tests/Pumasi.Core.Tests/Pumasi.Core.Tests.csproj --filter CreateSnapshot_IncludesTaskSource --no-restore
```

Expected: the test passes.

## Task 2: Add Testable Todo Row Formatting

**Files:**
- Create: `src/Pumasi.Core/Ui/TodoDisplayFormatter.cs`
- Create: `tests/Pumasi.Core.Tests/Ui/TodoDisplayFormatterTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Pumasi.Core.Tests/Ui/TodoDisplayFormatterTests.cs`.

```csharp
using Pumasi.Core.Configuration;
using Pumasi.Core.Tasks;
using Pumasi.Core.Ui;
using Xunit;

namespace Pumasi.Core.Tests.Ui;

public sealed class TodoDisplayFormatterTests
{
    [Fact]
    public void FormatRow_IncludesActiveTaskDetails()
    {
        var item = new TodoItemSnapshot(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            "HarvestCrop:Greenhouse:10,8",
            TaskType.HarvestCrop,
            HelperTaskStatus.Queued,
            "Greenhouse",
            10,
            8,
            90,
            "Ready crop",
            "scan",
            null);

        var row = TodoDisplayFormatter.FormatRow(UiLanguage.English, 1, item);

        Assert.Equal("#1 [Queued] Harvest crop Greenhouse(10,8) P90 source=scan - Ready crop", row);
    }

    [Fact]
    public void FormatRow_LocalizesFinalStatusReason()
    {
        var item = new TodoItemSnapshot(
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            "WaterCrop:Farm:4,5",
            TaskType.WaterCrop,
            HelperTaskStatus.Skipped,
            "Farm",
            4,
            5,
            50,
            "Dry crop",
            "scan",
            "crop-already-watered");

        var row = TodoDisplayFormatter.FormatRow(UiLanguage.Korean, 2, item);

        Assert.Equal("#2 [건너뜀] 작물 물주기 Farm(4,5) P50 source=scan - 작물에 이미 물이 있어요", row);
    }
}
```

- [ ] **Step 2: Run tests and verify the failure**

Run:

```bash
./.dotnet/dotnet test tests/Pumasi.Core.Tests/Pumasi.Core.Tests.csproj --filter TodoDisplayFormatterTests --no-restore
```

Expected: compile failure because `TodoDisplayFormatter` does not exist.

- [ ] **Step 3: Implement the formatter**

Create `src/Pumasi.Core/Ui/TodoDisplayFormatter.cs`.

```csharp
using Pumasi.Core.Configuration;
using Pumasi.Core.Tasks;

namespace Pumasi.Core.Ui;

public static class TodoDisplayFormatter
{
    public static string FormatRow(UiLanguage language, int position, TodoItemSnapshot item)
    {
        var status = PumasiText.GetTaskStatus(language, item.Status);
        var type = PumasiText.GetTaskType(language, item.Type);
        var reason = string.IsNullOrWhiteSpace(item.StatusReason)
            ? item.Reason
            : PumasiText.GetExecutionReason(language, item.StatusReason);
        var source = string.IsNullOrWhiteSpace(item.Source)
            ? "source=unknown"
            : $"source={item.Source}";

        return $"#{position} [{status}] {type} {item.Location}({item.X},{item.Y}) P{item.Priority} {source} - {reason}";
    }
}
```

- [ ] **Step 4: Verify**

Run:

```bash
./.dotnet/dotnet test tests/Pumasi.Core.Tests/Pumasi.Core.Tests.csproj --filter TodoDisplayFormatterTests --no-restore
```

Expected: both formatter tests pass.

## Task 3: Keep Finished Todo Rows Visible Briefly

**Files:**
- Create: `src/Pumasi.Core/Ui/TodoDisplayFilter.cs`
- Create: `tests/Pumasi.Core.Tests/Ui/TodoDisplayFilterTests.cs`

- [ ] **Step 1: Write the failing tests**

Create `tests/Pumasi.Core.Tests/Ui/TodoDisplayFilterTests.cs`.

```csharp
using Pumasi.Core.Tasks;
using Pumasi.Core.Ui;
using Xunit;

namespace Pumasi.Core.Tests.Ui;

public sealed class TodoDisplayFilterTests
{
    [Fact]
    public void SelectVisibleItems_ShowsActiveFirstThenNewestFinished()
    {
        var oldCompleted = Item("old", HelperTaskStatus.Completed);
        var queued = Item("queued", HelperTaskStatus.Queued);
        var failed = Item("failed", HelperTaskStatus.Failed);
        var skipped = Item("skipped", HelperTaskStatus.Skipped);

        var selected = TodoDisplayFilter.SelectVisibleItems(
            new[] { oldCompleted, queued, failed, skipped },
            capacity: 3);

        Assert.Collection(
            selected,
            item => Assert.Equal("queued", item.Key),
            item => Assert.Equal("skipped", item.Key),
            item => Assert.Equal("failed", item.Key));
    }

    [Fact]
    public void SelectVisibleItems_TruncatesActiveItemsBeforeFinishedItems()
    {
        var selected = TodoDisplayFilter.SelectVisibleItems(
            new[]
            {
                Item("active-1", HelperTaskStatus.Queued),
                Item("active-2", HelperTaskStatus.Claimed),
                Item("active-3", HelperTaskStatus.InProgress),
                Item("done", HelperTaskStatus.Completed)
            },
            capacity: 2);

        Assert.Collection(
            selected,
            item => Assert.Equal("active-1", item.Key),
            item => Assert.Equal("active-2", item.Key));
    }

    [Fact]
    public void CountActive_IgnoresFinishedItems()
    {
        var activeCount = TodoDisplayFilter.CountActive(new[]
        {
            Item("queued", HelperTaskStatus.Queued),
            Item("done", HelperTaskStatus.Completed),
            Item("failed", HelperTaskStatus.Failed)
        });

        Assert.Equal(1, activeCount);
    }

    private static TodoItemSnapshot Item(string key, HelperTaskStatus status)
    {
        return new TodoItemSnapshot(
            Guid.NewGuid(),
            key,
            TaskType.WaterCrop,
            status,
            "Farm",
            10,
            10,
            50,
            "Dry crop",
            "scan",
            status is HelperTaskStatus.Completed or HelperTaskStatus.Skipped or HelperTaskStatus.Failed
                ? "crop-already-watered"
                : null);
    }
}
```

- [ ] **Step 2: Run tests and verify the failure**

Run:

```bash
./.dotnet/dotnet test tests/Pumasi.Core.Tests/Pumasi.Core.Tests.csproj --filter TodoDisplayFilterTests --no-restore
```

Expected: compile failure because `TodoDisplayFilter` does not exist.

- [ ] **Step 3: Implement the filter**

Create `src/Pumasi.Core/Ui/TodoDisplayFilter.cs`.

```csharp
using Pumasi.Core.Tasks;

namespace Pumasi.Core.Ui;

public static class TodoDisplayFilter
{
    private static readonly HelperTaskStatus[] ActiveStatuses =
    {
        HelperTaskStatus.Queued,
        HelperTaskStatus.Claimed,
        HelperTaskStatus.InProgress
    };

    private static readonly HelperTaskStatus[] FinishedStatuses =
    {
        HelperTaskStatus.Completed,
        HelperTaskStatus.Skipped,
        HelperTaskStatus.Failed
    };

    public static int CountActive(IReadOnlyList<TodoItemSnapshot> items)
    {
        return items.Count(IsActive);
    }

    public static bool IsActive(TodoItemSnapshot item)
    {
        return ActiveStatuses.Contains(item.Status);
    }

    public static IReadOnlyList<TodoItemSnapshot> SelectVisibleItems(IReadOnlyList<TodoItemSnapshot> items, int capacity)
    {
        if (capacity <= 0)
            return Array.Empty<TodoItemSnapshot>();

        var active = items
            .Where(IsActive)
            .ToArray();

        var finished = items
            .Where(item => FinishedStatuses.Contains(item.Status))
            .Reverse()
            .ToArray();

        return active.Concat(finished).Take(capacity).ToArray();
    }
}
```

- [ ] **Step 4: Verify**

Run:

```bash
./.dotnet/dotnet test tests/Pumasi.Core.Tests/Pumasi.Core.Tests.csproj --filter TodoDisplayFilterTests --no-restore
```

Expected: all filter tests pass.

## Task 4: Apply Formatter And Filter To Board And Commands

**Files:**
- Modify: `src/Pumasi/UI/TodoOverlay.cs`
- Modify: `src/Pumasi/ModEntry.cs`

- [ ] **Step 1: Update board row selection**

In `src/Pumasi/UI/TodoOverlay.cs`, replace the inline active-only filtering with:

```csharp
var activeCount = TodoDisplayFilter.CountActive(snapshot.Items);
var visibleItems = TodoDisplayFilter.SelectVisibleItems(snapshot.Items, maxTodoRows).ToArray();
DrawIcon(spriteBatch, iconBounds, activeCount, Expanded);
```

- [ ] **Step 2: Keep reorder controls active-only**

Still in `TodoOverlay.Draw`, use `activeCount` for reorder controls.

```csharp
var panel = TodoOverlayLayout.CreatePopup(visibleItems.Length, lineHeight, Game1.uiViewport.Width, Game1.uiViewport.Height, iconBounds);
var position = new Vector2(panel.TextX, panel.TextY);
var canReorder = Context.IsMainPlayer && activeCount > 1;
if (canReorder)
    reorderControls = TodoOverlayLayout.CreateReorderControls(panel, activeCount, lineHeight);
var textWidth = GetTodoTextWidth(panel, reorderControls);
```

- [ ] **Step 3: Use the formatter for board rows**

Replace the inline status/type string in the board loop with:

```csharp
var text = TodoDisplayFormatter.FormatRow(Language, i + 1, item);
DrawShadowedText(spriteBatch, TrimToWidth(text, textWidth), position, BodyText);
if (i < activeCount)
    DrawReorderControls(spriteBatch, reorderControls.Where(control => control.FromPosition == i + 1));
position.Y += lineHeight;
```

- [ ] **Step 4: Update `/pms todo` list selection**

In `src/Pumasi/ModEntry.cs`, change `ShowTodoList` so reorder uses active count, but display uses the visible filter.

```csharp
var snapshot = Context.IsMainPlayer ? taskManager.CreateSnapshot() : multiplayer.LatestSnapshot;
var activeCount = TodoDisplayFilter.CountActive(snapshot.Items);
var visibleItems = TodoDisplayFilter.SelectVisibleItems(snapshot.Items, 12).ToArray();

if (!string.IsNullOrWhiteSpace(argument))
{
    if (!Context.IsMainPlayer)
    {
        SendCommandFeedback(T(PumasiTextKey.TodoReorderHostOnly), surface, LogLevel.Warn);
        return;
    }

    HandleTodoReorder(argument, activeCount, surface);
    snapshot = taskManager.CreateSnapshot();
    activeCount = TodoDisplayFilter.CountActive(snapshot.Items);
    visibleItems = TodoDisplayFilter.SelectVisibleItems(snapshot.Items, 12).ToArray();
}
```

- [ ] **Step 5: Use the formatter for `/pms todo` rows**

Replace the existing row loop with:

```csharp
for (var i = 0; i < visibleItems.Length; i++)
{
    SendCommandFeedback(TodoDisplayFormatter.FormatRow(Language, i + 1, visibleItems[i]), surface);
}
```

- [ ] **Step 6: Verify Core tests**

Run:

```bash
./.dotnet/dotnet test tests/Pumasi.Core.Tests/Pumasi.Core.Tests.csproj --no-restore
```

Expected: all Core tests pass.

## Task 5: Broadcast Task Start And Finish State

**Files:**
- Modify: `src/Pumasi/ModEntry.cs`

- [ ] **Step 1: Add a start broadcast**

In `ProcessNextTask`, immediately after `taskManager.Start(claimed.Id)`, update helper state and broadcast.

```csharp
taskManager.Start(claimed.Id);
helperState.Status = PumasiText.GetTaskType(Language, claimed.Type);
helperState.CurrentTaskKey = claimed.Key;
Monitor.Log($"Executing todo {claimed.Key} ({claimed.Type}) at {claimed.Target.Location} {claimed.Target.X},{claimed.Target.Y}", LogLevel.Debug);
BroadcastState();

var result = executor.Execute(claimed);
```

- [ ] **Step 2: Keep a finish broadcast with diagnostics**

After complete/skip/fail, log the result and broadcast the final state.

```csharp
Monitor.Log($"Todo {claimed.Key} finished: completed={result.Completed}, skipped={result.Skipped}, reason={result.Reason}", LogLevel.Debug);
helperState.Status = PumasiText.GetExecutionReason(Language, result.Reason);
helperState.CurrentTaskKey = null;
BroadcastState();
```

- [ ] **Step 3: Verify build**

Run:

```bash
./.dotnet/dotnet build Pumasi.sln --no-restore
```

Expected: build succeeds. Existing SMAPI analyzer warnings are acceptable only if exit code is `0`.

## Task 6: Version, Docs, And Package

**Files:**
- Modify: `src/Pumasi/manifest.json`
- Modify: `README.md`
- Modify: `docs/user-en.md`
- Modify: `docs/user-ko.md`
- Modify: `docs/developer-en.md`
- Modify: `docs/developer-ko.md`

- [ ] **Step 1: Bump manifest version**

In `src/Pumasi/manifest.json`, change:

```json
"Version": "0.1.21"
```

- [ ] **Step 2: Update docs**

Update user/developer docs to mention:

- v0.1.21 todo rows now include status, priority, source, target tile, and readable result reason.
- `/pms todo` and the board use the same wording.
- Completed/skipped/failed tasks may stay visible briefly for observability.
- Reordering is still host-only and active-task-only.

- [ ] **Step 3: Run full verification**

Run:

```bash
./.dotnet/dotnet test Pumasi.sln --no-restore
./.dotnet/dotnet build Pumasi.sln --no-restore
git diff --check
```

Expected:

- Tests pass.
- Build exits `0`.
- `git diff --check` reports no whitespace errors.

- [ ] **Step 4: Confirm package path**

After build, confirm the zip exists:

```bash
ls -la "src/Pumasi/bin/Debug/net6.0/Pumasi 0.1.21.zip"
```

Expected: the zip file exists.

## Task 7: Local Install, Commit, Push, Release

**Files:**
- No new source files beyond previous tasks.
- Do not stage untracked sprite drafts unless the user explicitly asks to include them.

- [ ] **Step 1: Install locally for manual SMAPI test**

This writes outside the repo and requires approval in Codex.

```bash
ditto -x -k "src/Pumasi/bin/Debug/net6.0/Pumasi 0.1.21.zip" "/Users/p3ckolim/Library/Application Support/Steam/steamapps/common/Stardew Valley/Contents/MacOS/Mods"
```

Verify installed manifest:

```bash
/usr/bin/plutil -extract Version raw -o - "/Users/p3ckolim/Library/Application Support/Steam/steamapps/common/Stardew Valley/Contents/MacOS/Mods/Pumasi/manifest.json"
```

Expected:

```text
0.1.21
```

- [ ] **Step 2: Manual game verification**

In SMAPI console or in-game chat:

```text
pms_scan
/pms todo
```

Expected:

- Board icon badge shows only active task count.
- Board popup rows include `P<priority>`, `source=...`, and readable reason.
- `/pms todo` output matches board wording.
- After one task runs, the result row remains visible briefly with completed/skipped/failed reason.

- [ ] **Step 3: Stage only intended files**

Run `git status --short` and stage only v0.1.21 files. Do not stage:

```text
src/Pumasi/assets/pumasi-bot-joseon-farmer-frames/
src/Pumasi/assets/pumasi-bot-joseon-farmer-spritesheet-64.png
src/Pumasi/assets/pumasi-bot-joseon-farmer-spritesheet-keyed.png
src/Pumasi/assets/pumasi-bot-joseon-farmer-spritesheet-source.png
src/Pumasi/assets/pumasi-bot-joseon-farmer-spritesheet.json
src/Pumasi/assets/pumasi-bot-joseon-farmer-spritesheet.png
```

- [ ] **Step 4: Commit**

Suggested commit:

```bash
git commit -m "Improve todo execution observability"
```

- [ ] **Step 5: Push and create release**

Push:

```bash
git push origin main
```

Create GitHub release:

```bash
gh release create v0.1.21 "src/Pumasi/bin/Debug/net6.0/Pumasi 0.1.21.zip" \
  --repo p3ckolim-dev/stv_pumasi \
  --target main \
  --title "pumasi 0.1.21" \
  --notes "Improves todo observability: board and /pms todo now show status, priority, source, target, and readable completion/skip/failure reasons."
```

Expected:

- `main` contains the v0.1.21 commit.
- Release `v0.1.21` has the built mod zip attached.

## Manual QA Checklist

- [ ] Host opens the Pumasi icon board and sees non-overlapping rows.
- [ ] Host runs `/pms todo` and sees the same row wording.
- [ ] Host moves an active queued task up/down; finished rows are not used as reorder targets.
- [ ] Guest opens `/pms todo`; guest sees synced host todo state.
- [ ] Guest attempts reorder; guest receives host-only warning.
- [ ] One task starts; debug log contains `Executing todo`.
- [ ] One task finishes; debug log contains `finished: completed=..., skipped=..., reason=...`.
- [ ] API key is never printed in logs, chat, or docs.

## Follow-Up Roadmap After v0.1.21

- **v0.1.22:** Add safe animal work: petting and animal-product candidates, behind the existing animal work category toggle.
- **v0.1.23:** Improve hay/machine scanner diagnostics and reduce false positives.
- **v0.2.0:** Add a single synchronized Pumasi NPC-like visual presence near the current task target.
- **v0.2.x:** Add planting/chest work only with confirmation because those mutate inventory and resource choices.

## Self-Review

- Spec coverage: all v0.1.21 acceptance criteria map to Tasks 1 through 7.
- Placeholder scan: no task relies on unfinished markers or unspecified tests.
- Type consistency: `TodoItemSnapshot.Source`, `TodoDisplayFormatter.FormatRow`, and `TodoDisplayFilter.SelectVisibleItems` are defined before later tasks consume them.
- Risk control: host-only execution and active-only reorder are explicitly preserved.
