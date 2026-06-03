# T02 — HeatService + StabilizationProgress

| | |
|---|---|
| Branch | `feature/heat-stabilization` |
| Milestone | M1 — Pure rules |
| Design ref | GDD §12 (heat, victory); guardrails §16–17 |
| Depends on | T01 (RoundConfig) |
| Touches scenes/prefabs | no |
| Status | ✅ done |

## Goal

The two meters that drive the round's end states: `HeatService` (overload) and `StabilizationProgress`
(victory). Both pure C#, both reading their caps from `RoundConfig`.

## Acceptance criteria

- **`HeatService`** (plain C#): `+1` on wrong insert; `+1` on expired shard; `−1` at each combo milestone
  **if heat > 0** (combo relief); reports **overload exactly at `heatCap` (8)**, not before. Heat never
  goes below 0.
- **`StabilizationProgress`** (plain C#): `+1` per correct insert; reports **victory exactly at
  `stabilizationRequirement` (20)**.
- **EditMode tests**: heat gain on wrong/expire; relief at milestone only when heat > 0; overload fires at
  exactly 8 (and not at 7); stabilization victory fires at exactly 20 (and not at 19); no underflow.

## Implementation notes

- `Assets/_Project/Scripts/Gameplay/`; caps injected from `RoundConfig`.
- Combo relief is triggered *by* a milestone event (owned by `ComboTracker`, T01) — `HeatService` reacts;
  it doesn't compute milestones itself (guardrails §16).
- Keep "did we overload?" / "did we win?" as queryable state or events; the *decision* of which end state
  wins when several fire at once belongs to `RoundController` (T06), not here.

## Out of scope

- Timer / time-out (T03); end-state priority resolution + star rating (T06); any UI or VFX reaction to
  heat/stabilization (M3/M4).

## Verification

- Tests: the EditMode fixtures above.
- Done = full EditMode suite green + no Console errors.

## What was actually done

Built on `feature/heat-stabilization` (2026-06-03); **full EditMode suite 24/24 green** (MCP Test Runner;
12 prior + 12 new), no Console errors. Commit proposed (human commits).

- `HeatService` (pure C#) — `Assets/_Project/Scripts/Gameplay/HeatService.cs`: +1 on wrong insert, +1 on
  expired shard; `RelieveAtMilestone()` removes `comboHeatRelief` only when heat > 0; `IsOverloaded` at
  exactly `heatCap` (8); never below 0. `heatCap`/`comboHeatRelief` injected via ctor.
- `StabilizationProgress` (pure C#) — `…/StabilizationProgress.cs`: +1 per correct; `IsComplete` at exactly
  `stabilizationRequirement` (20). Requirement injected via ctor.
- Overload / victory exposed as **queryable state** (`IsOverloaded` / `IsComplete`); end-state priority is
  deferred to `RoundController` (T06) per the brief.
- Tests: `HeatServiceTests` (8) + `StabilizationProgressTests` (4) — incl. overload at 8 not 7, victory at
  20 not 19, relief only when heat > 0, no underflow.

No new asmdef/config — reused T01's `StarforgeRelay.Runtime` + EditMode test asmdef and `RoundConfig`
fields (`heatCap`, `comboHeatRelief`, `stabilizationRequirement`). Heat gain (+1/event) is the fixed GDD
rule (no RoundConfig field for it). No guardrail deviations.
