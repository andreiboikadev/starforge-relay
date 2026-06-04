# Tasks — Starforge Relay

Self-contained task briefs + the full plan to demo-ready. The model is adapted (right-sized) from a
larger team's T-task system; the multi-dev / cross-repo / external-issue-tracker machinery is deliberately
dropped — add it back only if the team or scope grows (see "Scaling up" below).

## How this works

- **One brief per task** under `Tnn-<slug>.md`. A fresh session reading `CLAUDE.md` + `docs/INDEX.md` +
  the relevant `Tnn-*.md` should be able to start working without re-reading the whole backlog.
- **Plan to completion.** The matrix below lists every task from now to demo-ready. Near-term tasks carry
  full briefs; later tasks are matrix rows + a one-line scope, **expanded into a full brief when you pick
  them up** (don't over-author the future — scope shifts as earlier tasks land).
- **One branch / one logical unit per task.** Branch `feature/<slug>` (or `fix/` `chore/` `perf/`
  `refactor/`) off `dev`, per `CLAUDE.md` GitFlow-lite. The human commits; the assistant proposes a
  Conventional Commit message.
- **Definition of Done = the per-mechanic gate** (guardrails §17): pure-rule tasks ship EditMode tests in
  the same change; adapter tasks get a smoke pass; the full suite re-runs green; no Console errors.
- **Status:** `▫ not started` · `🟡 in progress` · `🔴 blocked on <reason>` · `✅ done`.
- **Before claiming a task done / blocked,** follow [`../development/agent-verification.md`](../development/agent-verification.md).

## Brief template

```markdown
# Tnn — <title>

| | |
|---|---|
| Branch | `feature/<slug>` |
| Milestone | M? — <name> |
| Design ref | GDD §… / guardrails §… |
| Depends on | — / Tnn |
| Touches scenes/prefabs | no / minor (…) / yes — <which> |
| Status | ▫ not started |

## Goal
1–2 sentences: what we build and why; what it unblocks.

## Acceptance criteria
Concrete, testable bullets — exact type / field / file names, exact GDD numbers.
For pure rules, list the EditMode assertions.

## Implementation notes
Files (new `*(new)*` / existing to extend); patterns to follow; guardrail refs; pitfalls.

## Out of scope
What this PR does NOT include — especially downstream tasks that consume this one's output.

## Verification
- Tests: EditMode cases (pure rules) / smoke check (adapters — MCP Play / XR Device Simulator / Quest).
- Done = full EditMode suite green + no Console errors (see ../development/build-and-test.md);
  device pass where tracking / controllers / grab feel matter.

## What was actually done
Filled on close: what actually shipped, any deviation from the plan, the commit/PR, the date.
`—` until done. (This is the as-built record — it is what a future session trusts over the plan.)
```

## Matrix — plan to demo-ready

Legend: `▫` not started · `🟡` in progress · `🔴` blocked · `✅` done. **Brief:** `✓` full brief written ·
`·` matrix-only (expand into a full brief on start).

| ID | Title | Branch | Milestone | Depends on | Scenes | Brief | Status |
|---|---|---|---|---|---|---|---|
| T01 | RoundConfig + ScoreService + ComboTracker | `feature/round-config-scoring` | M1 rules | — | no | ✓ | ✅ |
| T02 | HeatService + StabilizationProgress | `feature/heat-stabilization` | M1 rules | T01 | no | ✓ | ✅ |
| T03 | RoundTimer | `feature/round-timer` | M1 rules | T01 | no | ✓ | ✅ |
| T04 | PortValidationService (color match in code) | `feature/port-validation` | M1 rules | T01 | no | ✓ | ✅ |
| T05 | ShardSpawnPlanner (seeded RNG) | `feature/shard-spawn-planner` | M1 rules | T01 | no | ✓ | ✅ |
| T06 | RoundController (end-state priority, star rating) | `feature/round-controller` | M1 rules | T01–T05 | no | ✓ | ✅ |
| T07 | Strip Locomotion; rename scene off `SampleScene` default; confirm rig | `chore/strip-locomotion` | M2 slice | — | yes (rig) | ✓ | ✅ |
| T08 | Shard prefab + grab (Grip) + ShardPool | `feature/shard-grab` | M2 slice | T01 | yes (prefab) | · | ▫ |
| T09 | Ports as accept-any sockets → validate in code | `feature/port-socket` | M2 slice | T04, T08 | yes | · | ▫ |
| T10 | Reactor core + feeder pads + spawner + lerp-back | `feature/reactor-spawn` | M2 slice | T05, T08 | yes | · | ▫ |
| T11 | Round loop adapter: grab→socket→rules→events | `feature/round-loop-slice` | M2 slice | T06, T09, T10 | yes | · | ▫ |
| T12 | Composition root (manual DI) | `feature/composition-root` | M3 wiring | T11 | minor | · | ▫ |
| T13 | AppStateMachine (Boot→…→Results) | `feature/app-state-machine` | M3 wiring | T12 | no | · | ▫ |
| T14 | World-space UI views + presenters (ray+Trigger) | `feature/worldspace-ui` | M3 wiring | T13 | yes | · | ▫ |
| T15 | Settings (sound/haptics) + persistence | `feature/settings-persistence` | M3 wiring | T14 | minor | · | ▫ |
| T16 | AudioService + HapticService + configs | `feature/audio-haptics` | M4 feedback | T11 | no | · | ▫ |
| T17 | VFX pool + core state visuals | `feature/vfx-pool` | M4 feedback | T11 | yes | · | ▫ |
| T18 | Import Kenney + dress bay + fake-glow materials | `feature/art-dressing` | M5 art | T11 | yes | · | ▫ |
| T19 | Final-ish colors / glow on core / shards / ports | `feature/final-glow` | M5 art | T18 | yes | · | ▫ |
| T20 | Quest 2 build + on-device smoke (3 end states) | `chore/device-smoke` | M6 device | T14 | no | · | ▫ |
| T21 | Profiling + tuning (FPS floor, FFR, reach/size) | `perf/quest2-tuning` | M6 device | T20 | yes | · | ▫ |

**M0 — engine setup** is ✅ done already (OpenXR + XRI rig, Android/Quest config, verified on Quest 2 — see
[ADR 0001](../architecture/adr/0001-tech-baseline.md)); it predates this matrix and has no brief.

Rows are **never deleted**. A cancelled task keeps its row marked `❌ scope-removed <date> — <reason>` for
traceability.

## Authoring / closing a task (right-sized)

- **Author** (turning a `·` row into a real task): write the full brief from the template above; check the
  acceptance criteria against the cited GDD / guardrails section; set Status `🟡`; note it in
  `current-status.md`.
- **Close** (on merge): set the brief Status `✅` and **fill "What was actually done"**; update the matrix
  row here; refresh `current-status.md`. Keep these consistent — see
  [`agent-verification.md`](../development/agent-verification.md). **`current-status.md` is a state snapshot**
  (where-we-are / blockers / next): refresh it; do **not** append per-task file lists or commit/PR status —
  git, the matrix, and the brief's *What was actually done* own those.

## Scaling up (only when the need is real)

This is the solo/small-team shape. If the project gains devs or a long lifetime, graduate it the way the
larger reference project did — **add, don't rebuild**: split this file into a matrix + an `AUTHORING.md`
(task-slicing methodology); add `Owner` and `Touches scenes` conflict-serialization columns; add an
external-issue-tracker mapping column. Don't add any of that before it earns its place.
