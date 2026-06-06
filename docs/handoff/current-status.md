# Current Status

Last updated: 2026-06-06
Updated by: Claude Code (T11b authoring)
Branch/context: **`T07`–`T11` ✅ merged to `dev`** (#6–#10) — the scene now runs the first full playable round (grab → 3 colour-validating sockets → reactor/feeders/spawner → round-loop wiring + consume/respawn + finalized score). Active task **`T11b`** (shard lifetime + expiry — the last M2-slice piece) on **`feature/shard-lifetime`** (off `dev`): **brief authored; implementation pending**. Brief: [`../tasks/T11b-shard-lifetime.md`](../tasks/T11b-shard-lifetime.md). EditMode **78/78 green this session** (re-run via MCP), Console clean. **Next:** implement T11b (pure `ShardLifetime` + spawner countdown → `RoundController.ApplyExpired`), then `T12` (composition root).

> **This file is a state snapshot, not a changelog.** Where-we-are / blockers / what's-next live here.
> Per-task detail lives in the `Tnn` briefs ("What was actually done"); the full task map in
> [`../tasks/README.md`](../tasks/README.md); commit/merge history in git. Don't duplicate those here.

## Where we are

- **M0 — engine setup:** ✅ OpenXR + XRI rig, Android/Quest config, **verified on a real Quest 2** (see
  [ADR 0001](../architecture/adr/0001-tech-baseline.md)).
- **M1 — pure rules:** ✅ **complete & merged to `dev`** (`T01`–`T06`; T06 = PR #5).
- **M2 — VR slice:** 🟡 — **`T07`–`T11` ✅ merged** (#6–#10): first **playable round** in-scene (round-loop adapter wires ports + spawner → `RoundController`, consume/respawn, events, `End()` finalization). **`T11b` 🟡** (shard lifetime + expiry — completes the slice; brief authored, implementation pending). Then M3 wiring (`T12` composition root → `T13` state machine → `T14` UI).
- **M3–M6:** not started (wiring → feedback → art → device). Full matrix + per-task scope:
  [`../tasks/README.md`](../tasks/README.md).
- EditMode suite **green 78/78, re-run this session** (T11 added 3 `RoundController.End()` finalization cases; no
  regression). XRI in C# stays confined to `PortSocket` + `ShardMotion` (verified by the API grep).
- **C# code style adopted & enforced:** `docs/architecture/csharp-style.md` (from the upstream package) + a
  repo-root `.editorconfig`; the M1 pure-rule classes + `RoundConfig` were conformed (`_camelCase` fields, no
  `this.`). New code must follow it.

## Still to build / watch

- Scene now has the **first full playable round (T11)**: grab (T08) + 3 colour-validating sockets (T09) +
  reactor core / 4 feeder pads / spawner with return-to-pad (T10) + round-loop wiring + consume/respawn + score
  finalization (T11). Remaining in M2: shard **lifetime/expiry** = **`T11b`**.
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

- **First:** implement **`T11b`** per its brief ([`../tasks/T11b-shard-lifetime.md`](../tasks/T11b-shard-lifetime.md)) on `feature/shard-lifetime` — pure `ShardLifetime` (EditMode-tested) + spawner-driven countdown + expiry → `RoundController.ApplyExpired`. Authoring commit (brief + matrix + this file) lands first, then the implementation commit → PR `dev`.
- **Then `T12`** (composition root): centralise service/`RoundController`/pool creation + `Prewarm` + **`ShardPool.Dispose` on teardown** (a leak notice fires on Play-stop) + the T11b `Func<int> accepts` seam, on `feature/composition-root`.
- **Carry-forward reminders:** **don't re-break the wrong-insert eject** — `ShardMotion.ReturnToPad()` is an **instant teleport** (a gradual lerp lets the socket's `keepSelectedTargetValid` re-snap the shard, the T10 bug). The dev `[Round]` logs in `RoundLoopController` are temporary — **HUD consumes those events at T14** (remove them then). Ports stay on **Default**; named "Shard" layer + rig-mask separation → **T14**. Spawn-colour variety within a round is a planner **tuning candidate (T21)**, not a bug.
- **Style covers ALL first-party C#** — runtime **and** tests (`.editorconfig` / IDE1006). New C#: `_camelCase` fields (static `s_camelCase`), `PascalCase` types/methods/properties/consts; C# 9 (block namespace, no `record`/`init`), no `#nullable enable`.
- **Per-mechanic test gate** (guardrails §17): pure rules ship EditMode tests in-change and the full suite
  re-runs green; interaction mechanics (M2+) also need a smoke / device pass. Pure-rule tasks need no device pass.
- **Hard rule — no git writes:** propose a commit message; the human commits. Read-only git only.
- Verify any XR API against the installed **XRI 3.3.0 / OpenXR 1.16** packages before relying on it.
- **In-Editor XR testing is enabled** (OpenXR on Android + Standalone): Quest Link Play and the XR Device
  Simulator work in the Editor; a device **Build And Run** stays the final validation.
