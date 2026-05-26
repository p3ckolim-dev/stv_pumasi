# pumasi

pumasi (`pms`) is a SMAPI mod prototype for Stardew Valley multiplayer. Its Korean name is "품앗이". It adds a host-authoritative helper NPC concept with a shared todo list, safe task queue, duplicate-task protection, and a Gemini API planning layer.

## Current Scope

- Host owns all world-changing behavior.
- Guests can view synced todos and send helper commands.
- Tasks are deduplicated with stable task keys.
- The helper executes one task at a time.
- Gemini planning uses the host's local API key only.
- The first safe task handlers cover crop watering, crop harvesting, and ready machine collection.

## Requirements

- Stardew Valley with SMAPI 4.x.
- .NET 6 SDK to build locally.
- Optional: Generic Mod Config Menu for ordinary settings.
- Gemini API key for AI planning.

## Gemini API Key

The key is stored only in the host's local SMAPI config. It is not included in multiplayer messages, todo snapshots, shared config snapshots, or logs.

In-game, install Generic Mod Config Menu and open pumasi's Gemini section. The API key field displays `********` after a key is saved; entering a new non-empty value replaces the stored key.

SMAPI console fallback:

```text
pms_key YOUR_GEMINI_API_KEY
```

Config fields:

```json
{
  "Gemini": {
    "BaseUrl": "https://generativelanguage.googleapis.com/v1beta",
    "Model": "gemini-2.5-flash",
    "ApiKey": ""
  }
}
```

## Commands

```text
pms_status
pms_scan
pms_ask <instruction>
pms_key <key>
pms_todo
```

Guests can run `pms_ask`; the command is routed to the host. Only the host calls Gemini and mutates game state.

`pms_ask` now separates farm-work requests from information questions:

- `pms_ask 온실 수확해줘` uses the task planner.
- `pms_ask 딸기 씨앗은 어디서 사?` searches the Korean Stardew Valley Wiki, then asks Gemini to answer only from that wiki context.
- Ambiguous requests ask for clarification and do not enqueue tasks.

Wiki-grounded answers use `https://ko.stardewvalleywiki.com` by default. The host performs wiki and Gemini calls, then broadcasts only the final public answer and source metadata to guests.

## Build And Test

This repository includes a local `.dotnet/` install path in `.gitignore`, so the SDK can be installed locally without committing it.

```bash
./.dotnet/dotnet build Pumasi.sln
./.dotnet/dotnet test Pumasi.sln
```

The SMAPI build package emits the installable mod zip under:

```text
src/Pumasi/bin/Debug/net6.0/
```
