# T02 — HeatService + StabilizationProgress

| | |
|---|---|
| Branch | `feature/heat-stabilization` |
| Milestone | M1 — Pure rules |
| Design ref | GDD §12 (heat, victory); guardrails §16–17 |
| Depends on | T01 (RoundConfig) |
| Touches scenes/prefabs | no |
| Status | ▫ not started |

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

—
