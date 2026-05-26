# Wiki-Grounded Answers Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add Korean Stardew Valley Wiki-grounded information answers to `pms_ask` while preserving existing farm task planning for action requests.

**Architecture:** Add wiki retrieval and answer generation to `Pumasi.Core` so it can be tested without SMAPI. The SMAPI layer will route `pms_ask` through an intent classifier: task requests keep the existing planner, information questions use wiki retrieval plus Gemini grounded answer generation, and ambiguous requests do not mutate world state.

**Tech Stack:** C#/.NET 6, xUnit, SMAPI, Gemini REST API, Korean Stardew Valley Wiki MediaWiki `api.php`.

---

## File Structure

- `src/Pumasi.Core/Knowledge/`: intent classifier, wiki search/extract models, wiki client, context builder, grounded answer planner.
- `tests/Pumasi.Core.Tests/Knowledge/`: unit tests for routing, request construction, context trimming, and answer parsing.
- `src/Pumasi.Core/Configuration/`: wiki answer config fields.
- `src/Pumasi/ModEntry.cs`: route `pms_ask` to task planning or wiki answer flow.
- `src/Pumasi/Multiplayer/`: helper answer broadcast message.
- `README.md`: document wiki-grounded answers and source behavior.

## Tasks

### Task 1: Intent Classifier

**Files:**
- Create: `src/Pumasi.Core/Knowledge/KnowledgeIntent.cs`
- Create: `src/Pumasi.Core/Knowledge/KnowledgeIntentClassifier.cs`
- Create: `tests/Pumasi.Core.Tests/Knowledge/KnowledgeIntentClassifierTests.cs`

- [x] Add failing tests for task, wiki, and ambiguous routing.
- [x] Implement deterministic Korean/English keyword routing.
- [x] Verify `온실 수확해줘` routes to task planning and `딸기 씨앗은 어디서 사?` routes to wiki answer.

### Task 2: Wiki Retrieval Models And Client

**Files:**
- Create: `src/Pumasi.Core/Knowledge/WikiModels.cs`
- Create: `src/Pumasi.Core/Knowledge/WikiClient.cs`
- Create: `tests/Pumasi.Core.Tests/Knowledge/WikiClientTests.cs`

- [x] Add fake-HTTP tests for MediaWiki search request URL.
- [x] Add fake-HTTP tests for MediaWiki extract request URL.
- [x] Implement search and extract parsing.
- [x] Return non-throwing error results for HTTP and malformed JSON failures.

### Task 3: Wiki Context Builder

**Files:**
- Create: `src/Pumasi.Core/Knowledge/WikiContextBuilder.cs`
- Create: `tests/Pumasi.Core.Tests/Knowledge/WikiContextBuilderTests.cs`

- [x] Add tests for max page count and character budget.
- [x] Implement compact context builder with title, URL, and extract.
- [x] Preserve source metadata for the answer response.

### Task 4: Grounded Answer Planner

**Files:**
- Create: `src/Pumasi.Core/Knowledge/GroundedAnswerPlanner.cs`
- Create: `tests/Pumasi.Core.Tests/Knowledge/GroundedAnswerPlannerTests.cs`

- [x] Add tests for prompt rules requiring Korean wiki grounding.
- [x] Add tests for parsing valid answer JSON with source titles and URLs.
- [x] Add tests for rejecting missing answer text.
- [x] Implement answer prompt builder and parser using the existing `GeminiClient`.

### Task 5: Configuration

**Files:**
- Modify: `src/Pumasi.Core/Configuration/ModConfig.cs`
- Create: `src/Pumasi.Core/Configuration/WikiAnswerConfig.cs`
- Modify: `src/Pumasi/Services/ConfigService.cs`
- Test: `tests/Pumasi.Core.Tests/Configuration/ConfigRedactionTests.cs`

- [x] Add tests proving wiki config contains no secrets and can be serialized.
- [x] Add wiki config defaults.
- [x] Add GMCM options for wiki enabled, base URL, max pages, context limit, and cooldown.

### Task 6: SMAPI Integration And Multiplayer Answer Broadcast

**Files:**
- Modify: `src/Pumasi/ModEntry.cs`
- Modify: `src/Pumasi/Multiplayer/Messages.cs`
- Modify: `src/Pumasi/Multiplayer/MultiplayerSyncService.cs`

- [x] Add final-answer message type and broadcast support.
- [x] Route host `pms_ask` wiki questions through wiki retrieval and grounded answer generation.
- [x] Route guest wiki questions to host using the existing guest command path.
- [x] Ensure ambiguous requests produce clarification and do not enqueue tasks.

### Task 7: Docs And Verification

**Files:**
- Modify: `README.md`
- Modify: `docs/superpowers/plans/2026-05-27-wiki-grounded-answers.md`

- [x] Document wiki-grounded answers and source attribution.
- [x] Run `./.dotnet/dotnet build Pumasi.sln`.
- [x] Run `./.dotnet/dotnet test Pumasi.sln`.
- [x] Commit and push the implementation branch.
