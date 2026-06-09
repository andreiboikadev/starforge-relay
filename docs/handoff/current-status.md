# Current Status

Last updated: 2026-06-09
Updated by: Claude Code (T13 authored)
Branch/context: **`T07`–`T12` ✅ merged to `dev`** (#6–#12) — the M2 slice + the T12 composition root are in. **`T13` (AppStateMachine) 🟡 authored** on `feature/app-state-machine` — brief written, implementation **not started**. T13 gates the round on an app state machine (Boot → MainMenu → Calibration → Playing ⇄ Paused → RoundComplete → Results); today the composition root auto-starts the round at scene load. Brief: [`../tasks/T13-app-state-machine.md`](../tasks/T13-app-state-machine.md). EditMode last green **85/85** (T11b/T12). **Next:** implement T13 on command.

> **This file is a state snapshot, not a changelog.** Where-we-are / blockers / what's-next live here.
> Per-task detail lives in the `Tnn` briefs ("What was actually done"); the full task map in
> [`../tasks/README.md`](../tasks/README.md); commit/merge history in git. Don't duplicate those here.

## Where we are

- **M0 — engine setup:** ✅ OpenXR + XRI rig, Android/Quest config, **verified on a real Quest 2** (see
  [ADR 0001](../architecture/adr/0001-tech-baseline.md)).
- **M1 — pure rules:** ✅ **complete & merged to `dev`** (`T01`–`T06`; T06 = PR #5).
- **M2 — VR slice:** ✅ complete & merged — **`T07`–`T11b`** (#6–#11): the slice is whole — grab → colour-validate → correct / wrong / **expired** → win / overload / time-out. Now **M3 wiring** (`T12` composition root → `T13` state machine → `T14` UI).
- **M3 — wiring:** 🟡 — **`T12` ✅** (composition root): one `StarforgeRelayCompositionRoot` owns construction +
  pool lifecycle; adapters take their deps via `Initialize`. Next `T13` (state machine) → `T14` (UI). M4–M6 not
  started (feedback → art → device). Full matrix: [`../tasks/README.md`](../tasks/README.md).
- EditMode suite **green 85/85 this session** (T11b added 6 `ShardLifetime` cases + a `RoundController`
  expired→overload case; no regression). XRI in C# stays confined to `PortSocket` + `ShardMotion` (API grep).
- **C# code style adopted & enforced:** `docs/architecture/csharp-style.md` (from the upstream package) + a
  repo-root `.editorconfig`; the M1 pure-rule classes + `RoundConfig` were conformed (`_camelCase` fields, no
  `this.`). New code must follow it.

## Still to build / watch

- Scene runs the **complete M2 slice**: grab (T08) + 3 colour-validating sockets (T09) + reactor core /
  4 feeder pads / spawner with return-to-pad (T10) + round-loop wiring + consume/respawn + score finalization
  (T11) + shard **lifetime/expiry** (T11b). Nothing left in M2; M3 wiring underway (`T12` ✅, `T13` next).
- **Env note (scene/rig tasks):** `execute_code` is broken on **both** dev machines (CodeDom `mono.exe`
  "filename or extension is too long"; no Roslyn) — re-verified on the work machine 2026-06-05; use
  structural MCP tools. Prefab **unpack** is a manual 1-click editor step; the MCP asset-rename tool reports
  "failed" but succeeds on disk (verify via filesystem).
- **MCP gotchas (scene/prefab tasks, T08 + T10):** `refresh_unity scope=scripts` recompiles but does **not
  import new files** (use `scope=all`); `manage_gameobject create` **silently ignores** `component_properties`
  **and** `components_to_remove` (set/remove via `manage_components` + **re-read to verify**); a **component-array**
  serialized ref must be set with `[{"instanceID":…}]` objects (bare ints resolve to `null`); `create
  save_as_prefab` leaves a **stray temp instance** in the scene (delete it); the **first Play after each
  recompile** throws a benign `routine is null` / AABB domain-reload cascade (a settled re-Play is clean), and
  for the same reason **never edit scripts while the Editor is in Play**.
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

- **First:** commit the **T13 brief** (this authoring change: brief + matrix row 🟡 + this status note), then **implement T13** on `feature/app-state-machine` on the human's command.
- **T13 key points** (from the brief): pure `AppStateMachine` + 7 states over an `IRoundLifecycle` seam;
  lifecycle calls live in the **transition triggers** (so resume ≠ restart); **pause = `timeScale = 0` + a
  `_paused` insert-gate** (timeScale alone doesn't stop XRI select events); **Play-Again/Restart rebuilds the
  round via a `Func<RoundController>` factory** (no per-service reset); machine retains `LastResult` for the
  Results snapshot; `TrackingLost` deferred. Scenes: none (root builds + ticks the machine; temporary
  Input-System debug keys, removed at T14).
- **Diagnosis carry-forward:** the Play-stop `Leak Detected: Persistent N allocations` / `routine is null` cascade is the **first-Play-after-recompile transient** (a settled re-Play is clean) — confirmed at T12; not a code leak.
- **Tuning (T21, on device — not now):** held-slow `0.25` (≈56 s held) is generous, and idle shards expire in a synchronized wave (equal spawn + lifetime). Both are **GDD-§14 tuning levers confirmed on device**, deliberately **not** changed speculatively in code.
- **Carry-forward reminders:** **don't re-break the wrong-insert eject** — `ShardMotion.ReturnToPad()` is an **instant teleport** (a gradual lerp lets the socket's `keepSelectedTargetValid` re-snap the shard, the T10 bug). The dev `[Round]` logs in `RoundLoopController` are temporary — **HUD consumes those events at T14** (remove them then). Ports stay on **Default**; named "Shard" layer + rig-mask separation → **T14**. Spawn-colour variety within a round is a planner **tuning candidate (T21)**, not a bug.
- **Style covers ALL first-party C#** — runtime **and** tests (`.editorconfig` / IDE1006). New C#: `_camelCase` fields (static `s_camelCase`), `PascalCase` types/methods/properties/consts; C# 9 (block namespace, no `record`/`init`), no `#nullable enable`.
- **Per-mechanic test gate** (guardrails §17): pure rules ship EditMode tests in-change and the full suite
  re-runs green; interaction mechanics (M2+) also need a smoke / device pass. Pure-rule tasks need no device pass.
- **Hard rule — no git writes:** propose a commit message; the human commits. Read-only git only.
- Verify any XR API against the installed **XRI 3.3.0 / OpenXR 1.16** packages before relying on it.
- **In-Editor XR testing is enabled** (OpenXR on Android + Standalone): Quest Link Play and the XR Device
  Simulator work in the Editor; a device **Build And Run** stays the final validation.
