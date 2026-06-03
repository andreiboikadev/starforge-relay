# T01 — RoundConfig + ScoreService + ComboTracker

| | |
|---|---|
| Branch | `feature/round-config-scoring` |
| Milestone | M1 — Pure rules |
| Design ref | GDD §12–13 (round rules, scoring); guardrails §7 (RoundConfig), §16–17 |
| Depends on | — |
| Touches scenes/prefabs | no |
| Status | ✅ done |

## Goal

Lay the data foundation (`RoundConfig`) and the first two pure rules — `ScoreService` and `ComboTracker`.
Everything downstream reads the GDD's numbers from `RoundConfig` (never hard-coded) and these two services.

## Acceptance criteria

- **`ShardColor` enum** (`Solar`, `Ion`, `Pulse`) — the shared color vocabulary used by shards, ports,
  `PortValidationService` (T04), and `ShardSpawnPlanner` (T05). Define it here as the foundation so those
  tasks have a real type to depend on.
- **`RoundConfig`** ScriptableObject at `Assets/_Project/ScriptableObjects/Config/RoundConfig.cs`
  (+ one asset instance) with the GDD defaults: `roundDurationSeconds=90`, `stabilizationRequirement=20`,
  `heatCap=8`, `activeShardsDefault=4`, `activeShardsMax=6`, `shardLifetimeStart=14`, `shardLifetimeLate=12`,
  `lateLifetimeAfterAccepts=10`, `respawnDelayMin=0.3`, `respawnDelayMax=0.8`, `comboBonusInterval=5`,
  `comboBonusScore=50`, `comboHeatRelief=1`, `correctScore=10`, `heatPenaltyPerHeat=10`,
  `victoryTimeBonusPerSecond=2`. Treated as read-only at runtime.
- **`ScoreService`** (plain C#, no `MonoBehaviour`, no Unity statics): `+10` per correct; `+50` at every
  5-combo milestone; victory time bonus = `remainingSeconds × 2`; results heat penalty = `heat × 10`,
  clamped to never below 0.
- **`ComboTracker`** (plain C#): `+1` on correct; resets to 0 on wrong insert; resets on expire **only if
  the shard was held**; reports a milestone every 5.
- **EditMode tests** assert each bullet above (one fixture per service).

## Implementation notes

- Pure classes under `Assets/_Project/Scripts/Gameplay/`; values injected from `RoundConfig` (pass the
  numbers in, don't reference the SO type inside the rule if avoidable — keeps the rule headless-testable).
- **Asmdefs become required here** (this resolves guardrails §4's "optional asmdef" — create them now):
  put the runtime rules in a `StarforgeRelay.Runtime` asmdef, and the tests under
  `Assets/_Project/Tests/EditMode/` in a `StarforgeRelay.Tests.EditMode` asmdef that references it. An
  EditMode test asmdef **cannot see `Assembly-CSharp` types**, so the runtime code must live in its own
  asmdef the moment you want unit tests — i.e. from T01 on.
- Keep combo milestone logic in `ComboTracker`; `ScoreService` consumes the milestone signal — don't
  duplicate the formula (guardrails §16: one owner per rule).

## Out of scope

- Heat / stabilization / timer (T02, T03); spawn planning (T05); RoundController orchestration and star
  rating (T06). Any MonoBehaviour adapter or scene wiring (M2).

## Verification

- Tests: the EditMode fixtures above, run via MCP Test Runner (or CLI per `../development/build-and-test.md`).
- Done = full EditMode suite green + no Console errors. No device pass needed (pure logic).

## What was actually done

Built on `feature/round-config-scoring` (2026-06-03); **12/12 EditMode tests green** via MCP Test Runner,
no Console errors after compile. Commit proposed (human commits).

- `ShardColor` enum (Solar/Ion/Pulse) — `Assets/_Project/Scripts/Gameplay/ShardColor.cs`.
- `RoundConfig` SO — `Assets/_Project/ScriptableObjects/Config/RoundConfig.cs` + `RoundConfig.asset`; all
  GDD §12–13 defaults verified serialized (90/20/8 · 4/6 · 14/12/10 · 0.3/0.8 · 5/50/1 · 10/10/2);
  read-only getters, `[CreateAssetMenu]`.
- `ScoreService` (pure C#) — +10/correct; +50/milestone; victory bonus = remaining×2; heat penalty =
  heat×10 clamped ≥0. Scoring numbers injected via ctor (4 ints), not the SO type.
- `ComboTracker` (pure C#) — +1/correct (returns a milestone bool every 5); reset on wrong; reset on
  expire only if held. Owns the milestone formula; `ScoreService` consumes the signal (one owner per rule).
- Asmdefs: `StarforgeRelay.Runtime` (one runtime assembly at `_Project/` root) + `StarforgeRelay.Tests.EditMode`.
- Tests: `ScoreServiceTests` (6) + `ComboTrackerTests` (6).

Decisions: single `StarforgeRelay.Runtime` asmdef at the `_Project/` root covers `Scripts/` +
`ScriptableObjects/` (resolves guardrails §4's optional-asmdef); the EditMode test asmdef under
`Tests/EditMode/` carves out its own subtree. Passed the 4 scoring ints to `ScoreService` directly rather
than a config struct (abstraction deferred to 2nd use). No guardrail deviations.
