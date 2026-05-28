# pumasi 사용자 가이드

퀵링크: [홈](../README.md) | 사용자: [English](user-en.md) / [한국어](user-ko.md) | 개발자: [English](developer-en.md) / [한국어](developer-ko.md)

`pumasi` (`pms`, 한글 이름: `품앗이`)는 Stardew Valley용 SMAPI 모드 프로토타입입니다. 질문에 답하고, 투두 목록을 관리하고, 제한된 범위의 안전한 반복 농장 작업을 수행하는 도우미를 제공합니다.

현재 모드 버전: `0.1.8`

## 이 문서의 대상

이 문서는 모드를 설치해서 사용하는 플레이어를 위한 문서입니다. 저장소를 포크해서 수정하거나 기능을 개발하려면 [개발자 가이드](developer-ko.md)를 확인하세요.

## 요구 사항

- Stardew Valley 1.6.
- SMAPI 4.x.
- Stardew Valley `Mods` 폴더에 설치된 `pumasi` 모드.
- AI 계획이나 위키 답변 요약을 사용하려면 Gemini API 키.
- 선택 사항: 인게임 설정을 위한 Generic Mod Config Menu.

## 설치

모드 zip을 다운로드하거나 빌드한 뒤 Stardew Valley `Mods` 폴더에 압축을 풉니다. 최종 폴더 구조는 아래처럼 되어야 합니다.

```text
Stardew Valley/
  Mods/
    Pumasi/
      manifest.json
      Pumasi.dll
      Pumasi.Core.dll
      assets/
```

Steam 기준 일반적인 위치는 다음과 같습니다.

```text
Windows: C:\Program Files (x86)\Steam\steamapps\common\Stardew Valley\Mods
macOS:   ~/Library/Application Support/Steam/steamapps/common/Stardew Valley/Contents/MacOS/Mods
Linux:   ~/.local/share/Steam/steamapps/common/Stardew Valley/Mods
```

SMAPI는 시작할 때 정확한 `Mods go here:` 경로를 출력합니다. 위 예시와 다르면 SMAPI가 출력한 경로를 기준으로 설치하세요.

설치 후 SMAPI로 게임을 실행했을 때 로드된 모드 목록에 `pumasi 0.1.8`가 보이면 정상입니다.

## Gemini API 키

싱글 플레이어 또는 멀티플레이 호스트만 Gemini API 키를 설정하면 됩니다.

SMAPI 콘솔에서 설정합니다.

```text
pms_key YOUR_GEMINI_API_KEY
```

키는 로컬 SMAPI 설정 파일에 저장됩니다.

```text
Mods/Pumasi/config.json
```

보안 관련 사실:

- 키는 `config.json`에 평문으로 저장됩니다.
- 키는 멀티플레이 게스트에게 전송되지 않습니다.
- 키는 투두 스냅샷, 공유 설정 스냅샷, 도우미 답변, 로그에 포함되지 않습니다.
- `/pms_key`는 실수로 키를 채팅에 입력하지 않도록 인게임 채팅에서 거부됩니다.
- Generic Mod Config Menu가 설치되어 있으면 저장된 키는 `********`로 표시됩니다.

## 명령어

SMAPI 콘솔 명령어:

```text
pms_status
pms_scan
pms_ask <질문 또는 농장 작업 요청>
pms_key <gemini-api-key>
pms_todo [move <from> <to>|up <index>|down <index>|top <index>|bottom <index>]
pms_work animals on|off
```

인게임 채팅 명령어:

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

채팅 별칭:

```text
/pms_ask
/pms_status
/pms_scan
/pms_todo
/pms_work
```

## 싱글 플레이 사용

싱글 플레이는 지원됩니다. 로컬 플레이어가 메인 플레이어이므로 명령은 로컬에서 처리되고, 안전 작업 실행은 현재 세이브를 변경할 수 있습니다.

간단한 테스트 명령어:

```text
pms_status
pms_key YOUR_GEMINI_API_KEY
pms_ask 딸기 씨앗은 어디서 사?
pms_ask 온실 수확해줘
pms_scan
pms_todo
```

## 멀티플레이 사용

호스트가 권한을 가집니다.

- 호스트가 Gemini API 키를 저장합니다.
- 호스트가 Gemini와 한국어 Stardew Valley Wiki를 호출합니다.
- 호스트가 투두 큐를 소유합니다.
- 호스트가 월드를 변경하는 작업을 실행합니다.
- 호스트가 도우미 상태, 투두 목록, 공개 답변 텍스트를 게스트에게 브로드캐스트합니다.

게스트는 질문이나 작업 요청을 보낼 수 있습니다.

```text
/pms 딸기 씨앗은 어디서 사?
/pms ask 온실 수확해줘
```

게스트 요청은 호스트에게 전달됩니다. 게스트는 Gemini API 키가 필요 없습니다. 최종 답변은 모드를 설치한 플레이어의 인게임 채팅창에 표시됩니다.

## AI와 위키 답변

`pms_ask`와 `/pms`는 입력을 세 가지 경로로 분류합니다.

- 농장 작업 요청은 Gemini 작업 계획기를 사용합니다.
- Stardew Valley 정보 질문은 한국어 Stardew Valley Wiki를 검색한 뒤, Gemini가 검색된 문맥만 기반으로 답변합니다.
- 애매한 요청은 작업을 등록하지 않고 더 구체적으로 말해달라고 답합니다.

예시:

```text
/pms 딸기 씨앗은 어디서 사?
/pms 아비게일이 좋아하는 선물은 뭐야?
/pms ask 온실 수확해줘
```

위키 기반 답변은 기본적으로 아래 위키를 사용합니다.

```text
https://ko.stardewvalleywiki.com
```

답변은 채팅창에 아래처럼 표시됩니다.

```text
Pumasi: 딸기 씨앗은 봄 달걀 축제에서 살 수 있어요.
출처: 딸기 - https://ko.stardewvalleywiki.com/딸기
```

## 투두와 자동화

현재 스캐너가 지원하는 범위:

- 농장 작물.
- 온실 작물.
- 지원 위치의 준비된 기계.
- 스프링클러 주변의 아직 파지 않은 일반 땅.
- 동물 건물의 건초 리필 후보. `Animals` 작업 카테고리가 켜져 있어야 합니다.

현재 실행기가 지원하는 범위:

- 마른 작물 물주기.
- 다 자란 작물 수확.
- 준비된 기계 수거.
- 스프링클러 주변 일반 땅 파기.
- 동물 건물 건초 리필.

도우미는 보이는 투두 목록의 맨 위부터 한 번에 하나씩 처리합니다. 체크리스트를 위에서 아래로 체크하는 방식에 가깝습니다. 유저가 요청한 작업은 큐의 맨 아래에 추가되므로, 호스트가 순서를 조정하지 않으면 기존 작업 뒤에서 기다립니다.

매일 아침 자동화 모드가 `Off`가 아니면 Pumasi가 농장을 살펴보고 기본적으로 우선순위가 높은 안전 작업 3개 정도를 투두로 쌓습니다. 호스트는 `Assistant.MorningTodoLimit` 값으로 이 개수를 조정할 수 있습니다.

투두 순서 조정 명령은 호스트 전용입니다.

```text
/pms todo move 3 1
/pms todo up 2
/pms todo down 1
/pms todo top 4
/pms todo bottom 1
```

호스트는 투두 보드의 각 항목 오른쪽에 있는 `^` / `v` 버튼을 클릭해서도 순서를 올리거나 내릴 수 있습니다. 게스트는 보드를 볼 수 있지만 순서는 바꿀 수 없습니다.

작업 카테고리도 호스트가 채팅이나 SMAPI 콘솔에서 바꿀 수 있습니다.

```text
/pms animals on
/pms animals off
/pms work animals on
pms_work animals on
```

현재 한계:

- 아직 바닐라 NPC 수준의 일정, 호감도, 선물, 이벤트, 사회적 행동은 없습니다.
- 씨앗 심기, 판매, 파괴, 희귀 아이템 이동, 쓰다듬기/생산품 수거 같은 세부 동물 돌보기, 상자 관리는 아직 실행 범위가 아닙니다.
- 일부 설정은 이후 기능 확장을 위해 먼저 존재하며, 해당 실행기가 아직 없을 수 있습니다.

## 문제 해결

- SMAPI가 Generic Mod Config Menu가 없다고 표시해도 `config.json`, SMAPI 콘솔 명령어, `/pms` 채팅 명령어로 사용할 수 있습니다.
- Gemini 답변이 실패하면 `pms_status`를 실행해서 `geminiConfigured=True`인지 확인하세요.
- 게스트 명령이 반응하지 않으면 호스트와 게스트 모두 같은 pumasi 모드를 설치했는지 확인하세요.
- 업데이트 후 채팅 명령이 작동하지 않으면 Stardew Valley/SMAPI를 재시작해서 새 DLL을 로드하세요.
- API 키를 실수로 채팅에 입력했다면 Google AI Studio에서 해당 키를 재발급하거나 삭제하세요.
