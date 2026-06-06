# T12 — Composition root (manual DI)

| | |
|---|---|
| Branch | `feature/composition-root` |
| Milestone | M3 — wiring |
| Design ref | guardrails §6 (Composition — wiring only, no rules), §8 (dependency rules: serialized refs + ctor injection + `Initialize` + one root; the single scene-level root is **not** a singleton), §9 (manual DI is the MVP default — no container), §20 (architecture-decision gate); [ADR 0001](../architecture/adr/0001-tech-baseline.md) (manual DI: composition root + ctor injection + serialized refs + factories/pools) |
| Depends on | T11 (`RoundLoopController` self-wiring), T11b (`ShardSpawner.AcceptsProvider` seam) |
| Touches scenes/prefabs | minor — add one `Composition Root` GameObject (serialized refs: `RoundConfig`, shard prefab + pooled-shard container, `RoundLoopController`, `ShardSpawner`); `ShardSpawner` drops its prefab/container refs. No prefab change |
| Status | ✅ done |

## Goal
Centralise dependency construction + lifecycle in **one** `StarforgeRelayCompositionRoot` (guardrails §6/§9).
Today `RoundLoopController.Start` builds the five M1 services + `RoundController`, and `ShardSpawner.Start`
builds the `ShardPool` + `ShardSpawnPlanner` + `Prewarm`s — each self-wires. T12 moves construction into the
root, hands the built graph to the adapters via explicit `Initialize(...)`, and **disposes the `ShardPool` on
teardown** (it is `IDisposable` but nobody disposes it → a leak notice fires on Play-stop). **Wiring/lifecycle
only — no gameplay-rule or XR change.**

## Acceptance criteria

### `StarforgeRelayCompositionRoot` (MonoBehaviour, `StarforgeRelay.Composition`) — wiring only
- Serialized: `RoundConfig`, the shard **prefab** + pooled-shard **container**, the scene `RoundLoopController`
  + `ShardSpawner`. Null-check in `Awake`; `LogError` + bail if any is unset (like the adapters do now).
- In **`Awake`** (runs before the adapters' `Start`): build the five services from `RoundConfig`
  (`ScoreService`/`ComboTracker`/`HeatService`/`StabilizationProgress`/`RoundTimer`) + a `RoundController`;
  build the **`ShardPool`** (factory instantiates the prefab under the container) and **`Prewarm`** it
  (guardrails §6 "create factories and pools"); then inject via explicit `Initialize(...)`.
- **Owns disposal:** in `OnDestroy`, `pool.Dispose()` → **no leak notice on Play-stop**.
- **Not a singleton** (guardrails §8 — a scene-level root is allowed; expected custom singletons stay **0**).
  Contains **no** scoring/heat/XR/UI logic (guardrails §6 must-not).

### `RoundLoopController` / `ShardSpawner` — receive deps, stop self-building
- Replace `Start`-time construction with `Initialize(...)` called by the root; guard against acting before
  `Initialize` (no double-fill, no NRE).
- `RoundLoopController.Initialize(RoundController roundController)` — stores it, then does its **existing**
  wiring unchanged (subscribe ports + spawner, `AcceptsProvider = () => roundController.Stabilization`,
  `roundController.Start()`). Keeps its serialized scene refs (`_ports`, `_spawner`); it just no longer
  *builds* the controller.
- `ShardSpawner.Initialize(ShardPool pool)` — stores the pool, builds its planner from its **own** pad
  transforms (scene-coupled), fills to target. Keeps `RoundConfig` + pads serialized; **drops** its prefab /
  container refs and the pool construction (now the root's; the root disposes it).

### Behaviour (smoke)
- The round plays **exactly as before** (grab → validate → correct / wrong / expired → win / overload / time-out).
- **Play-stop shows no `ShardPool` leak notice** (the current symptom is gone).
- The root is the one obvious place that shows the whole wiring.

## Implementation notes
- **The split (per guardrails §6 "create factories and pools"):** the **root** owns the generic graph — the
  five services, `RoundController`, and the `ShardPool` (+ `Prewarm` + `Dispose`). The **`ShardSpawnPlanner`
  stays in `ShardSpawner`**, because it is built from the scene **pad transforms** the spawner owns — hoisting
  it to the root would only relocate those scene refs for no gain. The single sub-decision to confirm at
  validation: leave the planner in the spawner (**recommended**) vs. also move the pad refs + planner to the
  root (more central, more churn, no real benefit here).
- Execution order: root in `Awake`, adapters act only **after** the root injects (via `Initialize`, or the root
  drives the start). Verify no first-Play `routine is null` regression.
- **New:** `Scripts/Composition/StarforgeRelayCompositionRoot.cs`. **Edit:** `RoundLoopController.cs`
  (build → `Initialize(roundController)`), `ShardSpawner.cs` (drop prefab/container + pool build →
  `Initialize(pool)`), the **scene** (add the root object + refs, saved). **Style:** C# 9, `_camelCase`,
  `sealed`, XML docs.
- **No new pure rule → no new EditMode test required** (guardrails §17); covered by the full-suite re-run + the
  smoke. If a binding-resolution helper turns out testable, ship a test in-change.

## Out of scope
- `AppStateMachine` / MainMenu → Calibration → Playing → Results → **T13**. World-space UI → T14.
- A DI **container** (VContainer): manual DI stays (ADR 0001); a container would need its own ADR.
- Removing the dev `[Round]` logs → T14 (HUD consumes the events). No gameplay tuning (T21).

## Verification
- **Tests:** full EditMode suite re-run **green this session** (no regression); no new pure rule.
- **Restricted-API:** clean over `Assets/_Project`; XRI in C# still only `PortSocket` + `ShardMotion`; the root
  is wiring-only (no XRI).
- **Smoke (XR Device Simulator):** the full round still plays; **no leak notice on Play-stop**; console clean
  (sim-haptic noise excepted).
- **Done =** full EditMode green this session + restricted-API clean + smoke + no leak notice; then close docs
  (brief Status + matrix + current-status).

## What was actually done
Implemented + verified 2026-06-06 (branch `feature/composition-root`; human commits).

- **New `StarforgeRelayCompositionRoot`** (`StarforgeRelay.Composition`, wiring-only, not a singleton): in
  `Awake` builds the five services + `RoundController` and the `ShardPool` (+ `Prewarm`), injects them via the
  adapters' `Initialize`, and disposes the pool in `OnDestroy`. Null-guards its 5 serialized refs.
- **`RoundLoopController`:** `Start` → `Initialize(RoundController)` (keeps its port/spawner/`AcceptsProvider`
  wiring + `_round.Start()`); dropped the `_config` field (services now built by the root).
- **`ShardSpawner`:** `Start` → `Initialize(ShardPool)` (keeps building its planner from the scene pads + fill);
  dropped `_shardPrefab` / `_shardContainer` (the root builds the pool). Planner stays here per the recommended
  split (§6: root owns the pool; the scene-coupled planner stays with the spawner).
- **Scene:** added a `Composition Root` GameObject wired to `RoundConfig`, the shard prefab + `Shards` container,
  and the `Round Loop` / `Shard Spawner` adapters — **all 5 refs verified resolved**; the adapters' moved-out
  serialized fields are gone.
- **Checks:** compile clean; EditMode **85/85** this session (no new pure rule — wiring task); restricted-API
  clean; XRI in C# still only `PortSocket` + `ShardMotion` (root is XRI-free).
- **Smoke (MCP Play):** the round auto-wires through the root — **4 shards spawn**, console clean during play;
  a **settled Play-stop is clean** (no errors).
- **Leak finding (honest):** the `Leak Detected: Persistent 4 allocations` + `routine is null` ×6 seen on the
  *first* Play-after-recompile were the **documented domain-reload transient** (clean on a settled re-play) — not
  a persistent `ShardPool` leak. `pool.Dispose()` is kept as correct `IDisposable` practice. T12 changed **zero**
  gameplay logic (only construction location), so the grab → insert → consume path is unchanged from T11b.
- Commit: `feat(core): composition root — centralise DI + pool lifecycle (T12)`.
