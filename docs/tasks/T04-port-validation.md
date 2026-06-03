# T04 — PortValidationService (color match in code)

| | |
|---|---|
| Branch | `feature/port-validation` |
| Milestone | M1 — Pure rules |
| Design ref | GDD §10 (ports), §12 (wrong insert); guardrails §6 (critical note), §13, §16–17 |
| Depends on | T01 (shard/port color identity) |
| Touches scenes/prefabs | no |
| Status | ✅ done |

## Goal

The rule that decides **correct vs wrong** insert by **color match in code** — the pure-logic half of the
single most important XR decision in the project. The socket (T09) accepts *any* shard; this service is
what makes a mismatch actually register.

## Acceptance criteria

- **`PortValidationService`** (plain C#): given an inserted shard's color and the target port's color —
  - **match** → `CorrectInsert` outcome;
  - **mismatch** (shard clearly inside a *wrong* port) → `WrongInsert` outcome;
  - **no port** (empty-space drop) → `NoPenalty` outcome (no correct, no wrong).
- Uses the shared `ShardColor` enum (`Solar`, `Ion`, `Pulse`) introduced in T01 — does not redefine it.
- **EditMode tests**: each color matches its own port → correct; every cross pair → wrong; null/none
  target → no penalty; all three colors covered.

## Implementation notes

- `Assets/_Project/Scripts/Gameplay/`; pure decision only — returns an outcome, does **not** apply heat,
  combo, beams, or move objects (those are owned by `HeatService`/`ComboTracker` via `RoundController`, and
  by the views).
- **Critical (guardrails §6 / ADR 0001):** this service exists precisely so the socket does **not**
  hard-filter color. Do **not** add an `IXRSelectFilter` or per-color interaction-layer gate — a wrong
  shard must be *selectable* so the wrong-insert path fires. The XR socket wiring is T09; keep this task
  pure.

## Out of scope

- The `XRSocketInteractor` / `PortSocket` adapter and force-eject + push-back behavior (T09); applying the
  outcome to heat/combo/score (T06); beam/spark VFX (M4).

## Verification

- Tests: the EditMode fixtures above.
- Done = full EditMode suite green + no Console errors.

## What was actually done

Built on `feature/port-validation` (2026-06-03); **full EditMode suite 45/45 green** (33 prior + 12 new),
no Console errors. Commit proposed (human commits).

- `PortValidationService` (pure C#) — `Assets/_Project/Scripts/Gameplay/PortValidationService.cs`:
  `Validate(ShardColor shard, ShardColor? port)` → `InsertOutcome.Correct` (match), `Wrong` (mismatch),
  `NoPenalty` (`port == null`, empty-space drop). Pure decision, no side effects.
- `InsertOutcome` enum — `Assets/_Project/Scripts/Gameplay/InsertOutcome.cs` (`Correct` / `Wrong` /
  `NoPenalty`); shared decision type for `RoundController` (T06) and the port-socket adapter (T09).
- Reuses the shared `ShardColor` (T01) for both shard and port color; **no socket color-gate added**
  (guardrails §6 / ADR 0001 — the wrong-insert path must stay selectable; XR socket wiring is T09).
- Tests: `PortValidationServiceTests` (12) — 3 matches (all colors), 6 cross-pair mismatches, 3 null-port.

No new asmdef/config. No guardrail deviations. **Naming note (deviation from the brief's wording):** the
enum values are `Correct` / `Wrong` / `NoPenalty`; the acceptance text above phrased the first two as
`CorrectInsert` / `WrongInsert`. Shortened deliberately so the *decision* value isn't confused with the
§15 `CorrectInsert` / `WrongInsert` *events* (raised later by `RoundController`). Behavior is identical and
the 12 tests assert it.
