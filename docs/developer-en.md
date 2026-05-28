# pumasi Developer Guide

Quick links: [Home](../README.md) | User: [English](user-en.md) / [한국어](user-ko.md) | Developer: [English](developer-en.md) / [한국어](developer-ko.md)

This page is for developers who want to fork, modify, test, or extend `pumasi`.

Current mod version: `0.1.11`

## Repository Overview

```text
Pumasi.sln
src/
  Pumasi/                  SMAPI entrypoint and Stardew-specific integration
  Pumasi.Core/             Testable domain logic with no SMAPI dependency
tests/
  Pumasi.Core.Tests/       xUnit tests for core behavior
docs/
  user-en.md
  user-ko.md
  developer-en.md
  developer-ko.md
  superpowers/             Planning/spec history
```

The mod is intentionally split into two layers:

- `Pumasi.Core`: AI prompt shaping, command parsing, configuration models, wiki retrieval models, task queue logic, and pure formatting.
- `Pumasi`: SMAPI lifecycle hooks, console/chat command registration, Stardew Valley scanning/execution, multiplayer sync, overlay rendering, and config UI integration.

## Build And Test

Use the repository-local .NET command if available:

```bash
./.dotnet/dotnet build Pumasi.sln
./.dotnet/dotnet test Pumasi.sln
```

The build creates a SMAPI zip:

```text
src/Pumasi/bin/Debug/net6.0/Pumasi 0.1.11.zip
```

The `.dotnet/` directory is ignored by git, so a local SDK can be installed without committing it.

## Local Install For Testing

Extract the generated zip into the Stardew Valley `Mods` folder. The installed folder should be:

```text
Stardew Valley/
  Mods/
    Pumasi/
```

Typical Steam locations are:

```text
Windows: C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley\Mods
macOS:   ~/Library/Application Support/Steam/steamapps/common/Stardew Valley/Contents/MacOS/Mods
Linux:   ~/.local/share/Steam/steamapps/common/Stardew Valley/Mods
```

SMAPI prints the exact `Mods go here:` path at startup. Prefer that path when testing on a different install.

## Current Architecture

`ModEntry` is the SMAPI entrypoint. It wires:

- Game loop events for save load, day start, update ticks, and HUD rendering.
- SMAPI console commands.
- Stardew chat commands through `ChatCommands.Register`.
- Multiplayer message routing.
- The Pumasi quick settings tab in the ESC/game menu.
- Gemini planning and wiki-grounded answer flows.

`TaskManager` owns the todo queue, preserves explicit visible todo order, supports host-side active todo reordering, and deduplicates active work with task keys.

`FarmTaskScanner` scans currently supported locations for candidate work.

`FarmTaskExecutor` mutates the Stardew world for currently implemented safe tasks.

`MultiplayerSyncService` broadcasts host-owned state and routes guest commands back to the host.

`Pumasi.Core.Chat.HelperChatFormatter` formats helper answers for in-game chat.

## Command Flow

Console commands:

```text
pms_status
pms_scan
pms_ask <question or farm-work request>
pms_key <gemini-api-key>
pms_todo [move <from> <to>|up <index>|down <index>|top <index>|bottom <index>]
pms_work <category> on|off
```

Chat commands:

```text
/pms status
/pms scan
/pms todo
/pms todo move 3 1
/pms animals on
/pms work animals off
/pms ask <question or farm-work request>
/pms <question or farm-work request>
```

Command parsing is centralized in `Pumasi.Core.Commands.PumasiCommandParser`. Chat key input is rejected by design, so secrets are not accepted through in-game chat.

Todo board `^` / `v` button clicks are handled through the SMAPI input event and call the existing `TaskManager.MoveActiveTask` path. This is host-only and shares the same validation rules as command-based reorder operations.

Todo execution is top-to-bottom by visible queue order. Morning farm scans enqueue up to `Assistant.MorningTodoLimit` safe tasks by default, and user/AI-planned tasks append to the bottom unless the host reorders them.

## In-Game Menu UI

`PumasiSettingsPage` is a quick settings page appended to Stardew `GameMenu.pages`. Vanilla `GameMenu.getTabNumberFromName` only knows vanilla tab names, so Pumasi does not directly add a custom tab to `GameMenu.tabs`. Instead, it draws a `P` tab in `RenderedActiveMenu` and detects clicks through the SMAPI input event, then switches `currentTab` to the Pumasi page index.

The setting order is owned by `Pumasi.Core.Ui.PumasiSettingsCatalog`, and `PumasiSettingsCatalogTests` verifies the row order and labels.

## AI And Wiki Flow

`KnowledgeIntentClassifier` classifies `pms_ask` and `/pms` input:

- Task planning: `PlanWithGeminiAsync`.
- Wiki answer: `AnswerWithWikiAsync`.
- Ambiguous: `ContextualIntentRouter` sends recent conversation and current todos to Gemini, then routes to task planning, wiki answer, chat answer, or clarification.

Wiki answers use:

- `WikiClient` for MediaWiki API search/extract calls.
- `WikiMemoryCache` for in-memory query/page caching.
- `WikiContextBuilder` for context shaping.
- `GroundedAnswerPlanner` for Gemini prompt generation and JSON answer parsing.

The default wiki base URL is:

```text
https://ko.stardewvalleywiki.com
```

## Multiplayer Model

The host is authoritative:

- Guests do not call Gemini.
- Guests do not mutate the world.
- Guests send `GuestCommandMessage` to the host.
- The host broadcasts `TodoSnapshotMessage`, `HelperStateMessage`, `SharedConfigMessage`, and `HelperAnswerMessage`.
- Shared config snapshots intentionally exclude Gemini API keys.

This avoids duplicate task execution and keeps world-changing behavior in one process.

## Configuration And Secrets

The user-facing config lives in:

```text
Mods/Pumasi/config.json
```

`ConfigService.SetGeminiApiKey` writes the key through SMAPI's config API. The key is local and plain text, so code changes should continue to avoid logging it or sending it in multiplayer messages.

Existing tests cover redaction and shared config behavior in `ConfigRedactionTests`.

## Current MVP Boundaries

Implemented execution:

- Water dry crops.
- Harvest ready crops.
- Collect ready machines.
- Till plain ground around sprinklers.
- Refill hay in animal buildings.

Scanner scope:

- Farm.
- Greenhouse.
- Untilled plain ground around sprinklers.
- Hay refill candidates in loaded animal buildings when the `Animals` work category is enabled.

Not yet implemented:

- Full NPC schedule, friendship, gifts, events, or social behavior.
- Planting, selling, destroying, rare item movement, detailed animal care like petting/product collection, chest management.
- Durable persistent todo storage beyond current runtime snapshots.
- Permission UI for individual multiplayer guests.

When extending behavior, keep world mutation host-only and add core tests for non-SMAPI logic first.

## Forking And Development Workflow

1. Fork the repository.
2. Create a branch for the change.
3. Keep pure logic in `Pumasi.Core` when possible.
4. Add or update xUnit tests in `tests/Pumasi.Core.Tests`.
5. Build and test locally.
6. Install the generated zip into a local SMAPI `Mods` folder.
7. Test single-player behavior.
8. Test host/guest behavior for multiplayer-sensitive changes.

Recommended verification:

```bash
./.dotnet/dotnet test Pumasi.sln
./.dotnet/dotnet build Pumasi.sln
git diff --check
```

## Release Notes For Maintainers

When changing public behavior:

- Update `src/Pumasi/manifest.json` version.
- Update user and developer docs in both English and Korean if the behavior is user-visible.
- Rebuild the SMAPI zip.
- Confirm SMAPI loads the expected version.

SMAPI analyzer warnings about compiler/analyzer version mismatch may appear depending on the local SDK. Treat build errors as blockers; track analyzer warnings separately unless they indicate a real mod issue.
