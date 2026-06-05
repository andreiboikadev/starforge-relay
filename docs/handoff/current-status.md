# Current Status

Last updated: 2026-06-05
Updated by: Claude Code (T08 start)
Branch/context: on `feature/shard-grab` (off `dev`). **T07 ✅ merged (#6)** — Locomotion/teleport/gaze stripped, rig **unpacked** & on **Near-Far** interactors (grab on Grip), scene `StarforgeRelay`. **`T08` brief authored — awaiting validation, then implement on command.** Brief: [`../tasks/T08-shard-grab.md`](../tasks/T08-shard-grab.md). EditMode **68/68** at last run (M1). (M1 merged via PRs; T06 = #5, T07 = #6.)

> **This file is a state snapshot, not a changelog.** Where-we-are / blockers / what's-next live here.
> Per-task detail lives in the `Tnn` briefs ("What was actually done"); the full task map in
> [`../tasks/README.md`](../tasks/README.md); commit/merge history in git. Don't duplicate those here.

## Where we are

- **M0 — engine setup:** ✅ OpenXR + XRI rig, Android/Quest config, **verified on a real Quest 2** (see
  [ADR 0001](../architecture/adr/0001-tech-baseline.md)).
- **M1 — pure rules:** ✅ **complete & merged to `dev`** (`T01`–`T06`; T06 = PR #5).
- **M2 — VR slice:** 🟡 in progress — **`T07` ✅ merged (#6)** (Locomotion stripped, rig unpacked & on Near-Far, scene → `StarforgeRelay`); **`T08` 🟡 (brief stage — implement on command)** — shard prefab + grab (Grip) + ShardPool; then T09 (port sockets) / T10 (core + feeder pads + spawner) in-scene.
- **M2–M6:** not started (VR slice → wiring → feedback → art → device). Full matrix + per-task scope:
  [`../tasks/README.md`](../tasks/README.md).
- EditMode suite **green (68/68), re-verified this session** (T06 added 16; Console clean; restricted-API + objective style checks clean). All
  gameplay so far is **pure C# under `Assets/_Project/`** (`StarforgeRelay.Runtime` +
  `StarforgeRelay.Tests.EditMode` asmdefs); no scene gameplay / MonoBehaviours yet.
- **C# code style adopted & enforced:** `docs/architecture/csharp-style.md` (from the upstream package) + a
  repo-root `.editorconfig`; the M1 pure-rule classes + `RoundConfig` were conformed (`_camelCase` fields, no
  `this.`). New code must follow it.

## Still to build / watch

- Scene interaction (grab / sockets / spawn) begins at **T08+** — nothing interactive in the scene yet.
- **Env note (scene/rig tasks):** `execute_code` is broken on **both** dev machines (CodeDom `mono.exe`
  "filename or extension is too long"; no Roslyn) — re-verified on the work machine 2026-06-05; use
  structural MCP tools. Prefab **unpack** is a manual 1-click editor step; the MCP asset-rename tool reports
  "failed" but succeeds on disk (verify via filesystem).

## Blockers

- None.

## Decisions

- Tech baseline: [ADR 0001](../architecture/adr/0001-tech-baseline.md) — Unity 6.3 LTS, URP, OpenXR 1.16 +
  XRI 3.3.0 + Meta Quest + Oculus Touch, Single Pass Instanced, Vulkan, IL2CPP/ARM64, manual DI, single
  scene, Floor origin, pooling via `ObjectPool<T>`.
- In-repo docs are the single source of truth.
- **C# code style** = the package convention (.NET-runtime naming: `_camelCase` / `s_camelCase` fields,
  `PascalCase` types/methods/properties/consts), home in `docs/architecture/csharp-style.md`, enforced by the
  repo-root `.editorconfig` (rule `IDE1006`) — recorded in [ADR 0002](../architecture/adr/0002-csharp-style.md).

## Notes for next chat

- Read `CLAUDE.md` + this file + the relevant `Tnn` brief first.
- **Branch first:** create + switch to `feature/<slug>` off `dev` **before any edits**; PR → `dev` (pattern: #3/#4/#5) — never commit straight to `dev`. (Assistant: at task start run `git status`; if on `dev`, STOP and ask to branch.)
- **Style check covers ALL first-party C#** — runtime **and** tests (`.editorconfig` / IDE1006). A test-file `s_` violation slipped past a runtime-only check this session; don't repeat.
- **T07 is the first scene/rig task** → needs a smoke / XR-Simulator (and Quest where grab feel matters) pass per §17, not just EditMode.
- **New C# follows `docs/architecture/csharp-style.md`** (enforced by the repo-root `.editorconfig`):
  non-public fields `_camelCase` (static `s_camelCase`), `PascalCase` types/methods/properties/consts.
- **Per-mechanic test gate** (guardrails §17): pure rules ship EditMode tests in-change and the full suite
  re-runs green; interaction mechanics (M2+) also need a smoke / device pass. Pure-rule tasks need no device pass.
- **Hard rule — no git writes:** propose a commit message; the human commits. Read-only git only.
- Verify any XR API against the installed **XRI 3.3.0 / OpenXR 1.16** packages before relying on it.
- **In-Editor XR testing is enabled** (OpenXR on Android + Standalone): Quest Link Play and the XR Device
  Simulator work in the Editor; a device **Build And Run** stays the final validation.
