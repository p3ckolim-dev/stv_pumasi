# pumasi 사용자 가이드

퀵링크: [홈](../README.md) | 사용자: [English](user-en.md) / [한국어](user-ko.md) | 개발자: [English](developer-en.md) / [한국어](developer-ko.md)

`pumasi` (`pms`, 한글 이름: `품앗이`)는 Stardew Valley용 SMAPI 모드 프로토타입입니다. 질문에 답하고, 투두 목록을 관리하고, 제한된 범위의 안전한 반복 농장 작업을 수행하는 도우미를 제공합니다.

현재 모드 버전: `0.1.22`

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

설치 후 SMAPI로 게임을 실행했을 때 로드된 모드 목록에 `pumasi 0.1.22`가 보이면 정상입니다.

## 업데이트

SMAPI의 `UpdateKeys`는 자동 업데이트가 아니라 업데이트 알림 기능입니다. SMAPI 콘솔에 `You can update 1 mod`가 보여도 모드 파일이 자동으로 교체되지는 않습니다.

업데이트하려면 GitHub Releases에서 최신 `Pumasi.x.x.x.zip`을 다운로드한 뒤 기존 `Mods/Pumasi` 폴더에 덮어쓰거나, 기존 `Pumasi` 폴더를 지우고 새 zip을 다시 압축 해제하세요. 멀티플레이에서는 호스트와 게스트가 같은 버전을 설치하는 것이 좋습니다.

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

## 인게임 빠른 설정

ESC 메뉴를 열면 기본 메뉴 탭 왼쪽에 `P`로 표시된 Pumasi 탭이 추가됩니다. 이 탭은 Generic Mod Config Menu가 없어도 사용할 수 있는 빠른 설정 화면입니다.

현재 Pumasi 탭에서 바꿀 수 있는 항목:

- 언어: 한국어 / 영어.
- 투두 HUD 아이콘 표시.
- 도우미 알림 표시.
- 작물, 기계, 동물, 상자, 씨앗 심기 작업 카테고리.
- 한국어 위키 답변 사용 여부.

각 설정 행에는 현재 할 수 있는 일에 대한 짧은 설명이 함께 표시됩니다.

- 작물 작업: 마른 작물 물주기, 다 자란 작물 수확, 스프링클러 주변 일반 땅 파기.
- 기계 작업: 완료된 기계 생산품 수거.
- 동물 작업: 건초 리필, 동물 쓰다듬기, 바닥 동물 생산품 수거.
- 상자 작업: 준비 중.
- 씨앗 심기 작업: 준비 중.

언어 설정은 설정 화면, 투두 보드, 채팅 명령 피드백, HUD 알림, GMCM 설정 이름 같은 Pumasi 자체 UI 문구에 적용됩니다. Gemini가 직접 만든 답변이나 위키 원문에서 온 내용은 모델/위키 결과를 그대로 표시할 수 있습니다.

항목이 화면보다 많으면 마우스 휠이나 오른쪽 스크롤바로 아래 항목을 볼 수 있습니다. Pumasi 탭은 바닐라 탭 줄의 왼쪽에 붙고, 화면 밖으로 나가지 않도록 안전 여백 안으로 보정됩니다.

설정은 클릭 즉시 `Mods/Pumasi/config.json`에 저장됩니다. 멀티플레이 게스트는 언어와 로컬 UI 설정만 바꿀 수 있고, 작업 카테고리와 위키 답변 같은 호스트 권한 설정은 호스트가 바꿔야 실제 실행에 반영됩니다.

도우미 이름, 행동 규칙, Gemini API 키 같은 텍스트 설정은 현재 버전에서는 Generic Mod Config Menu, `config.json`, 또는 SMAPI 콘솔의 `pms_key`를 사용합니다.

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

`pms_ask`와 `/pms`는 명확한 농장 작업 요청과 명확한 Stardew Valley 정보 질문을 먼저 구분합니다. 그 외의 일반 대화나 애매한 말은 JSON 라우터를 거치지 않고 Gemini가 최근 대화와 현재 투두를 보고 바로 자연스럽게 답합니다.

- 농장 작업 요청은 Gemini 작업 계획기를 사용합니다.
- Gemini가 추가할 수 있는 작업은 호스트가 현재 스캔한 안전 후보 작업과 일치하는 작업으로 제한됩니다.
- Stardew Valley 정보 질문은 한국어 Stardew Valley Wiki를 검색한 뒤, Gemini가 검색된 문맥만 기반으로 답변합니다.
- 자연어 질문은 먼저 핵심 검색어 후보로 정리됩니다. 예를 들어 `딸기 씨앗은 어디서 사?`는 `딸기 씨앗`으로도 다시 검색합니다.
- 품앗이 자체에 대한 질문, 인사, 감사, 짧은 반응, 맥락 의존 입력은 최근 대화와 투두를 기반으로 바로 대화 답변을 만듭니다.
- 대화 범위는 Stardew Valley, 현재 농장, 투두, 멀티플레이 농장일, 품앗이 도우미에 한정됩니다.
- 범위를 벗어난 질문은 해당 주제에 답하지 않고 농장 도움 쪽으로 짧게 돌려 말합니다.

예시:

```text
/pms 딸기 씨앗은 어디서 사?
/pms 아비게일이 좋아하는 선물은 뭐야?
/pms ask 온실 수확해줘
```

## 대화 컨텍스트

품앗이는 호스트 기준으로 최근 사용자/도우미 대화를 최대 12턴까지 기억합니다. 이 기록은 세이브별 SMAPI save data에 저장되어 같은 세이브를 다시 열었을 때도 이어집니다.

이 기능 때문에 아래처럼 이어서 말할 수 있습니다.

```text
/pms 딸기 씨앗은 어디서 사?
/pms 그거 가격은?
/pms 방금 말한 걸로 해줘
```

멀티플레이에서는 호스트가 이 컨텍스트를 보관하고, 게스트 요청도 호스트의 공용 대화 흐름에 들어갑니다. Gemini API 키는 이 대화 저장 데이터에 포함되지 않습니다.

위키 기반 답변은 기본적으로 아래 위키를 사용합니다.

```text
https://ko.stardewvalleywiki.com
```

답변은 채팅창에 아래처럼 표시됩니다.

```text
Pumasi: 딸기 씨앗은 봄 달걀 축제에서 살 수 있어요.
```

위키 검색에 사용한 출처 정보는 내부 처리에는 남지만, 인게임 답변과 콘솔 로그에는 표시하지 않습니다.

## 투두와 자동화

현재 스캐너가 지원하는 범위:

- 농장 작물.
- 온실 작물.
- 지원 위치의 준비된 기계.
- 스프링클러 주변의 아직 파지 않은 일반 땅.
- 동물 건물의 건초 리필 후보. `Animals` 작업 카테고리가 켜져 있어야 합니다.
- 로드된 동물 건물에서 아직 쓰다듬지 않은 동물 후보. `Animals` 작업 카테고리가 켜져 있어야 합니다.
- 로드된 동물 건물 바닥의 동물 생산품 후보. `Animals` 작업 카테고리가 켜져 있어야 합니다.

현재 실행기가 지원하는 범위:

- 마른 작물 물주기.
- 다 자란 작물 수확.
- 준비된 기계 수거.
- 스프링클러 주변 일반 땅 파기.
- 동물 건물 건초 리필.
- 아직 쓰다듬지 않은 동물 쓰다듬기.
- 바닥에 놓인 동물 생산품 수거.

바닥 동물 생산품 보관은 보수적으로 처리합니다. 품앗이는 먼저 같은 아이템과 같은 품질이 이미 들어 있는 일반 상자를 찾고, 그 상자가 전체 스택을 받을 수 있으면 그곳에 보관합니다. 맞는 상자가 없으면 호스트 인벤토리에 넣습니다. 호스트 인벤토리도 받을 수 없으면 생산품을 그대로 두고 작업을 건너뜁니다.

품앗이는 실제 농장 일꾼 캐릭터가 아니라 호스트 권한으로 작업합니다. 아이템과 바닐라 게임이 부여하는 부가 효과는 호스트에게 귀속됩니다. 현재 버전에서는 품앗이가 XP를 직접 지급하거나 게스트에게 XP/아이템을 분배하지 않습니다.

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

HUD에는 기본적으로 작은 `P` 아이콘만 표시됩니다. 아이콘을 클릭하면 투두 보드가 팝업으로 열리고, 다시 클릭하면 닫힙니다. 활성 투두가 있으면 아이콘 오른쪽 위에 개수가 표시됩니다.

투두 보드와 `/pms todo`는 같은 문구 형식을 사용합니다. 각 행에는 상태, 작업 종류, 위치/타일, 우선순위, 출처, 현재 이유 또는 최종 결과 이유가 표시됩니다. 완료, 건너뜀, 실패한 작업은 어떤 일이 있었는지 확인할 수 있도록 활성 작업 아래에 잠깐 남아 있을 수 있습니다.

호스트는 열린 투두 보드의 각 항목 오른쪽에 있는 `^` / `v` 버튼을 클릭해서도 순서를 올리거나 내릴 수 있습니다. 게스트는 보드를 볼 수 있지만 순서는 바꿀 수 없습니다.

작업 카테고리도 호스트가 채팅이나 SMAPI 콘솔에서 바꿀 수 있습니다.

```text
/pms animals on
/pms animals off
/pms work animals on
pms_work animals on
```

현재 한계:

- 아직 바닐라 NPC 수준의 일정, 호감도, 선물, 이벤트, 사회적 행동은 없습니다.
- 씨앗 심기, 판매, 파괴, 희귀 아이템 이동, 우유 짜기/양털 깎기 같은 도구 필요 동물 생산품 수거, 일반 상자 관리는 아직 실행 범위가 아닙니다.
- 일부 설정은 이후 기능 확장을 위해 먼저 존재하며, 해당 실행기가 아직 없을 수 있습니다.

## 문제 해결

- SMAPI가 Generic Mod Config Menu가 없다고 표시해도 Pumasi 메뉴 탭, `config.json`, SMAPI 콘솔 명령어, `/pms` 채팅 명령어로 사용할 수 있습니다.
- Gemini 답변이 실패하면 `pms_status`를 실행해서 `geminiConfigured=True`인지 확인하세요.
- 게스트 명령이 반응하지 않으면 호스트와 게스트 모두 같은 pumasi 모드를 설치했는지 확인하세요.
- 업데이트 후 채팅 명령이 작동하지 않으면 Stardew Valley/SMAPI를 재시작해서 새 DLL을 로드하세요.
- API 키를 실수로 채팅에 입력했다면 Google AI Studio에서 해당 키를 재발급하거나 삭제하세요.
