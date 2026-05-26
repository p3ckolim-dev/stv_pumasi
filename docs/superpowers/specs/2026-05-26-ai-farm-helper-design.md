# AI Farm Helper NPC Design

## Purpose

Create a SMAPI mod for Stardew Valley multiplayer where one shared helper NPC assists with repetitive farm work. The helper should feel like a single visible character to all players, expose an in-game todo list, and use AI for planning, prioritization, and natural-language interaction.

The mod must be realistic to implement: the helper is not a real multiplayer farmhand. It is a host-authoritative NPC-like entity rendered by each client, with all world-changing behavior controlled by the host.

## Goals

- Show one shared helper NPC to the host and all guests who install the mod.
- Let players configure the helper in-game: name, behavior rules, model/provider settings, API key, enabled work categories, and automation mode.
- Generate and display a shared todo list for farm work.
- Execute tasks without duplicate work, double rewards, or conflicting client-side changes.
- Allow guests to submit commands or suggestions while the host remains authoritative.
- Keep AI output constrained to validated task proposals instead of arbitrary game-state changes.

## Non-Goals

- Do not create a real connected multiplayer player/farmhand bot.
- Do not let guests run world-changing automation independently.
- Do not let AI directly mutate saves, inventories, crops, animals, machines, or maps.
- Do not support every farm task in the first milestone.
- Do not send API keys, private prompts, or secret configuration through multiplayer sync.

## Recommended Approach

Use a host-authoritative architecture.

The host runs the AI planner, owns the task queue, executes all world changes, and broadcasts sanitized state snapshots. Guests render the helper NPC, show the shared todo list, and send player commands to the host. This avoids duplicate execution and keeps multiplayer state consistent.

Alternative approaches were considered:

- A real farmhand bot would look native but is much harder because it would need an actual multiplayer session, inventory, tools, movement, and connection lifecycle.
- Fully local automation on every client would be easier to prototype but unsafe in multiplayer because the same task could run more than once.
- Host-authoritative NPC automation gives the best balance of feasibility, safety, and player-facing believability.

## Architecture

```text
Host
  AI Settings
  AI Planner
  Todo Generator
  Task Manager
  Task Executor
  Helper NPC Controller
  Multiplayer State Broadcaster

Guests
  Helper NPC Renderer
  Todo List UI
  Command Input UI
  Host State Receiver
```

Only the host mutates game state. Guests may request changes, but those requests become host-side proposals and must pass the same validation and deduplication pipeline as AI-generated work.

## Major Components

### Config Manager

Stores local mod configuration.

Host settings:

- Helper name.
- Helper personality and behavior rules.
- AI provider mode.
- API base URL, model name, and API key.
- Enabled task categories.
- Automation level.
- Confirmation rules for risky actions.
- Max tasks per day and max API calls per day.

Guest settings:

- Local UI preferences.
- Optional command hotkey.
- Whether to show helper status notifications.

The API key is host-local only. It must be masked in the UI, never logged, never included in multiplayer messages, and never sent to guests. For the first implementation, it will be stored in the mod config file with clear in-game wording that it is local machine configuration. Later, platform keychain storage can be added if needed.

### In-Game Settings UI

Generic Mod Config Menu is an optional dependency. Use it for ordinary settings when it is installed, and provide a custom in-game helper settings menu for secret or long-form fields.

Recommended split:

- Generic Mod Config Menu: helper name, enabled categories, automation toggles, UI preferences, hotkeys.
- Custom menu: API key entry, model/provider settings, behavior rules, connection test, prompt preview.

If Generic Mod Config Menu is missing, the custom menu should still cover all required settings.

The first AI provider adapter will be OpenAI-compatible: configurable base URL, model name, and API key behind an internal provider interface. This keeps the first version concrete while leaving room for other providers later.

### AI Planner

The AI planner receives a compact, sanitized game summary and returns structured task proposals. It does not receive raw save files or secrets.

Input examples:

- Current day, season, weather, time.
- Farm locations with relevant objects.
- Ready crops count and positions.
- Machines ready for collection.
- Animal care summary.
- Current todo list and task history.
- Player-provided instructions.

Output must be constrained JSON-like data:

```json
{
  "message": "온실 작물부터 처리하고 술통을 확인할게요.",
  "tasks": [
    {
      "type": "HarvestCrop",
      "location": "Greenhouse",
      "tile": { "x": 10, "y": 8 },
      "priority": 90,
      "reason": "수확 가능한 작물"
    }
  ]
}
```

The mod validates every AI proposal before it enters the task queue. Invalid, unsafe, unknown, or impossible tasks are rejected with a visible explanation in debug logs and, when useful, in the todo UI.

### Todo List UI

All players see the same logical todo list.

Each todo row shows:

- Status.
- Task name.
- Target location.
- Assigned helper.
- Priority.
- Optional AI explanation.
- Failure or skip reason.

Example statuses:

- Proposed
- Queued
- Claimed
- InProgress
- Completed
- Skipped
- Failed
- Cancelled

The host broadcasts todo snapshots to guests. Guests do not independently edit todo state.

### Task Manager

The Task Manager is the core safety layer.

Responsibilities:

- Convert AI proposals and player requests into normalized tasks.
- Generate stable task keys.
- Deduplicate tasks.
- Validate target state before queueing and again before execution.
- Claim one task at a time for the single helper NPC.
- Track task lifecycle.
- Broadcast state updates.

Example task keys:

```text
HarvestCrop:Farm:64,22
WaterCrop:Farm:65,22
CollectMachine:Farm:Keg:71,19
PetAnimal:Barn1:AnimalId123
```

A task with the same key cannot be queued if an existing task is Queued, Claimed, or InProgress. Before execution, the task must re-check the world state. If the work is already done, it becomes Skipped or Completed without producing duplicate effects.

### Task Executor

The executor performs host-side world changes through explicit task handlers.

Initial task handlers:

- Harvest ready crops.
- Water dry crops.
- Collect finished machines.
- Refill simple machines when input exists and the behavior rules allow it.

Later task handlers:

- Pet animals.
- Collect animal products.
- Fill feeding benches when safe.
- Sort chests.
- Plant seeds after explicit player approval.

Risky actions should require confirmation by default:

- Planting seeds.
- Selling items.
- Moving items between chests.
- Destroying objects.
- Consuming rare resources.
- Any action not easily reversible.

### Helper NPC Controller

The helper should be visible as a single NPC-like character.

MVP behavior:

- Spawn a visual helper on the farm or near the farmhouse.
- Move visually toward task targets when practical.
- Play simple animations while working.
- Display short status text.
- Teleport or path simplification is acceptable when pathfinding fails.

The helper is a representation of host-side task execution, not a real multiplayer farmer. Guests receive position, animation, and status updates from the host and render the same helper.

### Multiplayer Sync

Host broadcasts:

- Helper identity.
- Helper position and animation state.
- Todo list snapshots.
- Task status changes.
- Public helper messages.
- Non-secret configuration that affects shared behavior.

Guests send:

- Natural-language commands.
- Task approval or cancellation requests.
- UI interaction requests.

Never sync:

- API key.
- Raw provider credentials.
- Full private prompt if it contains secrets.
- Local config file contents.
- Detailed debug traces with secrets.

## Data Flow

```text
Player command or scheduled scan
  -> Host builds game summary
  -> AI Planner proposes todos
  -> Task Manager validates and deduplicates
  -> Todo List updates
  -> Helper claims next task
  -> Task Executor revalidates target
  -> Host mutates world state
  -> Host broadcasts result
  -> Guests update UI and helper rendering
```

## Automation Modes

Recommended modes:

- Off: no AI calls and no automatic execution.
- Suggest: AI creates todos, but the player starts tasks manually.
- Confirm: AI queues todos, but risky categories require approval.
- Auto: safe tasks execute automatically; risky tasks still require approval.

The default mode is Confirm. Full Auto is available only after the host explicitly enables it.

## Error Handling

AI provider errors:

- Show a concise in-game message.
- Keep existing todos intact.
- Back off before retrying.
- Do not block manual task execution.

Invalid AI output:

- Reject the malformed proposal.
- Log the validation reason.
- Ask the AI for corrected structured output only when rate limits allow it.

Multiplayer desync:

- Host remains source of truth.
- Guests request a fresh snapshot when state version numbers differ.
- Guest-side visual helper can snap back to the host position if needed.

Task failure:

- Mark Failed with a reason.
- Release the task claim.
- Do not retry infinitely.
- Allow manual retry from the todo UI.

Missing dependency:

- If Generic Mod Config Menu is unavailable, use the custom settings menu.
- If AI provider settings are incomplete, disable AI planning and keep manual task controls available.

## Testing Strategy

Unit-level tests:

- Task key generation.
- Deduplication rules.
- Task lifecycle transitions.
- AI output validation.
- Config serialization without leaking API key into sync payloads.

In-game manual tests:

- Single-player host mode.
- Multiplayer host with one guest using the same mod.
- Guest command submission.
- Duplicate todo proposal rejection.
- Ready crop harvest.
- Machine collection.
- Settings menu API key masking.
- AI disabled fallback.

Regression scenarios:

- Guest lacks API key.
- Guest disconnects while todo list exists.
- Host changes helper name mid-session.
- AI returns invalid task type.
- Target crop is harvested manually before helper arrives.
- Network snapshot arrives out of order.

## Milestones

### Milestone 1: Local Foundation

- Create SMAPI mod project.
- Load and save config.
- Add basic in-game menu entry.
- Implement host-only task queue.
- Implement todo UI in single-player/host mode.
- Add manual task scan without AI.

### Milestone 2: Safe Farm Tasks

- Harvest ready crops.
- Water dry crops.
- Collect ready machines.
- Add TaskKey deduplication.
- Add one-helper serial execution.
- Add helper visual status.

### Milestone 3: Multiplayer Sync

- Detect host and guests.
- Sync todo snapshots.
- Sync helper visual state.
- Allow guest commands to become host-side requests.
- Prevent guest-side world mutation.

### Milestone 4: AI Planner

- Add provider settings and masked API key entry.
- Build sanitized farm summaries.
- Call the provider from host only.
- Validate structured AI task proposals.
- Add behavior rules and automation modes.

### Milestone 5: Polish and Expansion

- Better helper movement and animations.
- Animal care tasks.
- Chest sorting with confirmations.
- Planting and refilling rules.
- Localization.
- Improved error reporting.

## First Implementation Decisions

- Visual asset style: start with a simple custom NPC sprite sheet, then make appearance configurable later.
- AI provider: implement one OpenAI-compatible adapter first.
- Generic Mod Config Menu: keep it optional from day one.
- Pathfinding: attempt normal visual movement when practical; teleport as a fallback when the target is unreachable or movement would stall task execution.

## Acceptance Criteria For First Playable Build

- Host can configure helper name and basic behavior in-game.
- Host can enter and mask an API key in-game.
- One helper appears to host and guest.
- Host and guest see the same todo list.
- AI or manual scan can propose safe crop/machine tasks.
- Duplicate tasks are not queued.
- The helper executes one task at a time.
- Only host mutates world state.
- Guest commands are routed to host.
- API key is not sent to guests or printed in logs.
