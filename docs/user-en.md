# pumasi User Guide

Quick links: [Home](../README.md) | User: [English](user-en.md) / [한국어](user-ko.md) | Developer: [English](developer-en.md) / [한국어](developer-ko.md)

`pumasi` (`pms`, Korean name: `품앗이`) is a SMAPI mod prototype for Stardew Valley. It adds a helper that can answer questions, maintain a todo list, and perform a limited set of safe repetitive farm chores.

Current mod version: `0.1.4`

## Who This Page Is For

Use this page if you want to install and play with the mod. If you want to fork, modify, or extend the code, use the [Developer Guide](developer-en.md).

## Requirements

- Stardew Valley 1.6.
- SMAPI 4.x.
- The `pumasi` mod installed in the Stardew Valley `Mods` folder.
- Gemini API key if you want AI planning or wiki answer summaries.
- Optional: Generic Mod Config Menu for in-game settings.

## Install

Download or build the mod zip, then extract it into the Stardew Valley `Mods` folder so the final folder looks like this:

```text
Stardew Valley/
  Mods/
    Pumasi/
      manifest.json
      Pumasi.dll
      Pumasi.Core.dll
      assets/
```

Typical Steam locations are:

```text
Windows: C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley\Mods
macOS:   ~/Library/Application Support/Steam/steamapps/common/Stardew Valley/Contents/MacOS/Mods
Linux:   ~/.local/share/Steam/steamapps/common/Stardew Valley/Mods
```

SMAPI also prints the exact `Mods go here:` path when it starts. Use that path if it differs from the examples above.

After installation, start the game through SMAPI. The SMAPI console should list `pumasi 0.1.4` among loaded mods.

## Gemini API Key

Only the single-player user or multiplayer host needs a Gemini API key.

Set it through the SMAPI console:

```text
pms_key YOUR_GEMINI_API_KEY
```

The key is saved to the local SMAPI config:

```text
Mods/Pumasi/config.json
```

Security details:

- The key is stored locally in plain text inside `config.json`.
- The key is not sent to multiplayer guests.
- The key is not included in todo snapshots, shared config snapshots, helper answers, or logs.
- `/pms_key` is intentionally rejected in in-game chat to reduce accidental key leaks.
- If Generic Mod Config Menu is installed, the key field displays `********` after a key is saved.

## Commands

SMAPI console commands:

```text
pms_status
pms_scan
pms_ask <question or farm-work request>
pms_key <gemini-api-key>
pms_todo [move <from> <to>|up <index>|down <index>|top <index>|bottom <index>]
```

In-game chat commands:

```text
/pms status
/pms scan
/pms todo
/pms todo move 3 1
/pms ask <question or farm-work request>
/pms <question or farm-work request>
```

Chat aliases:

```text
/pms_ask
/pms_status
/pms_scan
/pms_todo
```

## Single-Player Use

Single-player is supported. The local player is the main player, so commands run locally and safe task execution can change the current save.

Suggested smoke test:

```text
pms_status
pms_key YOUR_GEMINI_API_KEY
pms_ask 딸기 씨앗은 어디서 사?
pms_ask 온실 수확해줘
pms_scan
pms_todo
```

## Multiplayer Use

The host is authoritative:

- The host stores the Gemini API key.
- The host calls Gemini and the Korean Stardew Valley Wiki.
- The host owns the todo queue.
- The host executes world-changing tasks.
- The host broadcasts helper state, todos, and public answer text to guests.

Guests can ask questions or request work:

```text
/pms 딸기 씨앗은 어디서 사?
/pms ask 온실 수확해줘
```

Guest requests are sent to the host. Guests do not need a Gemini API key. The final answer is shown in the in-game chat for players who have the mod installed.

## AI And Wiki Answers

`pms_ask` and `/pms` classify input into one of three paths:

- Farm-work requests use Gemini for task planning.
- Stardew Valley information questions search the Korean Stardew Valley Wiki, then Gemini answers only from that retrieved context.
- Ambiguous requests ask for clarification and do not enqueue tasks.

Examples:

```text
/pms 딸기 씨앗은 어디서 사?
/pms 아비게일이 좋아하는 선물은 뭐야?
/pms ask 온실 수확해줘
```

Wiki-grounded answers use this wiki by default:

```text
https://ko.stardewvalleywiki.com
```

Answers appear in chat like this:

```text
Pumasi: 딸기 씨앗은 봄 달걀 축제에서 살 수 있어요.
출처: 딸기 - https://ko.stardewvalleywiki.com/딸기
```

## Todo And Automation

The current scanner supports:

- Farm crops.
- Greenhouse crops.
- Ready machines on supported locations.

The current executor supports:

- Water dry crops.
- Harvest ready crops.
- Collect ready machines.

The helper processes one queued task at a time from the top of the visible todo list, like checking items off in order. User-requested tasks are appended to the bottom of the queue, so they wait behind existing work unless the host reorders them.

Each morning, if automation is not `Off`, Pumasi scans the farm and queues about three high-priority safe todos by default. The host can change this with `Assistant.MorningTodoLimit`.

Todo order commands are host-only:

```text
/pms todo move 3 1
/pms todo up 2
/pms todo down 1
/pms todo top 4
/pms todo bottom 1
```

Current limitations:

- No full vanilla-style NPC schedule, friendship, gifts, events, or social behavior yet.
- No planting, selling, destroying, moving rare items, animal care, or chest management execution yet.
- Some settings exist before their executor exists because the MVP is being built incrementally.

## Troubleshooting

- If SMAPI says Generic Mod Config Menu is missing, the mod still works through `config.json`, SMAPI console commands, and `/pms` chat commands.
- If Gemini answers fail, run `pms_status` and check whether `geminiConfigured=True`.
- If a guest command does nothing, confirm host and guest both have the same pumasi mod installed.
- If chat commands are unavailable after an update, restart Stardew Valley/SMAPI so the new DLL is loaded.
- If an API key was accidentally typed into chat, rotate or delete that key in Google AI Studio.
