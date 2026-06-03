# Current Status

Last updated: 2026-06-03
Updated by: Claude Code (T03 — round timer; status doc slimmed)
Branch/context: `feature/round-timer` (off `dev`; human reviews and commits)

> **This file is a state snapshot, not a changelog.** Where-we-are / blockers / what's-next live here.
> Per-task detail lives in the `Tnn` briefs ("What was actually done"); the full task map in
> [`../tasks/README.md`](../tasks/README.md); commit/merge history in git. Don't duplicate those here.

## Where we are

- **M0 — engine setup:** ✅ OpenXR + XRI rig, Android/Quest config, **verified on a real Quest 2** (see
  [ADR 0001](../architecture/adr/0001-tech-baseline.md)).
- **M1 — pure rules:** `T01` ✅ · `T02` ✅ · `T03` ✅ → **next `T04` PortValidationService** → `T05` · `T06`.
- **M2–M6:** not started (VR slice → wiring → feedback → art → device). Full matrix + per-task scope:
  [`../tasks/README.md`](../tasks/README.md).
- EditMode suite **green (33/33)** at last close. All gameplay so far is **pure C# under `Assets/_Project/`**
  (`StarforgeRelay.Runtime` + `StarforgeRelay.Tests.EditMode` asmdefs); no scene gameplay / MonoBehaviours yet.

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

## Notes for next chat

- Read `CLAUDE.md` + this file + the relevant `Tnn` brief first.
- **Per-mechanic test gate** (guardrails §17): pure rules ship EditMode tests in-change and the full suite
  re-runs green; interaction mechanics (M2+) also need a smoke / device pass. Pure-rule tasks need no device pass.
- **Hard rule — no git writes:** propose a commit message; the human commits. Read-only git only.
- Verify any XR API against the installed **XRI 3.3.0 / OpenXR 1.16** packages before relying on it.
- **In-Editor XR testing is enabled** (OpenXR on Android + Standalone): Quest Link Play and the XR Device
  Simulator work in the Editor; a device **Build And Run** stays the final validation.
