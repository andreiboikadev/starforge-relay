# T03 — RoundTimer

| | |
|---|---|
| Branch | `feature/round-timer` |
| Milestone | M1 — Pure rules |
| Design ref | GDD §12 (90 s round, time-out); guardrails §11, §16–17 |
| Depends on | T01 (RoundConfig) |
| Touches scenes/prefabs | no |
| Status | ▫ not started |

## Goal

A pure, testable countdown that the Playing state ticks. No dependence on `Time.time` — the caller passes
`deltaTime`, so it runs identically in EditMode tests and on device.

## Acceptance criteria

- **`RoundTimer`** (plain C#): starts at `roundDurationSeconds` (90); `Tick(deltaTime)` counts down and
  clamps at 0; `Pause()` / `Resume()` hold and restore remaining time **without losing or double-counting**
  the frame they toggle on; reports **time-out exactly when remaining reaches 0**; exposes `Remaining`
  (for the victory time-bonus in `ScoreService`).
- **EditMode tests**: ticks down by the supplied delta; multiple ticks accumulate correctly; pause holds
  remaining and resume continues from it; ticking while paused does nothing; time-out fires at 0 and not
  before; remaining never goes negative.

## Implementation notes

- `Assets/_Project/Scripts/Gameplay/`; inject the duration from `RoundConfig`; inject `deltaTime` per tick
  (no Unity statics inside the class — guardrails §17 testability).
- The MonoBehaviour that calls `Tick(Time.deltaTime)` belongs to the Playing state (M2/M3), not here.

## Out of scope

- The actual per-frame driver / Playing state (M3); pausing the *rest* of the game (spawning, lifetimes) —
  this class only owns the clock; central pause/resume coordination is `RoundController` (T06) / app flow.

## Verification

- Tests: the EditMode fixtures above.
- Done = full EditMode suite green + no Console errors.

## What was actually done

—
