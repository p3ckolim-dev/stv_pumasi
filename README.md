# pumasi

Quick links: [User EN](docs/user-en.md) | [User KO](docs/user-ko.md) | [Developer EN](docs/developer-en.md) | [Developer KO](docs/developer-ko.md)

`pumasi` (`pms`, Korean name: `품앗이`) is a SMAPI mod prototype for Stardew Valley. It provides a host-authoritative helper concept that can answer Stardew Valley questions, keep a shared todo list, and execute a small set of safe repetitive farm chores.

Current mod version: `0.1.21`

## Name Origin / 이름의 유래

`pumasi` is named after `품앗이`, a Korean tradition of reciprocal help, especially in farming communities. When a household did not have enough hands for seasonal work, neighbors or relatives would help with labor, and that help would later be repaid through labor in return.

The name fits this mod because the helper is intended to take part in repetitive farm chores in the same cooperative spirit: it does not replace the farm, but helps carry the work when the player or multiplayer group is short on time.

`pumasi`는 한국 전통 농촌 문화인 `품앗이`에서 따온 이름입니다. 품앗이는 농번기처럼 한 집의 일손만으로 감당하기 어려운 일이 있을 때 이웃이나 친인척이 노동을 보태고, 이후 비슷한 방식으로 그 도움을 되갚는 상호부조 관습입니다.

이 모드의 도우미도 같은 의미를 이어받아, 플레이어나 멀티플레이 그룹이 반복적인 농장일에 시간이 부족할 때 함께 일을 나누는 존재를 목표로 합니다.

References:

- [Encyclopedia of Korean Culture: Dure](https://encykorea.aks.ac.kr/Article/E0016972) describes `품앗이` as a small-scale reciprocal labor exchange based on individual choice, distinct from the more obligatory village-wide `두레`.
- [FAO Globally Important Agricultural Heritage Systems: Geumsan Traditional Ginseng System](https://www.fao.org/giahs/giahs-around-the-world/korea-geumsan-traditional-ginseng-system/en) notes `Pumasi` as a rural Korean practice of exchanging labor to help one another's farms.
- [Digital Chilgok Culture Encyclopedia: 품앗이](https://chilgok.grandculture.net/chilgok/toc/GC02301577) explains `품앗이` as a traditional method of mutual farm labor exchange and connects it to `품갚음`, repaying labor with labor.

## Documentation

Choose the page that matches what you want to do:

- [User Guide - English](docs/user-en.md): install the mod, configure Gemini, and use commands in single-player or multiplayer.
- [사용자 가이드 - 한국어](docs/user-ko.md): 모드 설치, Gemini 설정, 싱글/멀티 명령어 사용법.
- [Developer Guide - English](docs/developer-en.md): fork the repository, build/test locally, and understand the current architecture.
- [개발자 가이드 - 한국어](docs/developer-ko.md): 저장소 포크, 로컬 빌드/테스트, 현재 구조 이해.

## Scope Snapshot

- Works in single-player and multiplayer.
- In multiplayer, the host owns AI calls, task queue mutation, and world-changing behavior.
- Guests can send helper requests to the host.
- Helper answers are shown in the in-game chat without source lines.
- The HUD shows a compact Pumasi icon; clicking it opens the todo board popup.
- The todo board and `/pms todo` show task status, priority, source, target tile, and readable completion/skip/failure reasons.
- A Pumasi tab in the Stardew Valley game menu provides scrollable quick settings, including Korean/English UI language selection.
- Gemini is used for task planning and wiki-grounded answer generation.
- Stardew Valley information answers are grounded with the Korean Stardew Valley Wiki.
- Wiki questions are normalized into focused Korean Wiki search terms before lookup.
- Recent user/helper conversation is kept per save so follow-up phrases like `그거`, `방금 말한 거`, or `do that` can use context.
- Current task execution supports crop watering, crop harvesting, ready machine collection, sprinkler-area tilling, and animal building hay refill.
- SMAPI update keys notify players about new releases; installing the new zip is still manual.

This is an MVP/prototype, not a full vanilla-style NPC system yet.
