# Current Status

Last updated: 2026-06-05
Updated by: Claude Code (T10 authored)
Branch/context: on **`dev`** (clean). **`T07`/`T08`/`T09` ✅ merged** (#6/#7/#8) — the VR slice now has the rig (locomotion stripped), a poolable grabbable shard, and 3 accept-any ports that validate colour in code + eject-on-wrong. **`T10` authored & 🟡 in progress** — reactor core + feeder pads + spawner + lerp-back (the spatial/spawn half of the slice; lifetime/expiry + rules deferred to T11). Brief: [`../tasks/T10-reactor-spawn.md`](../tasks/T10-reactor-spawn.md). **Next:** branch `feature/reactor-spawn` off `dev`, commit the T10 docs separately, then implement.

> **This file is a state snapshot, not a changelog.** Where-we-are / blockers / what's-next live here.
> Per-task detail lives in the `Tnn` briefs ("What was actually done"); the full task map in
> [`../tasks/README.md`](../tasks/README.md); commit/merge history in git. Don't duplicate those here.

## Where we are

- **M0 — engine setup:** ✅ OpenXR + XRI rig, Android/Quest config, **verified on a real Quest 2** (see
  [ADR 0001](../architecture/adr/0001-tech-baseline.md)).
- **M1 — pure rules:** ✅ **complete & merged to `dev`** (`T01`–`T06`; T06 = PR #5).
- **M2 — VR slice:** 🟡 in progress — **`T07` ✅ (#6)**, **`T08` ✅ (#7)**, **`T09` ✅ (#8)** (3 ports: accept-any socket + validate colour in code + eject); **`T10` 🟡** (brief authored) — reactor core + feeder pads + spawner + lerp-back; then T11 (round-loop wiring: consume/expiry + rules/events).
- **M2–M6:** not started (VR slice → wiring → feedback → art → device). Full matrix + per-task scope:
  [`../tasks/README.md`](../tasks/README.md).
- EditMode suite **last green at 75/75** (T09 session) — **not re-run this session** (docs-only). T10 is adapter-heavy (spawner /
  `ShardMotion` / core+pad views) with **no new pure rule expected** (planning logic is T05, already covered); it re-runs the full
  suite + an XR-sim smoke before close. XRI stays confined to the adapter set (`PortSocket`/`PortView`/`ShardView` + new `ShardMotion`).
- **C# code style adopted & enforced:** `docs/architecture/csharp-style.md` (from the upstream package) + a
  repo-root `.editorconfig`; the M1 pure-rule classes + `RoundConfig` were conformed (`_camelCase` fields, no
  `this.`). New code must follow it.

## Still to build / watch

- Scene now has **grab (T08) + 3 colour-validating sockets (T09)**; **spawn + feeder pads + reactor core land in
  T10** (in progress). Shard lifetime/expiry + scoring/event wiring come with **T11**.
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

- Read `CLAUDE.md` + this file + the **`T10`** brief ([`../tasks/T10-reactor-spawn.md`](../tasks/T10-reactor-spawn.md)) first.
- **Workflow this task:** the **T10 docs commit on their own first** (brief + this refresh + matrix + ADR-index fix), *then* implementation begins — docs commit → go.
- **Branch first:** create + switch to **`feature/reactor-spawn`** off `dev` **before any edits**; PR → `dev` (pattern #6/#7/#8) — never commit straight to `dev`. (Assistant: at task start run `git status`; if on `dev`, STOP and ask to branch.)
- **`T10` carry-forward:** scope = the matrix's "core + feeder pads + spawner + lerp-back" only; **lifetime/expiry + consume-on-accept + all RoundController rules/events → T11** (see the brief's "Scope cut"). The wrong-insert **eject becomes the real return-to-pad** (`ShardMotion.ReturnToPad()` after the **non-obsolete** deferred `SelectExit`; drop `PortSocket._ejectDistance`). Ports stay on **Default**; named "Shard" layer + rig-mask separation **deferred to T14** (when introduced, update the interactors' mask or grab breaks).
- **MCP gotchas (T08/T09):** `manage_gameobject create` may not apply `component_properties` → set via `manage_components set_property` + **re-read** to verify; `refresh scope=all` for new files; asset-rename reports "failed" but succeeds on disk; **never edit scripts while the Editor is in Play** (domain reload corrupts the live XR session).
- **Style covers ALL first-party C#** — runtime **and** tests (`.editorconfig` / IDE1006). New C#: `_camelCase` fields (static `s_camelCase`), `PascalCase` types/methods/properties/consts; C# 9 (block namespace, no `record`/`init`), no `#nullable enable`.
- **Per-mechanic test gate** (guardrails §17): pure rules ship EditMode tests in-change and the full suite
  re-runs green; interaction mechanics (M2+) also need a smoke / device pass. Pure-rule tasks need no device pass.
- **Hard rule — no git writes:** propose a commit message; the human commits. Read-only git only.
- Verify any XR API against the installed **XRI 3.3.0 / OpenXR 1.16** packages before relying on it.
- **In-Editor XR testing is enabled** (OpenXR on Android + Standalone): Quest Link Play and the XR Device
  Simulator work in the Editor; a device **Build And Run** stays the final validation.
