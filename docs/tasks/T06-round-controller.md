# T06 — RoundController (end-state priority, star rating)

| | |
|---|---|
| Branch | `feature/round-controller` |
| Milestone | M1 — Pure rules |
| Design ref | GDD §12 (end states), §13 (star rating); guardrails §16 (priority), §17 |
| Depends on | T01 + T02 + T03 + T04 + T05 |
| Touches scenes/prefabs | no |
| Status | ▫ not started |

## Goal

The pure orchestrator that owns round state: it routes insert outcomes (from `PortValidationService`) into
score / combo / heat / stabilization, resolves the **concurrent end-state priority**, and computes the
**star rating** for results. Still a plain-C# core — the MonoBehaviour adapter that ticks it and raises
Unity-facing events is the M2 slice (T11).

## Acceptance criteria

- **`RoundController`** (plain C#) holds references to the T01–T05 services and exposes the round lifecycle
  (start / apply correct / apply wrong / apply expired / tick / end) plus current runtime state.
- **Concurrent end-state priority** (guardrails §16): if stabilization reaches 20, heat reaches 8, and/or
  the timer reaches 0 on the same tick, resolve in order **victory > overload > time-out** — deterministic.
- **Star rating** (GDD §13): 0 stars for 0–5 correct **or overload before 6**; 1 star for 6–11; 2 stars
  for 12–19; 3 stars for a stabilized core (20).
- Correct insert applies: `ScoreService +10` (+50 at milestone), `ComboTracker +1`, `StabilizationProgress
  +1`, and combo-milestone heat relief; wrong applies `HeatService +1` + combo reset; expired applies
  `HeatService +1` + combo reset only if held — i.e. it *wires* the T01–T04 rules, doesn't re-implement them.
- **EditMode tests**: victory>overload>time-out resolved correctly when conditions coincide; each end state
  reachable in isolation; star-rating boundaries (5/6, 11/12, 19/20, overload-before-6); a correct insert
  updates all four meters once (no double-apply).

## Implementation notes

- `Assets/_Project/Scripts/Gameplay/`; constructor-inject the five services (manual DI). Results **reads**
  final state — it must not recompute score/heat (guardrails §16).
- Raise outcomes as small typed structs/events (`CorrectInsert`, `WrongInsert`, `RoundWon`, …) for the M2
  adapter to forward — but keep this class free of `UnityEngine` types where practical.

## Out of scope

- The MonoBehaviour tick driver, XR event plumbing, HUD, audio/VFX (T11 + M3/M4); pooling; scene wiring.

## Verification

- Tests: the EditMode fixtures above. **Re-run the full M1 suite (T01–T05) — no regression.**
- Done = full EditMode suite green + no Console errors. M1 is complete when T06 lands green.

## What was actually done

—
