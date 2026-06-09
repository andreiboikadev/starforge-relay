# T13 — AppStateMachine (Boot → … → Results)

| | |
|---|---|
| Branch | `feature/app-state-machine` |
| Milestone | M3 — wiring |
| Design ref | GDD §15 (user flow), §16 (pause freezes timer/lifetime/spawning; pause-menu options), §23 (practical state machine), §13 (results snapshot); guardrails §6 (App Flow), §11 (tick/coroutines), §15 (events), §16 (rule ownership), §17 (per-mechanic gate); [ADR 0001](../architecture/adr/0001-tech-baseline.md) (manual DI) |
| Depends on | T12 (composition root) |
| Touches scenes/prefabs | **no** — the existing `Composition Root` object builds + ticks the machine in code; no new GameObject and no new serialized reference |
| Status | 🟡 in progress |

## Goal
Add the app/round state machine (GDD §23) and **gate the round on it**. Today the round auto-starts at
scene load — `RoundLoopController.Initialize` ends with `_round.Start()` and `ShardSpawner.Initialize` ends
with `SpawnToTarget()` + `_active = true`. T13 makes the round start **only on entering `Playing`**, freeze
in `Paused` (timer + shard lifetime + spawning, GDD §16), route a round end through `RoundComplete →
Results`, and **reset cleanly on Play-Again / Restart**. Pure, testable state logic over a thin
`IRoundLifecycle` seam; no UI yet — a temporary debug-key driver stands in for buttons until T14 replaces it.

## Decisions (resolved at authoring)
1. **Scope = the full loop, incl. Play-Again / Restart reset** (matrix title `Boot→…→Results`). Reset is done
   by **rebuilding the plain-C# round through a `Func<RoundController>` factory** — no per-service `Reset()`,
   the tested T01–T06 core is untouched. *(Fork parked: if shard-teardown proves gnarly, split the reset to a
   `T13b`; not expected.)*
2. **`TrackingLost` deferred.** Optional per GDD §23 / guardrails §6 ("only if detection is cheap"). Ship the
   7 core states; wire TrackingLost later only if it earns its place.
3. **Lifecycle calls live in the machine's transition triggers, not in `PlayingState.Enter`.** This is the
   key correctness point: `Paused → Playing` re-enters `Playing`, so if `PlayingState.Enter` called
   `StartRound()` a **resume would restart the round**. Instead the *triggers* call the lifecycle —
   `RequestStartRound → StartRound()` (fresh), `RequestResume → ResumeRound()` (unfreeze only). State
   `Enter`/`Exit` are reserved for view/audio hooks (T14/T16); only `RoundCompleteState` has tick logic (its
   delay). The machine stays the single caller of `IRoundLifecycle`.
4. **Pause = `Time.timeScale = 0` *plus* a one-line input gate.** `timeScale = 0` freezes everything
   time-based — verified against source: `RoundController.Tick(0)` is a no-op (`RoundTimer` ignores
   non-positive deltas, T03), the spawner's lifetime `Tick` and `WaitForSeconds` respawn freeze, and
   `ShardMotion`'s bob (`Time.time`) + spin/return (`Time.deltaTime`) freeze. **But `timeScale` does NOT stop
   XRI select events** (interactors run on unscaled time) and `RoundController.Phase` is still `Playing`
   during pause — so without a guard a shard socketed mid-pause would still score. So `PauseRound()` also
   sets a `_paused` flag and `RoundLoopController.OnInsertEvaluated` (and, defensively,
   `OnShardLifetimeExpired`) early-returns while `_paused`. The machine ticks on `Time.unscaledDeltaTime` so
   it can leave Paused; the `timeScale`/`_paused` writes live in the **adapter**, so the state classes stay
   `UnityEngine`-free. World-space UI (XRI ray + Trigger) runs on unscaled time, so the T14 pause menu stays
   interactable.
5. **Class-per-state shape** (`IAppState` + 7 states), not an enum+switch — T14 (UI per state) and T16 (audio
   per state) will hang real `Enter`/`Exit` behavior on each. *(If MVP minimalism is preferred, an enum +
   transition table is the lighter fallback — say so and I'll collapse it.)*
6. **Driver in the composition root.** The root creates + ticks the machine and reads **temporary** Input
   System debug keys (no new scene object → "Scenes: no"). T14 removes the debug keys and drives the same
   triggers from world-space buttons.

## Acceptance criteria

### Pure — `AppStateMachine` + states (`StarforgeRelay.App`, plain C#, EditMode-tested)
- **`AppPhase`** enum: `Boot, MainMenu, Calibration, Playing, Paused, RoundComplete, Results`.
- **`IAppState`**: `void Enter()`, `void Exit()`, `void Tick(float unscaledDeltaTime)`, `AppPhase Phase { get; }`.
- **`AppStateMachine`** (plain C# — **no `MonoBehaviour`, no `UnityEngine` statics**; the new pure logic the
  gate covers):
  - Owns the current `IAppState`; `ChangeState` runs `Exit` → swap → `Enter` and raises
    `event Action<AppPhase> PhaseChanged` (fired on the **initial** enter too, so T14/T16 can render/voice it).
  - Transition triggers — each **invalid-for-the-current-phase trigger is a no-op** (dev `Debug.LogWarning`,
    `Debug.isDebugBuild`-gated). Each calls `IRoundLifecycle` as noted, then `ChangeState`:
    - `RequestCalibration()` — `MainMenu → Calibration` (no lifecycle call).
    - `RequestStartRound()` — `{Calibration, Results, Paused} → Playing`, calls `lifecycle.StartRound()`
      (covers Play / Play-Again / Restart-from-pause; `StartRound` restores `timeScale = 1`).
    - `RequestPause()` — `Playing → Paused`, calls `lifecycle.PauseRound()`.
    - `RequestResume()` — `Paused → Playing`, calls `lifecycle.ResumeRound()` (**not** `StartRound`).
    - `RequestMainMenu()` — `{Calibration, Paused, Results} → MainMenu`, calls `lifecycle.StopRound()`
      (safe/no-op when no round is live; restores `timeScale = 1`).
    - `OnRoundEnded(RoundEndedEvent e)` — `Playing → RoundComplete`; **stores `e` as `LastResult`**.
  - `Tick(float unscaledDeltaTime)` delegates to the current state (only `RoundCompleteState` uses it).
  - **`RoundEndedEvent LastResult { get; }`** — the snapshot the Results view (T14) reads (GDD §16: Results
    reads, never recomputes). Retained from `OnRoundEnded`.
- **States** (thin; hold the machine as context so `Boot`/`RoundComplete` can request the next transition;
  no state touches `IRoundLifecycle` directly — the machine's triggers own those calls):
  - `BootState` advances to MainMenu on its **first `Tick`** (not inside `Enter` — avoids re-entrant
    `ChangeState`); no asset loading needed yet.
  - `MainMenuState` / `CalibrationState` / `PlayingState` / `PausedState` / `ResultsState` — `Enter`/`Exit`
    are **empty in T13** (reserved for T14 UI show/hide + T16 audio). The round lifecycle is driven by the
    triggers above, not these hooks.
  - `RoundCompleteState.Enter` starts a brief delay (≈1.5 s, GDD §12; a `const`/serialized default — **no
    asset change**); `Tick(unscaledDt)` counts it down and, at ≤ 0, asks the machine to advance to Results.
    *(Victory/overload/time-out feedback **content** is T17 — here RoundComplete is only the delay.)*
  - The machine subscribes `lifecycle.RoundEnded` → `OnRoundEnded` (Playing → RoundComplete).

### `IRoundLifecycle` seam (`StarforgeRelay.App`)
- `void StartRound()` — **fresh** round: build/rebuild it, clear + fill shards, `Time.timeScale = 1`. Used
  for Play, Play-Again, and Restart-from-pause (idempotent — one path).
- `void PauseRound()` → `Time.timeScale = 0` + set the adapter's `_paused`.
- `void ResumeRound()` → `Time.timeScale = 1` + clear `_paused` (unfreeze only — **does not** rebuild).
- `void StopRound()` — stop ticking, clear active shards, `Time.timeScale = 1`; no live round (to-menu).
- `event Action<RoundEndedEvent> RoundEnded` — re-raised from `RoundController.Ended`.

### Adapter wiring (grounded in the current source)
- **`StarforgeRelayCompositionRoot`** — replace the single `RoundController` build with a
  `Func<RoundController> roundFactory` (closure over `_config`, building the five services exactly as the
  current `Awake` does); keep the `ShardPool` build + `Prewarm` + `OnDestroy` dispose. Construct the
  `AppStateMachine` + 7 states wired to the `RoundLoopController` (as `IRoundLifecycle`). Inject the factory
  into `RoundLoopController.Initialize`; `_spawner.Initialize(_pool)` no longer auto-fills. Add `Update()`
  that ticks the machine on `Time.unscaledDeltaTime` and reads **temporary** Input System debug keys
  (null-guard `Keyboard.current`). Enter the initial `Boot` state in **`Start`** (after every `Awake`
  wiring). **Expose the `AppStateMachine` via a public getter** so T14's presenters can reach it (call
  triggers, subscribe `PhaseChanged`, read `LastResult`). Stays XRI-free, **not** a singleton. *Altitude
  note:* the root *ticks* the machine here — borderline vs guardrails §6 "wiring only"; acceptable for T13
  (keeps "Scenes: no"). If T14 wants a cleaner split, extract a thin, referenceable `AppFlowController` then
  (that would make T14 "Scenes: minor").
- **`RoundLoopController`** — `Initialize(Func<RoundController> roundFactory)` stores the factory and wires
  the **stable** refs (subscribe the 3 `PortSocket.InsertEvaluated`); it **no longer builds or `Start()`s a
  round**. Implements `IRoundLifecycle`:
  - `StartRound()` — `StopAllCoroutines()` (cancel any in-flight `ConsumeRoutine` from a prior round),
    unsubscribe any previous `_round`, build a fresh one via the factory, subscribe its 4 events, set
    `_spawner.AcceptsProvider = () => _round.Stabilization`, `_paused = false`, `Time.timeScale = 1`,
    `_round.Start()`, then `_spawner.BeginFill()`.
  - `PauseRound()` → `_paused = true`, `Time.timeScale = 0`. `ResumeRound()` → `_paused = false`,
    `Time.timeScale = 1`. `StopRound()` → `StopAllCoroutines()`, unsubscribe `_round`,
    `_spawner.StopRespawns()` + `_spawner.ClearActive()`, `Time.timeScale = 1`.
  - `OnInsertEvaluated` / `OnShardLifetimeExpired` early-return while `_paused` (the timeScale gap, decision 4).
  - `OnEnded` re-raises `RoundEnded` (machine → RoundComplete) **and** `_spawner.StopRespawns()` (as today).
    `Update` still ticks `_round` while `Phase == Playing` (harmless `Tick(0)` if a stray frame slips).
    `OnDisable` unsubscribes ports + the current `_round`.
- **`ShardSpawner`** — `Initialize(pool)` stores the pool + validates pads/config but **no longer builds the
  planner, fills, or sets `_active`** (drop those two lines). Add:
  - `BeginFill()` — `ClearActive()`, **(re)build a fresh planner** from the pads (so per-round state — pad
    reservations, colour counts — is clean; with seed 0 this also gives fresh per-round variety),
    `SpawnToTarget()`, `_active = true`. Called by `StartRound`.
  - `ClearActive()` — for each `_shardPads` shard: `motion.ForceRelease()` then `_pool.Release(shard)`; clear
    `_shardPads`; `StopAllCoroutines()` (cancel pending respawns). Safe on an empty set (first start).
  - Keep `StopRespawns()` for round end.

### Behaviour (smoke, via the temporary debug keys)
- Scene load → `Boot → MainMenu`: **no shards, no round ticking** (the current auto-start is gone).
- Advance to `Playing` → 4 colour-distributed shards spawn; timer counts down.
- `Pause` → timer + shard lifetime + respawns + idle bob freeze, **and a shard socketed while paused does
  not score**; `Resume` → continues from where it was (the round is **not** restarted).
- Reach `Won` / `Overloaded` / `TimedOut` → `RoundComplete` (brief delay) → `Results` exposing the finalized snapshot.
- `Play-Again` / `Restart` → a fresh round: score / combo / heat / stabilization / timer reset, stale shards cleared, 4 new shards.
- `Results → MainMenu` (and `Paused → MainMenu`) → shards cleared, idle menu, `timeScale` back to 1.

## Implementation notes
- **New:** `Scripts/App/AppPhase.cs`, `Scripts/App/IAppState.cs`, `Scripts/App/IRoundLifecycle.cs`,
  `Scripts/App/AppStateMachine.cs`, `Scripts/App/States/{Boot,MainMenu,Calibration,Playing,Paused,RoundComplete,Results}State.cs`,
  `Tests/EditMode/AppStateMachineTests.cs`. **Edit:** `Scripts/Composition/StarforgeRelayCompositionRoot.cs`,
  `Scripts/Gameplay/RoundLoopController.cs`, `Scripts/Gameplay/ShardSpawner.cs`. **No scene/prefab/asset
  change** (root object exists; machine built in code; RoundComplete delay is a `const`/serialized default).
- **Single asmdef** (`StarforgeRelay.Runtime`): the `StarforgeRelay.App` namespace lives in it (no new
  assembly). `IRoundLifecycle` lives in `App`; `RoundLoopController` (Gameplay) implements it — fine inside
  one assembly (namespacing is organizational; no cyclic-ref risk).
- **Round rebuild seam:** lift the five-service build from the root's `Awake` into the
  `Func<RoundController>` so `StartRound()` builds a fresh round each time (Play / Play-Again / Restart share
  one path). Do **not** add `Reset()` to `RoundController`/services — rebuilding plain C# is cheaper and
  leaves the tested core untouched.
- **Subscription + coroutine hygiene (guardrails §15, §11):** `StartRound`/`StopRound` unsubscribe the
  previous `_round` and `StopAllCoroutines()` so neither a stale event nor a deferred `ConsumeRoutine` can act
  across a round boundary (else a 1-frame-deferred consume could pool a *re-used* shard in the next round).
  Port subscriptions are wired once in `Initialize`; `_spawner.ShardLifetimeExpired` stays subscribed as today.
- **Input System, not legacy `Input`** for the debug keys (`UnityEngine.InputSystem.Keyboard.current`,
  null-guarded): e.g. Space = advance Boot→MainMenu→Calibration→Playing, P = pause/resume, R = start/restart,
  M = main menu. Mark clearly **temporary** (removed in T14). XRI-free. **Asmdef:** the keys need
  `Unity.InputSystem` referenced by `StarforgeRelay.Runtime.asmdef` (XRI does not re-export it) — add the
  reference for T13; revisit when the keys are removed at T14.
- **MCP discipline (T08–T12):** `refresh scope=all` for the new files; never edit scripts while the Editor is
  in Play (domain reload corrupts the live XR session); smoke from a settled editor (the first Play after a
  recompile shows the benign `routine is null` / domain-reload transient — a re-Play is clean).
- **Style** (`csharp-style.md` + `.editorconfig`): C# 9 (block namespace, **no** `record`/`init`), no
  `#nullable enable`, `_camelCase` fields, `sealed`, XML docs on public members, `var` only when RHS-apparent.

## Out of scope
- World-space UI views/presenters + the actual MainMenu/Calibration/Pause/Results **buttons** → **T14**
  (T13's debug keys are temporary and removed there). Settings/HowTo/Credits panels + persistence → T14/T15.
- **Recenter** (Calibration's Recenter button) → deferred (GDD "if easy"; XR-origin recenter wiring later).
- **`TrackingLost`** state → deferred (optional, GDD §23).
- Audio per state → T16; beam/spark/fizzle + **RoundComplete feedback content** + core-state visuals → T17.
- Removing the dev `[Round]` `Debug.Log`s → T14 (HUD consumes the events). No gameplay tuning (T21).

## Verification
- **Tests (EditMode, pure — fake `IRoundLifecycle` asserting call counts/order):**
  - `Boot→MainMenu`; `MainMenu→Calibration`; `Calibration→Playing` calls `StartRound` **once**.
  - `Playing→Paused` calls `PauseRound`; **`Paused→Playing` (resume) calls `ResumeRound`, NOT `StartRound`**
    (regression guard for the resume-restart bug).
  - `Playing→RoundComplete` on `RoundEnded` and **`LastResult` holds the event's snapshot**;
    `RoundComplete→Results` only **after** the delay elapses (not before).
  - `Results→Playing` (Play-Again) and `Paused→Playing` (Restart) call `StartRound`; `{Results, Paused,
    Calibration}→MainMenu` calls `StopRound`.
  - Illegal triggers are no-ops (e.g. `RequestPause` in MainMenu, `RequestResume` in Playing, `RequestStartRound`
    in Playing); `PhaseChanged` fires on each transition incl. the initial enter.
  - Ship in the same change; **re-run the full suite** (was 85/85 → 85 + N), no regression.
- **Restricted-API:** clean over `Assets/_Project`; `AppStateMachine`/states are pure (no XRI, no
  `UnityEngine`); the `Time.timeScale`/`_paused` writes are confined to the `RoundLoopController` adapter; XRI
  in C# stays only in `PortSocket` + `ShardMotion`; debug input uses the Input System (no legacy `Input.Get*`).
- **Smoke (XR Device Simulator):** the behaviours above — gated start (no auto-play), pause freezes
  timer/lifetime/spawning **and blocks mid-pause inserts**, resume does not restart, all three end states →
  RoundComplete → Results, Play-Again/Restart resets every meter and the shards, Main-Menu clears the board.
  Console clean (the known benign sim-haptic noise excepted; re-check on device at T20).
- **Done =** full EditMode suite green this session + restricted-API clean + the smoke above + no Console
  errors; then close docs (brief Status + matrix + current-status).

## What was actually done
—
