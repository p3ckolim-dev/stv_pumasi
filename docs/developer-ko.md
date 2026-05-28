# pumasi 개발자 가이드

퀵링크: [홈](../README.md) | 사용자: [English](user-en.md) / [한국어](user-ko.md) | 개발자: [English](developer-en.md) / [한국어](developer-ko.md)

이 문서는 `pumasi`를 포크해서 수정, 테스트, 확장하려는 개발자를 위한 문서입니다.

현재 모드 버전: `0.1.9`

## 저장소 구조

```text
Pumasi.sln
src/
  Pumasi/                  SMAPI 진입점과 Stardew 전용 연동
  Pumasi.Core/             SMAPI 의존성이 없는 테스트 가능한 도메인 로직
tests/
  Pumasi.Core.Tests/       Core 동작을 검증하는 xUnit 테스트
docs/
  user-en.md
  user-ko.md
  developer-en.md
  developer-ko.md
  superpowers/             계획과 설계 이력
```

모드는 의도적으로 두 계층으로 나뉩니다.

- `Pumasi.Core`: AI 프롬프트 구성, 명령 파싱, 설정 모델, 위키 검색 모델, 작업 큐 로직, 순수 포매팅.
- `Pumasi`: SMAPI 생명주기 훅, 콘솔/채팅 명령 등록, Stardew Valley 스캔/실행, 멀티플레이 동기화, 오버레이 렌더링, 설정 UI 연동.

## 빌드와 테스트

저장소 로컬 .NET 명령이 있으면 아래처럼 실행합니다.

```bash
./.dotnet/dotnet build Pumasi.sln
./.dotnet/dotnet test Pumasi.sln
```

빌드가 SMAPI zip을 생성합니다.

```text
src/Pumasi/bin/Debug/net6.0/Pumasi 0.1.9.zip
```

`.dotnet/` 디렉터리는 git에서 제외되어 있으므로 로컬 SDK를 설치해도 커밋되지 않습니다.

## 로컬 설치 테스트

생성된 zip을 Stardew Valley `Mods` 폴더에 풉니다. 설치된 폴더는 아래 형태여야 합니다.

```text
Stardew Valley/
  Mods/
    Pumasi/
```

Steam 기준 일반적인 위치는 다음과 같습니다.

```text
Windows: C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley\Mods
macOS:   ~/Library/Application Support/Steam/steamapps/common/Stardew Valley/Contents/MacOS/Mods
Linux:   ~/.local/share/Steam/steamapps/common/Stardew Valley/Mods
```

SMAPI는 시작할 때 정확한 `Mods go here:` 경로를 출력합니다. 다른 설치 환경에서는 그 경로를 우선 사용하세요.

## 현재 아키텍처

`ModEntry`가 SMAPI 진입점입니다. 이 파일은 다음을 연결합니다.

- 세이브 로드, 하루 시작, 업데이트 틱, HUD 렌더링을 위한 게임 루프 이벤트.
- SMAPI 콘솔 명령.
- `ChatCommands.Register`를 통한 Stardew 채팅 명령.
- 멀티플레이 메시지 라우팅.
- Gemini 계획과 위키 기반 답변 흐름.

`TaskManager`는 투두 큐를 소유하고, 보이는 투두 순서를 유지하며, 호스트의 활성 투두 순서 조정을 지원하고, task key로 활성 작업 중복을 방지합니다.

`FarmTaskScanner`는 현재 지원 위치에서 후보 작업을 찾습니다.

`FarmTaskExecutor`는 현재 구현된 안전 작업에 대해 Stardew 월드를 변경합니다.

`MultiplayerSyncService`는 호스트 소유 상태를 브로드캐스트하고 게스트 명령을 호스트로 전달합니다.

`Pumasi.Core.Chat.HelperChatFormatter`는 도우미 답변을 인게임 채팅 줄로 포매팅합니다.

## 명령 흐름

콘솔 명령:

```text
pms_status
pms_scan
pms_ask <질문 또는 농장 작업 요청>
pms_key <gemini-api-key>
pms_todo [move <from> <to>|up <index>|down <index>|top <index>|bottom <index>]
pms_work <category> on|off
```

채팅 명령:

```text
/pms status
/pms scan
/pms todo
/pms todo move 3 1
/pms animals on
/pms work animals off
/pms ask <질문 또는 농장 작업 요청>
/pms <질문 또는 농장 작업 요청>
```

명령 파싱은 `Pumasi.Core.Commands.PumasiCommandParser`에 모여 있습니다. 인게임 채팅을 통한 키 입력은 설계상 거부되므로, 비밀값은 채팅 경로에서 받지 않습니다.

투두 보드의 `^` / `v` 버튼 클릭은 SMAPI 입력 이벤트에서 감지한 뒤 기존 `TaskManager.MoveActiveTask`를 호출합니다. 이 경로도 호스트 전용이며, 명령어 기반 순서 조정과 같은 검증 규칙을 공유합니다.

투두 실행은 보이는 큐 순서대로 맨 위부터 진행됩니다. 아침 농장 스캔은 기본적으로 `Assistant.MorningTodoLimit`만큼 안전 작업을 큐에 추가하고, 유저/AI 계획 작업은 호스트가 순서를 조정하지 않는 한 맨 아래에 추가됩니다.

## AI와 위키 흐름

`KnowledgeIntentClassifier`는 `pms_ask`와 `/pms` 입력을 분류합니다.

- 작업 계획: `PlanWithGeminiAsync`.
- 위키 답변: `AnswerWithWikiAsync`.
- 애매한 요청: `ContextualIntentRouter`가 최근 대화와 현재 투두를 Gemini에 전달해 작업 계획, 위키 답변, 일반 대화 답변, 구체화 질문 중 하나로 라우팅합니다.

위키 답변은 아래 구성요소를 사용합니다.

- `WikiClient`: MediaWiki API 검색과 extract 호출.
- `WikiMemoryCache`: 인메모리 검색/페이지 캐시.
- `WikiContextBuilder`: Gemini에 전달할 문맥 구성.
- `GroundedAnswerPlanner`: Gemini 프롬프트 생성과 JSON 답변 파싱.

기본 위키 URL:

```text
https://ko.stardewvalleywiki.com
```

## 멀티플레이 모델

호스트가 권한을 가집니다.

- 게스트는 Gemini를 호출하지 않습니다.
- 게스트는 월드를 변경하지 않습니다.
- 게스트는 `GuestCommandMessage`를 호스트에게 보냅니다.
- 호스트는 `TodoSnapshotMessage`, `HelperStateMessage`, `SharedConfigMessage`, `HelperAnswerMessage`를 브로드캐스트합니다.
- 공유 설정 스냅샷에는 Gemini API 키가 의도적으로 포함되지 않습니다.

이 구조는 중복 작업 실행을 피하고 월드 변경을 한 프로세스에 집중시킵니다.

## 설정과 비밀값

사용자 설정 파일은 아래 위치에 생성됩니다.

```text
Mods/Pumasi/config.json
```

`ConfigService.SetGeminiApiKey`는 SMAPI 설정 API를 통해 키를 저장합니다. 키는 로컬 평문 값이므로, 이후 코드 변경에서도 키를 로그로 남기거나 멀티플레이 메시지에 포함하지 않아야 합니다.

기존 `ConfigRedactionTests`가 redaction과 공유 설정 동작을 검증합니다.

## 현재 MVP 경계

구현된 실행 기능:

- 마른 작물 물주기.
- 다 자란 작물 수확.
- 준비된 기계 수거.
- 스프링클러 주변 일반 땅 파기.
- 동물 건물 건초 리필.

스캐너 범위:

- 농장.
- 온실.
- 스프링클러 주변의 아직 파지 않은 일반 땅.
- `Animals` 작업 카테고리가 켜진 경우 로드된 동물 건물의 건초 리필 후보.

아직 구현되지 않은 범위:

- 전체 NPC 일정, 호감도, 선물, 이벤트, 사회적 행동.
- 씨앗 심기, 판매, 파괴, 희귀 아이템 이동, 쓰다듬기/생산품 수거 같은 세부 동물 돌보기, 상자 관리.
- 현재 런타임 스냅샷을 넘어서는 영구 투두 저장.
- 멀티플레이 게스트별 권한 UI.

동작을 확장할 때는 월드 변경을 계속 호스트 전용으로 유지하고, SMAPI 의존성이 없는 로직에는 먼저 Core 테스트를 추가하는 것이 좋습니다.

## 포크와 개발 워크플로

1. 저장소를 포크합니다.
2. 변경용 브랜치를 만듭니다.
3. 가능하면 순수 로직은 `Pumasi.Core`에 둡니다.
4. `tests/Pumasi.Core.Tests`에 xUnit 테스트를 추가하거나 갱신합니다.
5. 로컬에서 빌드와 테스트를 실행합니다.
6. 생성된 zip을 로컬 SMAPI `Mods` 폴더에 설치합니다.
7. 싱글 플레이 동작을 테스트합니다.
8. 멀티플레이에 영향을 주는 변경은 호스트/게스트 동작을 테스트합니다.

권장 검증:

```bash
./.dotnet/dotnet test Pumasi.sln
./.dotnet/dotnet build Pumasi.sln
git diff --check
```

## 유지보수자를 위한 릴리스 메모

공개 동작을 바꿀 때는 다음을 확인하세요.

- `src/Pumasi/manifest.json` 버전을 갱신합니다.
- 사용자에게 보이는 변경이면 영어/한국어 사용자 문서와 개발자 문서를 함께 갱신합니다.
- SMAPI zip을 다시 빌드합니다.
- SMAPI가 기대한 버전을 로드하는지 확인합니다.

로컬 SDK에 따라 SMAPI analyzer의 컴파일러/분석기 버전 경고가 나타날 수 있습니다. 빌드 오류는 차단 사항으로 보고, analyzer 경고는 실제 모드 문제를 나타내는지 별도로 판단하세요.
