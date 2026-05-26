# Wiki-Grounded Answers Design

## Purpose

Extend pumasi (`pms`, Korean name "품앗이") so the helper NPC answers Stardew Valley information questions using the Korean Stardew Valley Wiki as its primary knowledge source.

The target source is:

```text
https://ko.stardewvalleywiki.com/Stardew_Valley_Wiki
```

The wiki is the official Korean Stardew Valley Wiki, is MediaWiki-powered, and provides gameplay pages for crops, animals, seasons, weather, villagers, friendship, quests, bundles, fish, machines, locations, and other topics. Its content is under Creative Commons Attribution-NonCommercial-ShareAlike unless otherwise noted, so in-game answers should show page titles or source URLs when practical.

## Goals

- Let players ask game-information questions through the existing `pms_ask` command path.
- Ground answers in Korean Stardew Valley Wiki pages instead of relying only on the model's memory.
- Keep information answers separate from automation tasks.
- Answer in Korean by default.
- Include compact source references, such as page titles or URLs.
- Avoid guessing when the wiki search does not return relevant evidence.
- Keep API keys local to the host and never include them in wiki requests or multiplayer sync messages.

## Non-Goals

- Do not mirror the full wiki into the mod.
- Do not scrape every page on startup.
- Do not quote large wiki passages in chat or UI.
- Do not let wiki answers directly enqueue or execute farm tasks.
- Do not add offline search in the first version.
- Do not support non-Korean wiki domains in the first version.

## Recommended Approach

Use a lightweight online retrieval flow:

```text
player input
  -> intent classifier
  -> wiki search and page extract
  -> Gemini grounded-answer prompt
  -> answer message with sources
```

This keeps the first implementation small and accurate enough. The wiki is queried only when the player asks an information question. The AI receives short extracted snippets, not full pages.

## Intent Routing

`pms_ask` should route input into one of three modes:

- `TaskPlanning`: farm automation request, such as "온실 수확해줘".
- `WikiAnswer`: information question, such as "딸기는 어디서 사?", "아비게일이 좋아하는 선물은 뭐야?", or "여름에 수익 좋은 작물 알려줘".
- `Ambiguous`: could be either information or action, such as "온실 어떻게 할까?".

Routing rule:

- If the input asks for doing, executing, scanning, harvesting, watering, collecting, moving, or scheduling farm work, keep the existing task planning flow.
- If the input asks "뭐야", "어디", "언제", "어떻게", "추천", "좋아하는", "가격", "조건", "얻는 법", "위치", or similar knowledge-seeking language, use wiki answer flow.
- If ambiguous, answer with a short clarification and do not enqueue tasks.

## Components

### KnowledgeIntentClassifier

Small deterministic classifier that runs before Gemini.

Input:

- Raw player command.

Output:

```text
TaskPlanning | WikiAnswer | Ambiguous
```

This should be testable without SMAPI dependencies.

### WikiSearchService

Searches Korean Stardew Valley Wiki pages.

First implementation:

- Use MediaWiki `api.php`.
- Search endpoint:
  - `action=query`
  - `list=search`
  - `srsearch=<query>`
  - `format=json`
  - `utf8=1`
- Limit to the top 3 to 5 pages.
- Keep page title, URL, and a short search snippet.

Search URL base:

```text
https://ko.stardewvalleywiki.com/mediawiki/api.php
```

### WikiPageFetcher

Fetches short page extracts for the top search results.

First implementation:

- Use MediaWiki `api.php`.
- Extract endpoint:
  - `action=query`
  - `prop=extracts`
  - `explaintext=1`
  - `exintro=0`
  - `titles=<title>`
  - `format=json`
  - `utf8=1`
- Trim each extract to a safe character budget.
- Remove empty sections and navigation-like noise when practical.

The first version should prefer plain text extracts over raw HTML parsing.

### WikiContextBuilder

Creates a compact prompt context from search results and extracts.

Rules:

- Include at most 3 pages per question.
- Include title, URL, and trimmed extract.
- Keep total wiki context below a configurable character budget.
- Preserve enough table-like lines for crop price, season, gift, fish, and location facts.

### GroundedAnswerPlanner

Wraps the existing Gemini client and asks for a Korean answer grounded only in provided wiki context.

Prompt rules:

```text
You are pumasi (품앗이), a Stardew Valley helper NPC.
Answer in Korean.
Use only the Korean Stardew Valley Wiki context below.
If the context is insufficient, say you could not confirm it from the wiki.
Do not invent facts.
Keep the answer concise and practical.
Include source page titles at the end.
Do not enqueue or execute game tasks.
```

Output shape:

```json
{
  "answer": "딸기는 봄 달걀 축제에서 피에르에게 씨앗을 살 수 있어요...",
  "sources": [
    {
      "title": "딸기",
      "url": "https://ko.stardewvalleywiki.com/딸기"
    }
  ],
  "confidence": "grounded"
}
```

### Answer Delivery

Host generates wiki-grounded answers and broadcasts the public helper message to guests.

The first UI can use:

- SMAPI console output for `pms_ask`.
- Helper status text for short answers.
- Todo overlay is not used for long wiki answers.

Later UI can add a dedicated answer panel.

## Multiplayer Behavior

- Guests may ask wiki questions through `pms_ask`.
- Guest questions are sent to the host.
- Host performs wiki search, Gemini call, and answer validation.
- Host broadcasts only the final answer and source metadata.
- Guests never receive Gemini API key, provider config, raw prompt, or hidden logs.

## Error Handling

Wiki unavailable:

- Answer: "지금은 위키에 접속할 수 없어서 확인하지 못했어요."
- Do not fall back to ungrounded model memory unless the host explicitly enables that option later.

No relevant page:

- Answer: "한국어 위키에서 관련 내용을 찾지 못했어요."

Gemini unavailable:

- Show the wiki page titles and URLs found, then say the AI summary could not be generated.

Malformed Gemini answer:

- Reject the model output and return a simple deterministic answer that points to the source pages.

Rate limiting:

- Add a per-session cooldown for wiki questions.
- Reuse cached search results and page extracts for repeated questions.

## Caching

First version cache:

- In-memory only.
- Key by normalized wiki search query and page title.
- Expire entries after the game session ends.

Do not write fetched wiki content to disk in the first version.

## Configuration

Add settings:

- `WikiAnswersEnabled`: default `true`.
- `WikiBaseUrl`: default `https://ko.stardewvalleywiki.com`.
- `WikiMaxPages`: default `3`.
- `WikiContextCharacterLimit`: default `8000`.
- `WikiQuestionCooldownSeconds`: default `10`.

These settings contain no secrets and can be included in shared config snapshots if needed.

## Testing Strategy

Core tests:

- Intent classifier routes task requests to `TaskPlanning`.
- Intent classifier routes information questions to `WikiAnswer`.
- Intent classifier routes ambiguous questions to `Ambiguous`.
- Wiki search request URL is correct.
- Wiki extract request URL is correct.
- Wiki context builder respects page count and character limits.
- Grounded answer parser rejects missing answer text.

Integration tests with fake HTTP:

- Search response with two pages becomes source metadata.
- Extract response becomes trimmed prompt context.
- Gemini grounded answer includes title and URL sources.
- Wiki failure returns a non-throwing error result.

Manual in-game tests:

- Host asks "딸기 씨앗은 어디서 사?"
- Guest asks "아비게일이 좋아하는 선물은 뭐야?"
- Host asks "온실 수확해줘" and still gets task planning, not wiki answer.
- Ambiguous input does not enqueue tasks.
- API key is not logged or synced.

## Milestones

### Milestone 1: Core Wiki Retrieval

- Add core intent classifier.
- Add wiki search and extract request builders.
- Add fake-HTTP unit tests.
- Add context builder.

### Milestone 2: Grounded Gemini Answer

- Add grounded answer prompt builder.
- Add JSON response parser.
- Add error-safe result types.
- Add tests for valid and invalid answers.

### Milestone 3: SMAPI Integration

- Route `pms_ask` through intent classifier.
- Keep task planning unchanged for action requests.
- Use wiki answer flow for information questions.
- Broadcast host-generated answers to guests.

### Milestone 4: Polish

- Add cooldown and in-memory cache.
- Add config fields.
- Document source attribution and Korean Wiki dependency.

## Acceptance Criteria

- `pms_ask 딸기 씨앗은 어디서 사?` uses Korean wiki retrieval before Gemini answer generation.
- `pms_ask 온실 수확해줘` still queues farm tasks through the existing task planner.
- Ambiguous input does not mutate world state.
- Answers are in Korean.
- Answers include source page titles or URLs.
- When wiki evidence is missing, pumasi says it cannot confirm from the wiki.
- Host performs all wiki and Gemini calls.
- Guests receive only final public answer content and source metadata.
