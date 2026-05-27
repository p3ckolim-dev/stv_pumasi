# pumasi

Quick links: [User EN](docs/user-en.md) | [User KO](docs/user-ko.md) | [Developer EN](docs/developer-en.md) | [Developer KO](docs/developer-ko.md)

`pumasi` (`pms`, Korean name: `품앗이`) is a SMAPI mod prototype for Stardew Valley. It provides a host-authoritative helper concept that can answer Stardew Valley questions, keep a shared todo list, and execute a small set of safe repetitive farm chores.

Current mod version: `0.1.3`

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
- Helper answers are shown in the in-game chat.
- Gemini is used for task planning and wiki-grounded answer generation.
- Stardew Valley information answers are grounded with the Korean Stardew Valley Wiki.
- Current task execution supports crop watering, crop harvesting, and ready machine collection.

This is an MVP/prototype, not a full vanilla-style NPC system yet.
