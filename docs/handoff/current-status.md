# Current Status

Last updated: 2026-06-04
Updated by: Claude Code (session handoff — M1 complete)
Branch/context: on `dev`, in sync with `origin/dev`. M1 merged via PRs (T06 = #5). **Next task starts on its own `feature/<slug>` branch off `dev` — create + switch BEFORE editing; PR back to `dev`.**

> **This file is a state snapshot, not a changelog.** Where-we-are / blockers / what's-next live here.
> Per-task detail lives in the `Tnn` briefs ("What was actually done"); the full task map in
> [`../tasks/README.md`](../tasks/README.md); commit/merge history in git. Don't duplicate those here.

## Where we are

- **M0 — engine setup:** ✅ OpenXR + XRI rig, Android/Quest config, **verified on a real Quest 2** (see
  [ADR 0001](../architecture/adr/0001-tech-baseline.md)).
- **M1 — pure rules:** ✅ **complete & merged to `dev`** (`T01`–`T06`; T06 = PR #5). → **next: M2 / `T07`** — strip the Starter-Assets Locomotion branch + rename the scene off `SampleScene`; then (T08+) shard grab / port sockets / spawner in-scene.
- **M2–M6:** not started (VR slice → wiring → feedback → art → device). Full matrix + per-task scope:
  [`../tasks/README.md`](../tasks/README.md).
- EditMode suite **green (68/68), re-verified this session** (T06 added 16; Console clean; restricted-API + objective style checks clean). All
  gameplay so far is **pure C# under `Assets/_Project/`** (`StarforgeRelay.Runtime` +
  `StarforgeRelay.Tests.EditMode` asmdefs); no scene gameplay / MonoBehaviours yet.
- **C# code style adopted & enforced:** `docs/architecture/csharp-style.md` (from the upstream package) + a
  repo-root `.editorconfig`; the M1 pure-rule classes + `RoundConfig` were conformed (`_camelCase` fields, no
  `this.`). New code must follow it.

## Still to build / watch

- Scene interaction (grab / sockets / spawn) begins at **M2 (T07+)** — nothing in the scene yet.
- The rig still includes the Starter Assets **Locomotion** branch — strip it in **T07** (no locomotion in MVP).
- The scene is still the URP default name `Assets/_Project/Scenes/SampleScene.unity` — **rename in T07**
  (name TBD); fix the vestigial `ProjectSettings.asset → templateDefaultScene` (old path; no build impact) in
  the same pass.

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
