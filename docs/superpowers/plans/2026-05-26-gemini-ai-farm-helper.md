# Gemini AI Farm Helper Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the first playable SMAPI mod foundation for a host-authoritative Stardew Valley helper NPC with shared todos, safe task execution, and Gemini API settings.

**Architecture:** Split gameplay-independent logic into a testable core library, then keep SMAPI-specific code in the mod project. The host owns AI calls, task validation, task execution, and multiplayer snapshots; guests render state and submit commands only.

**Tech Stack:** C#/.NET 6, SMAPI, Stardew Valley, xUnit tests, Gemini REST `generateContent` API with `x-goog-api-key`.

---

## File Structure

- `StardewAIFarmHelper.sln`: solution grouping the mod, core library, and tests.
- `src/StardewAIFarmHelper.Core/`: testable domain logic with no SMAPI dependency.
- `src/StardewAIFarmHelper/`: SMAPI mod entry, config UI, scanner, executor, multiplayer sync, and helper rendering hooks.
- `tests/StardewAIFarmHelper.Core.Tests/`: unit tests for task keys, dedupe, config redaction, Gemini requests, and AI proposal parsing.
- `docs/superpowers/specs/2026-05-26-ai-farm-helper-design.md`: design spec, updated for Gemini first.

## Tasks

### Task 1: Project Scaffold

**Files:**
- Create: `StardewAIFarmHelper.sln`
- Create: `src/StardewAIFarmHelper.Core/StardewAIFarmHelper.Core.csproj`
- Create: `src/StardewAIFarmHelper/StardewAIFarmHelper.csproj`
- Create: `src/StardewAIFarmHelper/manifest.json`
- Create: `tests/StardewAIFarmHelper.Core.Tests/StardewAIFarmHelper.Core.Tests.csproj`

- [x] Add .NET 6 project files and package references.
- [x] Add SMAPI manifest with `EntryDll` set to `StardewAIFarmHelper.dll`.
- [x] Reference `Pathoschild.Stardew.ModBuildConfig` from the mod project.

### Task 2: Core Task Domain

**Files:**
- Create: `src/StardewAIFarmHelper.Core/Tasks/*.cs`
- Create: `tests/StardewAIFarmHelper.Core.Tests/Tasks/*.cs`

- [x] Write tests for stable task keys.
- [x] Write tests for queue deduplication.
- [x] Implement task types, statuses, target coordinates, task proposals, helper tasks, and task manager.
- [x] Verify queued/claimed/in-progress tasks block duplicates and completed/skipped/failed tasks do not.

### Task 3: Config And Secret Redaction

**Files:**
- Create: `src/StardewAIFarmHelper.Core/Configuration/*.cs`
- Create: `tests/StardewAIFarmHelper.Core.Tests/Configuration/*.cs`

- [x] Write tests proving Gemini API key is present in local config but removed from sync config.
- [x] Implement automation mode, work category toggles, Gemini provider config, and shared config snapshot.
- [x] Ensure the API key never appears in `ToString()` output.

### Task 4: Gemini Planner

**Files:**
- Create: `src/StardewAIFarmHelper.Core/Ai/*.cs`
- Create: `tests/StardewAIFarmHelper.Core.Tests/Ai/*.cs`

- [x] Write tests for Gemini request URL, `x-goog-api-key` header, and JSON request body.
- [x] Write tests for extracting task JSON from Gemini text responses.
- [x] Implement Gemini REST client and AI planner adapter.
- [x] Return validation errors instead of throwing for malformed AI output.

### Task 5: SMAPI Mod Shell

**Files:**
- Create: `src/StardewAIFarmHelper/ModEntry.cs`
- Create: `src/StardewAIFarmHelper/Integrations/GenericModConfigMenuApi.cs`
- Create: `src/StardewAIFarmHelper/Services/*.cs`

- [x] Read/write local config.
- [x] Register console commands for status, scan, todo list, and Gemini planning.
- [x] Add optional Generic Mod Config Menu integration for ordinary settings.
- [x] Keep API key entry available through local config and console command in the first build.

### Task 6: Host Task Scan And Execution

**Files:**
- Create: `src/StardewAIFarmHelper/Game/*.cs`
- Create: `src/StardewAIFarmHelper/Tasks/*.cs`

- [x] Scan host farm/greenhouse for harvestable crops, dry crops, and ready machines.
- [x] Convert scan results into validated task proposals.
- [x] Execute only one claimed task at a time on the host.
- [x] Revalidate each target before mutation.

### Task 7: Multiplayer Sync And Guest Commands

**Files:**
- Create: `src/StardewAIFarmHelper/Multiplayer/*.cs`

- [x] Broadcast todo snapshots and helper status from host to guests.
- [x] Route guest commands to the host.
- [x] Ignore guest-side world mutation requests.
- [x] Exclude API key and private provider config from every sync message.

### Task 8: Helper Visual And Todo UI Foundation

**Files:**
- Create: `src/StardewAIFarmHelper/UI/*.cs`
- Create: `src/StardewAIFarmHelper/assets/helper.json`

- [x] Render a compact todo overlay.
- [x] Track helper status, current task, and approximate tile position.
- [x] Draw status text when a sprite is not loaded.
- [x] Allow hotkey toggling for the todo overlay.

### Task 9: Verification And Documentation

**Files:**
- Create: `README.md`
- Modify: `docs/superpowers/specs/2026-05-26-ai-farm-helper-design.md`

- [x] Document Gemini API key setup and local-only secret behavior.
- [x] Document install/build requirements, including .NET 6 and SMAPI.
- [x] Run `dotnet test` and `dotnet build` where SDK is available.
- [x] Record local verification outcome in the final response.
