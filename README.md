# pumasi

`pumasi` (`pms`, Korean name: `품앗이`) is a SMAPI mod prototype for Stardew Valley. It adds a host-authoritative helper concept that can answer Stardew Valley questions, keep a shared todo list, and execute a small set of safe repetitive farm chores.

Current mod version: `0.1.2`

- English documentation is below.
- 한국어 문서는 아래쪽의 [한국어](#한국어) 섹션을 확인하세요.

## English

### Current Scope

- Works in single-player and multiplayer.
- In multiplayer, the host owns all AI calls and all world-changing behavior.
- Guests can view synced todos and send helper requests to the host.
- Helper answers are shown in the in-game chat for the host and guests.
- Stardew Valley information questions are grounded with the Korean Stardew Valley Wiki.
- Gemini is used for task planning and wiki-grounded answer generation.
- Gemini API keys are stored only in the local host config.
- Tasks are deduplicated with stable task keys.
- The helper executes one queued task at a time.
- Current safe task handlers cover crop watering, crop harvesting, and ready machine collection on the farm and greenhouse.

This is still an MVP/prototype. The helper NPC is currently a gameplay helper plus overlay/chat presence, not a full vanilla-style NPC with schedules, dialogue friendship, events, or custom social behavior.

### Requirements

- Stardew Valley 1.6 with SMAPI 4.x.
- .NET 6 SDK to build locally.
- Optional: Generic Mod Config Menu for in-game settings.
- Gemini API key for AI planning and wiki answer summaries.

### Install

Build the mod:

```bash
./.dotnet/dotnet build Pumasi.sln
```

The SMAPI build package creates an installable zip here:

```text
src/Pumasi/bin/Debug/net6.0/Pumasi 0.1.2.zip
```

Unzip it into the Stardew Valley `Mods` folder. On the current local macOS install path, that folder is:

```text
/Users/p3ckolim/Library/Application Support/Steam/steamapps/common/Stardew Valley/Contents/MacOS/Mods
```

After installation, SMAPI should show `pumasi 0.1.2` in the loaded mod list.

### Gemini API Key

The API key is saved in the host's local SMAPI config:

```text
Mods/Pumasi/config.json
```

On the current local install, the full path is:

```text
/Users/p3ckolim/Library/Application Support/Steam/steamapps/common/Stardew Valley/Contents/MacOS/Mods/Pumasi/config.json
```

Set the key through the SMAPI console:

```text
pms_key YOUR_GEMINI_API_KEY
```

If Generic Mod Config Menu is installed, open pumasi's Gemini section. The API key field displays `********` after a key is saved; entering a new non-empty value replaces the stored key.

Important security notes:

- The key is stored locally in plain text inside `config.json`.
- The key is not sent to guests.
- The key is not included in multiplayer messages, todo snapshots, shared config snapshots, or logs.
- `/pms_key` is intentionally rejected in in-game chat to avoid accidental key leaks.
- Only the host/single-player user needs to configure a key.

### Commands

SMAPI console commands:

```text
pms_status
pms_scan
pms_ask <question or farm-work request>
pms_key <gemini-api-key>
pms_todo
```

In-game chat commands:

```text
/pms status
/pms scan
/pms todo
/pms ask <question or farm-work request>
/pms <question or farm-work request>
```

Registered chat aliases:

```text
/pms_ask
/pms_status
/pms_scan
/pms_todo
```

`/pms_key` is registered only to reject it safely in chat. Use the SMAPI console, config file, or Generic Mod Config Menu for API keys.

### Single-Player Behavior

Single-player works because the local player is the main player. Commands run locally, Gemini calls use the local config, and safe task execution can mutate the current save.

Useful single-player test commands:

```text
pms_status
pms_key YOUR_GEMINI_API_KEY
pms_ask 딸기 씨앗은 어디서 사?
pms_ask 온실 수확해줘
pms_scan
pms_todo
```

### Multiplayer Behavior

The host is authoritative:

- The host stores the Gemini API key.
- The host calls Gemini and the Korean wiki.
- The host owns the task queue.
- The host executes world-changing tasks.
- The host broadcasts todo snapshots, helper state, and public answer text to guests.

Guests can request work or ask questions:

```text
/pms 딸기 씨앗은 어디서 사?
/pms ask 온실 수확해줘
```

Guest requests are sent to the host. Guests do not need a Gemini API key. The final answer appears in the in-game chat for everyone who has the mod installed.

### AI And Wiki Answers

`pms_ask` and `/pms` classify the request:

- Farm-work requests use the Gemini task planner.
- Stardew Valley information questions search the Korean Stardew Valley Wiki, then ask Gemini to answer only from that wiki context.
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
Lumi: 딸기 씨앗은 봄 달걀 축제에서 살 수 있어요.
출처: 딸기 - https://ko.stardewvalleywiki.com/딸기
```

### Todo And Automation

The mod scans supported locations for safe chores and builds a deduplicated todo queue. Current scanning supports:

- Farm crops.
- Greenhouse crops.
- Ready machines on supported locations.

Current execution supports:

- Water dry crops.
- Harvest ready crops.
- Collect ready machines.

The helper processes one queued task at a time. Planting, selling, destroying, moving rare items, animals, chests, and broader NPC-like behavior are not part of the current MVP execution layer.

### Configuration

Key defaults:

```json
{
  "Assistant": {
    "Name": "Lumi",
    "AutomationMode": "Confirm",
    "MaxTasksPerDay": 60
  },
  "Gemini": {
    "BaseUrl": "https://generativelanguage.googleapis.com/v1beta",
    "Model": "gemini-2.5-flash",
    "ApiKey": "",
    "TimeoutSeconds": 30,
    "MaxCallsPerDay": 20
  },
  "WikiAnswers": {
    "WikiAnswersEnabled": true,
    "WikiBaseUrl": "https://ko.stardewvalleywiki.com",
    "WikiMaxPages": 3,
    "WikiContextCharacterLimit": 8000,
    "WikiQuestionCooldownSeconds": 10
  },
  "Ui": {
    "ShowTodoOverlay": true,
    "ToggleOverlayButton": "F8"
  }
}
```

### Build And Test

```bash
./.dotnet/dotnet build Pumasi.sln
./.dotnet/dotnet test Pumasi.sln
```

The local `.dotnet/` install path is ignored by git, so a local SDK can be used without committing it.

### Troubleshooting

- If SMAPI says Generic Mod Config Menu is missing, the mod still works through `config.json`, SMAPI console commands, and `/pms` chat commands.
- If Gemini answers fail, check `pms_status` and confirm `geminiConfigured=True`.
- If a guest command does nothing, confirm the host and guest both have the same pumasi mod installed.
- If chat commands are unavailable after an update, restart Stardew Valley/SMAPI so the new DLL is loaded.
- If an API key was accidentally written into a chat message, rotate the key in Google AI Studio.

## 한국어

### 현재 범위

`pumasi` (`pms`, 한글 이름: `품앗이`)는 스타듀밸리 SMAPI 모드 프로토타입입니다. 싱글과 멀티에서 사용할 수 있고, 반복적인 농장 잡무를 안전하게 처리하며, 스타듀밸리 질문에는 한국어 스타듀밸리 위키 기반으로 답변하도록 설계되어 있습니다.

현재 모드 버전: `0.1.2`

- 싱글 플레이에서 작동합니다.
- 멀티플레이에서는 호스트가 모든 AI 호출과 월드 변경 작업을 담당합니다.
- 게스트는 공유 투두를 보고, 호스트에게 작업 요청이나 질문을 보낼 수 있습니다.
- 품앗이 답변은 호스트와 게스트의 인게임 채팅창에 표시됩니다.
- 정보 질문은 한국어 스타듀밸리 위키를 검색한 뒤 Gemini가 해당 문맥만 기반으로 답변합니다.
- Gemini API 키는 호스트 로컬 config에만 저장됩니다.
- 작업은 안정적인 task key로 중복 등록을 피합니다.
- 도우미는 큐에 쌓인 작업을 한 번에 하나씩 처리합니다.
- 현재 안전 작업은 농작물 물주기, 다 자란 작물 수확, 준비된 기계 수거를 지원합니다.

아직 MVP/프로토타입 단계입니다. 현재 품앗이는 전체 바닐라 NPC처럼 일정, 호감도, 이벤트, 깊은 대화 시스템을 가진 캐릭터라기보다, 오버레이/채팅/작업 큐를 가진 농장 도우미 구조입니다.

### 요구 사항

- Stardew Valley 1.6 + SMAPI 4.x.
- 로컬 빌드를 위한 .NET 6 SDK.
- 선택 사항: 인게임 설정용 Generic Mod Config Menu.
- AI 계획과 위키 답변 요약을 위한 Gemini API 키.

### 설치

모드를 빌드합니다.

```bash
./.dotnet/dotnet build Pumasi.sln
```

빌드 후 설치 가능한 zip은 여기에 생성됩니다.

```text
src/Pumasi/bin/Debug/net6.0/Pumasi 0.1.2.zip
```

이 zip을 Stardew Valley `Mods` 폴더에 풀면 됩니다. 현재 로컬 macOS 설치 기준 경로는 다음과 같습니다.

```text
/Users/p3ckolim/Library/Application Support/Steam/steamapps/common/Stardew Valley/Contents/MacOS/Mods
```

설치 후 SMAPI 로그에서 `pumasi 0.1.2`가 로드되면 정상입니다.

### Gemini API 키

API 키는 호스트의 로컬 SMAPI 설정 파일에 저장됩니다.

```text
Mods/Pumasi/config.json
```

현재 로컬 설치 기준 전체 경로는 다음과 같습니다.

```text
/Users/p3ckolim/Library/Application Support/Steam/steamapps/common/Stardew Valley/Contents/MacOS/Mods/Pumasi/config.json
```

SMAPI 콘솔에서 키를 설정합니다.

```text
pms_key YOUR_GEMINI_API_KEY
```

Generic Mod Config Menu가 설치되어 있다면, 인게임 설정의 pumasi Gemini 섹션에서도 설정할 수 있습니다. 저장된 키는 `********`로 표시되고, 새 값을 입력하면 교체됩니다.

보안상 중요한 점:

- 키는 `config.json`에 평문으로 저장됩니다.
- 키는 게스트에게 전송되지 않습니다.
- 키는 멀티플레이 메시지, 투두 스냅샷, 공유 설정 스냅샷, 로그에 포함되지 않습니다.
- `/pms_key`는 실수로 채팅에 키를 입력하지 않도록 인게임 채팅에서 거부됩니다.
- 싱글 플레이어 또는 멀티 호스트만 키를 설정하면 됩니다.

### 명령어

SMAPI 콘솔 명령어:

```text
pms_status
pms_scan
pms_ask <질문 또는 농장 작업 요청>
pms_key <gemini-api-key>
pms_todo
```

인게임 채팅 명령어:

```text
/pms status
/pms scan
/pms todo
/pms ask <질문 또는 농장 작업 요청>
/pms <질문 또는 농장 작업 요청>
```

등록된 채팅 별칭:

```text
/pms_ask
/pms_status
/pms_scan
/pms_todo
```

`/pms_key`는 채팅에서 안전하게 거부되도록만 등록되어 있습니다. API 키는 SMAPI 콘솔, config 파일, Generic Mod Config Menu로 설정하세요.

### 싱글 플레이 동작

싱글 플레이에서는 로컬 플레이어가 곧 메인 플레이어이므로 그대로 작동합니다. 명령은 로컬에서 처리되고, Gemini 호출은 로컬 config의 키를 사용하며, 안전 작업은 현재 세이브에 직접 반영됩니다.

싱글 테스트용 명령어:

```text
pms_status
pms_key YOUR_GEMINI_API_KEY
pms_ask 딸기 씨앗은 어디서 사?
pms_ask 온실 수확해줘
pms_scan
pms_todo
```

### 멀티플레이 동작

호스트가 권한을 가집니다.

- 호스트가 Gemini API 키를 저장합니다.
- 호스트가 Gemini와 한국어 위키를 호출합니다.
- 호스트가 작업 큐를 소유합니다.
- 호스트가 월드를 변경하는 작업을 실행합니다.
- 호스트가 투두 목록, 도우미 상태, 공개 답변 텍스트를 게스트에게 브로드캐스트합니다.

게스트는 질문이나 작업 요청을 보낼 수 있습니다.

```text
/pms 딸기 씨앗은 어디서 사?
/pms ask 온실 수확해줘
```

게스트 요청은 호스트에게 전달됩니다. 게스트는 Gemini API 키가 필요 없습니다. 최종 답변은 모드를 설치한 모든 플레이어의 인게임 채팅창에 표시됩니다.

### AI와 위키 답변

`pms_ask`와 `/pms`는 요청을 분류합니다.

- 농장 작업 요청은 Gemini 작업 계획기로 전달됩니다.
- 스타듀밸리 정보 질문은 한국어 스타듀밸리 위키를 검색한 뒤, Gemini가 해당 위키 문맥만 기반으로 답변합니다.
- 애매한 요청은 작업을 등록하지 않고 더 구체적으로 말해달라고 답합니다.

예시:

```text
/pms 딸기 씨앗은 어디서 사?
/pms 아비게일이 좋아하는 선물은 뭐야?
/pms ask 온실 수확해줘
```

위키 기반 답변은 기본적으로 아래 사이트를 사용합니다.

```text
https://ko.stardewvalleywiki.com
```

답변은 채팅창에 이런 형태로 표시됩니다.

```text
Lumi: 딸기 씨앗은 봄 달걀 축제에서 살 수 있어요.
출처: 딸기 - https://ko.stardewvalleywiki.com/딸기
```

### 투두와 자동화

모드는 지원 위치를 스캔해서 안전한 잡무를 찾고, 중복을 피하며 투두 큐에 등록합니다. 현재 스캔 지원 범위는 다음과 같습니다.

- 농장 작물.
- 온실 작물.
- 지원 위치의 준비된 기계.

현재 실행 지원 범위는 다음과 같습니다.

- 마른 작물 물주기.
- 다 자란 작물 수확.
- 준비된 기계 수거.

도우미는 큐에 있는 작업을 한 번에 하나씩 처리합니다. 씨앗 심기, 판매, 파괴, 희귀 아이템 이동, 동물, 상자, 더 넓은 NPC 행동은 현재 MVP 실행 범위에 포함되어 있지 않습니다.

### 설정

주요 기본값:

```json
{
  "Assistant": {
    "Name": "Lumi",
    "AutomationMode": "Confirm",
    "MaxTasksPerDay": 60
  },
  "Gemini": {
    "BaseUrl": "https://generativelanguage.googleapis.com/v1beta",
    "Model": "gemini-2.5-flash",
    "ApiKey": "",
    "TimeoutSeconds": 30,
    "MaxCallsPerDay": 20
  },
  "WikiAnswers": {
    "WikiAnswersEnabled": true,
    "WikiBaseUrl": "https://ko.stardewvalleywiki.com",
    "WikiMaxPages": 3,
    "WikiContextCharacterLimit": 8000,
    "WikiQuestionCooldownSeconds": 10
  },
  "Ui": {
    "ShowTodoOverlay": true,
    "ToggleOverlayButton": "F8"
  }
}
```

### 빌드와 테스트

```bash
./.dotnet/dotnet build Pumasi.sln
./.dotnet/dotnet test Pumasi.sln
```

로컬 `.dotnet/` 설치 경로는 git에서 제외되어 있으므로, 로컬 SDK를 커밋하지 않고 사용할 수 있습니다.

### 문제 해결

- SMAPI가 Generic Mod Config Menu가 없다고 표시해도, `config.json`, SMAPI 콘솔 명령어, `/pms` 채팅 명령어로 사용할 수 있습니다.
- Gemini 답변이 실패하면 `pms_status`로 `geminiConfigured=True`인지 확인하세요.
- 게스트 명령이 반응하지 않으면 호스트와 게스트 모두 같은 pumasi 모드를 설치했는지 확인하세요.
- 업데이트 후 채팅 명령이 안 보이면 Stardew Valley/SMAPI를 재시작해서 새 DLL을 로드하세요.
- API 키를 실수로 채팅에 입력했다면 Google AI Studio에서 키를 재발급하거나 폐기하세요.
