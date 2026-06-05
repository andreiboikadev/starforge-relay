# Current Status

Last updated: 2026-06-05
Updated by: Claude Code (T10 done)
Branch/context: on **`feature/reactor-spawn`** (off `dev`). **`T07`/`T08`/`T09` ✅ merged** (#6/#7/#8). **`T10` ✅ done & verified** — `Reactor` (core + 4 feeder pads + 3 ports) + `ShardSpawner` (fills 4 colour-distributed shards) + `ShardMotion` (idle bob/spin + lerp-back; wrong-insert eject = **instant** return-to-pad) + `PortSocket` eject→return. EditMode **75/75 this session**, restricted-API clean, MCP-Play spawn verified, **human smoke passed** (wrong→pad, correct→stays socketed, drop→returns). **Code ready to commit → PR → `dev`** (the T10 brief was committed separately first). Next **`T11`** — round-loop wiring. Brief: [`../tasks/T10-reactor-spawn.md`](../tasks/T10-reactor-spawn.md).

> **This file is a state snapshot, not a changelog.** Where-we-are / blockers / what's-next live here.
> Per-task detail lives in the `Tnn` briefs ("What was actually done"); the full task map in
> [`../tasks/README.md`](../tasks/README.md); commit/merge history in git. Don't duplicate those here.

## Where we are

- **M0 — engine setup:** ✅ OpenXR + XRI rig, Android/Quest config, **verified on a real Quest 2** (see
  [ADR 0001](../architecture/adr/0001-tech-baseline.md)).
- **M1 — pure rules:** ✅ **complete & merged to `dev`** (`T01`–`T06`; T06 = PR #5).
- **M2 — VR slice:** 🟡 in progress — **`T07` ✅ (#6)**, **`T08` ✅ (#7)**, **`T09` ✅ (#8)**, **`T10` ✅** (reactor core + 4 feeder pads + spawner + return-to-pad; verified incl. human smoke — **code pending commit/PR**); **next `T11`** — round-loop wiring (consume/respawn + expiry/lifetime + rules/events).
- **M2–M6:** not started (VR slice → wiring → feedback → art → device). Full matrix + per-task scope:
  [`../tasks/README.md`](../tasks/README.md).
- EditMode suite **green 75/75, re-run this session** (T10 added no new pure rule — adapter-only; planning logic is T05, already
  covered). XRI in C# stays confined to `PortSocket` + `ShardMotion` (verified by the API grep).
- **C# code style adopted & enforced:** `docs/architecture/csharp-style.md` (from the upstream package) + a
  repo-root `.editorconfig`; the M1 pure-rule classes + `RoundConfig` were conformed (`_camelCase` fields, no
  `this.`). New code must follow it.

## Still to build / watch

- Scene now has **grab (T08) + 3 colour-validating sockets (T09) + reactor core, 4 feeder pads, and a working
  spawner with return-to-pad (T10)**. Shard **lifetime/expiry + consume-on-accept + scoring/event wiring** = **T11**.
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

- **First:** commit the **T10 code** (human; proposed message in the brief) → PR `feature/reactor-spawn` → `dev` (pattern #6/#7/#8). Then read `CLAUDE.md` + this file and **author the `T11` brief** (matrix row is still `·`).
- **Branch for T11:** `feature/round-loop-slice` off `dev` **before any edits** (assistant: run `git status`; if on `dev`, STOP and ask to branch).
- **`T11` carry-forward (what it must own):** wire `PortSocket.InsertEvaluated` (T09) + `ShardSpawner` (T10) into `RoundController` (T06) + typed `CorrectInsert`/`WrongInsert`/`ComboMilestone`/round-end events; **consume** an accepted shard → beam → pool → free pad → respawn (extend `ShardSpawner` with the `Despawn`/`planner.Release`/`NextRespawnDelay` loop it deliberately left unbuilt); shard **lifetime** (14→12 s after 10 accepts) + **expiry** (fizzle, +1 heat, combo-reset-if-held); **reset `ShardMotion` state on pool release** (else a reused shard keeps a stale home/return-timer). **Do NOT re-break the wrong-insert eject:** `ShardMotion.ReturnToPad()` is an **instant teleport** on purpose — a gradual lerp lets the socket's `keepSelectedTargetValid` re-snap the shard (the bug fixed in T10). Ports stay on **Default**; named "Shard" layer + rig-mask separation → **T14**.
- **Style covers ALL first-party C#** — runtime **and** tests (`.editorconfig` / IDE1006). New C#: `_camelCase` fields (static `s_camelCase`), `PascalCase` types/methods/properties/consts; C# 9 (block namespace, no `record`/`init`), no `#nullable enable`.
- **Per-mechanic test gate** (guardrails §17): pure rules ship EditMode tests in-change and the full suite
  re-runs green; interaction mechanics (M2+) also need a smoke / device pass. Pure-rule tasks need no device pass.
- **Hard rule — no git writes:** propose a commit message; the human commits. Read-only git only.
- Verify any XR API against the installed **XRI 3.3.0 / OpenXR 1.16** packages before relying on it.
- **In-Editor XR testing is enabled** (OpenXR on Android + Standalone): Quest Link Play and the XR Device
  Simulator work in the Editor; a device **Build And Run** stays the final validation.
