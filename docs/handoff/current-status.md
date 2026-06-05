# Current Status

Last updated: 2026-06-05
Updated by: Claude Code (T09 brief)
Branch/context: **T08 ✅ merged to `dev` (#7)**, tree clean. **`T09` brief authored** — branch `feature/port-socket`; **awaiting validation, then implement on command.** Brief: [`../tasks/T09-port-socket.md`](../tasks/T09-port-socket.md). EditMode **75/75** (M1 + T08). (T06 = #5, T07 = #6, T08 = #7.)

> **This file is a state snapshot, not a changelog.** Where-we-are / blockers / what's-next live here.
> Per-task detail lives in the `Tnn` briefs ("What was actually done"); the full task map in
> [`../tasks/README.md`](../tasks/README.md); commit/merge history in git. Don't duplicate those here.

## Where we are

- **M0 — engine setup:** ✅ OpenXR + XRI rig, Android/Quest config, **verified on a real Quest 2** (see
  [ADR 0001](../architecture/adr/0001-tech-baseline.md)).
- **M1 — pure rules:** ✅ **complete & merged to `dev`** (`T01`–`T06`; T06 = PR #5).
- **M2 — VR slice:** 🟡 in progress — **`T07` ✅ (#6)**, **`T08` ✅ (#7)** (shard prefab + grab + `ShardPool`); **`T09` 🟡 brief** — ports = accept-any `XRSocketInteractor` + validate colour in code; then T10 (core + feeder pads + spawner), T11 (round-loop wiring) in-scene.
- **M2–M6:** not started (VR slice → wiring → feedback → art → device). Full matrix + per-task scope:
  [`../tasks/README.md`](../tasks/README.md).
- EditMode suite **green (75/75), re-verified this session** (T08 added 7 `ShardPool` tests; restricted-API clean). T08 adds the
  **first MonoBehaviours** (`ShardView`, `ShardPool`) + the first prefab/material under `Assets/_Project/`; the M1 pure rules are
  unchanged and the `StarforgeRelay.Runtime` asmdef stays **XR-free** (XRI enters at T09).
- **C# code style adopted & enforced:** `docs/architecture/csharp-style.md` (from the upstream package) + a
  repo-root `.editorconfig`; the M1 pure-rule classes + `RoundConfig` were conformed (`_camelCase` fields, no
  `this.`). New code must follow it.

## Still to build / watch

- Scene interaction (grab / sockets / spawn) begins at **T08+** — nothing interactive in the scene yet.
- **Env note (scene/rig tasks):** `execute_code` is broken on **both** dev machines (CodeDom `mono.exe`
  "filename or extension is too long"; no Roslyn) — re-verified on the work machine 2026-06-05; use
  structural MCP tools. Prefab **unpack** is a manual 1-click editor step; the MCP asset-rename tool reports
  "failed" but succeeds on disk (verify via filesystem).
- **MCP gotchas (scene/prefab tasks, found T08):** `refresh_unity scope=scripts` recompiles but does **not
  import new files** (new scripts/tests look "missing", tests run a stale count → use `scope=all`);
  `manage_gameobject create` may **silently not apply** `component_properties` on added components (set them
  via `manage_components set_property`, then **re-read to verify** — don't trust the success message).
- **Env note (XR-sim smoke):** grabbing in the **XR Device Simulator** logs 2 benign XRI errors —
  `Failed to get haptic capabilities of XRSimulatedController … Continuing assuming a single haptic channel`
  (simulated controllers have no haptics; XRI falls back). Not a code defect; absent on real Touch
  controllers — re-confirm a clean device console at **T20**.

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
- **`T09` in progress** (branch `feature/port-socket`) — ports = **accept-any** `XRSocketInteractor` + **validate colour in code** (guardrails §6), *not* colour-gated; on the **Default** interaction layer for now. Named "Shard" layer + rig-mask separation **deferred to T14/UI** (when introduced, update the interactors' mask or grab breaks). Wrong-insert eject must avoid the socket re-select loop (use `socketActive`/`allowSelect`, **not** the obsolete `SelectExit`). Needs an XR-sim/device smoke.
- **New C# follows `docs/architecture/csharp-style.md`** (enforced by the repo-root `.editorconfig`):
  non-public fields `_camelCase` (static `s_camelCase`), `PascalCase` types/methods/properties/consts.
- **Per-mechanic test gate** (guardrails §17): pure rules ship EditMode tests in-change and the full suite
  re-runs green; interaction mechanics (M2+) also need a smoke / device pass. Pure-rule tasks need no device pass.
- **Hard rule — no git writes:** propose a commit message; the human commits. Read-only git only.
- Verify any XR API against the installed **XRI 3.3.0 / OpenXR 1.16** packages before relying on it.
- **In-Editor XR testing is enabled** (OpenXR on Android + Standalone): Quest Link Play and the XR Device
  Simulator work in the Editor; a device **Build And Run** stays the final validation.
