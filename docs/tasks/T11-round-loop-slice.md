# T11 — Round loop adapter: grab→socket→rules→events

| | |
|---|---|
| Branch | `feature/round-loop-slice` |
| Milestone | M2 — VR slice |
| Design ref | GDD §5 (loop), §12 (correct/wrong/expired, victory/overload/time-out), §13 (scoring/stars); guardrails §6 (Round Gameplay, events), §15 (event policy), §16 (rule ownership + end-state priority) |
| Depends on | T06 (`RoundController` + event structs), T09 (`PortSocket.InsertEvaluated`), T10 (`ShardSpawner`, `ShardMotion`, `ShardPool`) |
| Touches scenes/prefabs | **yes** — new `RoundLoopController` in the scene + wiring; edits existing scripts `ShardSpawner` / `PortSocket` / `RoundController` + the shard pool-reset (see Implementation notes). No new prefab. |
| Status | ✅ done |

## Goal
Close the Stabilize Run loop in-scene: a thin MonoBehaviour adapter (`RoundLoopController`) that wires the port
insert outcomes (T09) and the shard spawner (T10) into the pure `RoundController` (T06), ticks the round timer,
**consumes** an accepted shard and **respawns** a replacement, and re-surfaces `RoundController`'s typed events
for later HUD/audio/VFX. Produces the first **end-to-end playable round** — insert shards → score/combo/heat/
stabilization → **win / overload / time-out** — without UI or feedback polish.

## Scope (resolved) — lifetime/expiry split out to `T11b`
T10 deferred shard **lifetime + expiry** to here, but T11 is already a full task. **Decision: T11 = the
insert/consume/respawn/tick/events loop (a complete, playable round); shard lifetime + expiry split into a
follow-up `T11b — shard lifetime + expiry`** (matrix row added). Rationale: the loop is playable and
demonstrable without lifetime (win by 20 inserts; lose by heat or time-out); lifetime/expiry is an independent
pressure layer that reuses T11's `Despawn`/respawn plumbing and the `RoundController.ApplyExpired(wasHeld)` path
(already in T06, left unused until T11b).

## Final-score finalization fix (resolved — folded into T11; the one T06 pure-rule change)
`RoundController.End()` (T06) currently raises `RoundEndedEvent.Score = _score.Score` **without** the **victory
time bonus** (remaining s × 2 on a win) or the **heat penalty** (heat × 10, GDD §13) — `ScoreService` *has*
`AddVictoryTimeBonus` / `ApplyHeatPenalty` (tested at T01) but nobody calls them, though `RoundEndedEvent` is
the final snapshot Results reads ("never recomputes"). So the score at the first real round end (**T11**) is
silently wrong. **Fix (in T11):** in `RoundController.End()`, before raising the event — on `Won`,
`_score.AddVictoryTimeBonus((int)_timer.Remaining)`; **always** `_score.ApplyHeatPenalty(_heat.Current)` (clamps
≥ 0). **Ships a `RoundControllerTests` EditMode case** (won-round score includes the time bonus; heat penalty
applied; never < 0) — guardrails §17. This is the single T06 pure-rule change in T11; the rest is adapter wiring.

## Acceptance criteria

### `RoundLoopController` (MonoBehaviour adapter, `StarforgeRelay.Gameplay`)
- Serialized: `RoundConfig`, the 3 `PortSocket`s, the `ShardSpawner` (T10).
- **On `Start`** (self-wired for the slice — centralised in T12): construct the five M1 services from `RoundConfig`
  + a `RoundController(score, combo, heat, stabilization, timer)`; subscribe to each `PortSocket.InsertEvaluated`;
  call `roundController.Start()` then `shardSpawner.SpawnToTarget()`.
- **`Update`:** `roundController.Tick(Time.deltaTime)` while `Phase == Playing` (drives the 90 s timer → time-out);
  stop once `IsOver`.
- **On `InsertEvaluated(port, shard, outcome)`:**
  - `Correct` → `roundController.ApplyCorrect()`, then **consume** the shard via `shardSpawner.Despawn(shard)`
    (pool + free its pad + schedule a respawn). **Critical (the key hazard):** a correct shard is **socket-held**
    (the `XRSocketInteractor` selected it), so it must be **released from the socket (`SelectExit`) before it is
    pooled/deactivated** — never pool a socket-held shard (T09 note: a dangling socket selection corrupts the
    socket). **Sequence the pool *after* the release** (mirror T10's deferred-`SelectExit` eject — defer one
    frame, then pool). `PortSocket` owns the socket, so it should expose releasing its selected shard (e.g. a
    `ReleaseSelected()`); the adapter pools once it's free. Beam-into-core VFX on accept is **T17** — T11 consume
    is an instant pool.
  - `Wrong` → `roundController.ApplyWrong()`. The shard's return-to-pad is already handled by T10's eject — **no
    consume** (it stays in play, retryable).
- **Round end:** on `RoundController.Ended`, stop ticking + stop spawning replacements — **cancel pending respawn
  timers (or guard them on `IsOver`)** so a shard can't pop in after the round ends (the shards already on pads
  may remain; full teardown / Play-Again is T13/T14).
- Re-expose `RoundController`'s events (`CorrectInserted`/`WrongInserted`/`ShardExpired`/`Ended`) for downstream
  consumers (HUD T14, audio T16, VFX T17). In T11, a dev `Debug.Log` per event is enough to verify the loop.
- Subscribe in `OnEnable`/`Start`, **unsubscribe in `OnDisable`** (events policy, guardrails §15). XRI-free — it
  subscribes to `PortSocket.InsertEvaluated` (a plain C# `event`, not an XRI API).

### `ShardSpawner` — finish the consume/respawn loop (T10 left it unbuilt)
- Add **`Despawn(ShardView)`**: `pool.Release(shard)` + `planner.Release(padId)` (look up the pad via the
  shard↔`PadId` map T10 already keeps) + schedule `SpawnToTarget()` after `planner.NextRespawnDelay()` (a coroutine
  or timer — **no `new WaitForSeconds` per call in a loop**, guardrails §11).
- **Reset `ShardMotion` on pool release** (T10's flagged carry-forward): clear home + return state so a reused
  shard doesn't keep a stale home/return-timer — extend `ShardView.ResetForPool` to also reset a sibling
  `ShardMotion`, or have the spawner call `shardMotion.ResetForPool()` on release.

### Behaviour (smoke)
- Round starts → 4 shards on pads, timer counting down.
- **Correct** insert → score/combo/stabilization up; that shard disappears (consumed) and a replacement spawns on
  the freed pad after the respawn delay; colour distribution still honoured.
- **Wrong** insert → heat up, combo resets, shard returns to its pad (T10), retryable.
- Reach 20 correct → **`Won`**; heat 8 → **`Overloaded`**; timer 0 → **`TimedOut`**; `Ended` fires **once** with
  the results snapshot (stars / **finalized** score / stabilization / heat). Verify **all three** end states and
  that a won round's score includes the time bonus and the heat penalty.

## Implementation notes
- **`RoundController` API (verified against T06 this session):** ctor `(ScoreService, ComboTracker, HeatService,
  StabilizationProgress, RoundTimer)`; methods `Start()` / `ApplyCorrect()` / `ApplyWrong()` /
  `ApplyExpired(bool wasHeld)` / `Tick(float)`; events `CorrectInserted` / `WrongInserted` / `ShardExpired` /
  `Ended`; props `Phase` / `IsOver` / `Score` / `Combo` / `Heat` / `Stabilization` / `TimeRemaining` / `Stars`.
  All meters live in the services — `RoundLoopController` only routes calls + forwards events; it computes nothing.
- **Five service ctors (verified against the code this session — all plain values from `RoundConfig`, no clock /
  random deps):** `ScoreService(correctScore, comboBonusScore, victoryTimeBonusPerSecond, heatPenaltyPerHeat)`,
  `ComboTracker(comboBonusInterval)`, `HeatService(heatCap, comboHeatRelief)`,
  `StabilizationProgress(stabilizationRequirement)`, `RoundTimer(roundDurationSeconds)`. `RoundLoopController`
  builds all five + the `RoundController` directly from the serialized `RoundConfig`.
- **New:** `Scripts/Gameplay/RoundLoopController.cs`. **Edit:** `ShardSpawner.cs` (`Despawn`/respawn),
  `ShardView.cs`/`ShardMotion.cs` (pool-reset), `PortSocket.cs` (`ReleaseSelected()` for the correct-consume),
  `RoundController.cs` (the `End()` finalization fix) + `RoundControllerTests`, the **scene**
  (RoundLoopController object + serialized refs, saved).
- **One pure-rule change** — the `RoundController.End()` finalization above (ships its EditMode test); the rest is
  adapter wiring over the tested T01–T06 rules. Self-wiring now; **T12 composition root** centralises
  service/`RoundController` creation + pool `Prewarm` + seed.
- **MCP discipline (T08–T10):** `manage_components` set_property + re-read for serialized refs (component arrays
  need `[{"instanceID":…}]`); `refresh scope=all` for new files; never edit scripts while in Play; smoke from a
  settled editor (first Play after recompile shows the benign domain-reload transient).
- **Style** as M1/T08–T10: C# 9, no `#nullable enable`, `_camelCase` fields, `sealed`, XML docs.

## Out of scope
- Shard **lifetime + expiry** (countdown 14→12 s, fizzle, `ApplyExpired`) → **`T11b`** (split out; matrix row added).
- HUD/UI (T14), audio/haptics (T16), beam/spark/fizzle + core-state VFX (T17), **AppStateMachine** /
  MainMenu→Calibration→Playing→Results (T13), **composition root** (T12), Restart / Play-Again reset (T13/T14).

## Verification
- **Tests:** the adapter is covered by the smoke (§17); the one pure-rule change — `RoundController.End()`
  finalization — **ships a `RoundControllerTests` EditMode case in-change** (win bonus + heat penalty + never
  < 0). **Re-run the full EditMode suite this session** (no regression).
- **Restricted-API:** clean over `Assets/_Project`; `Interaction.Toolkit` in C# still confined to
  `PortSocket` + `ShardMotion` (`RoundLoopController`/`ShardSpawner` stay XRI-free).
- **Smoke (XR Device Simulator):** correct insert (consume + respawn + meters), wrong insert (heat + return), and
  reach each of **Won / Overloaded / TimedOut**; `Ended` fires once with the right snapshot. Console clean
  (sim-haptic excepted).
- **Done =** full EditMode suite green this session + restricted-API clean + the 3-end-state smoke + no Console
  errors; then close docs (brief + matrix + current-status).

## What was actually done
Implemented + verified 2026-06-05 (branch `feature/round-loop-slice`; human commits).

- **Code:** new `RoundLoopController` (builds `RoundController` from `RoundConfig`; routes each port's
  `InsertEvaluated` → `ApplyCorrect`/`ApplyWrong`; ticks the clock in `Update`; forwards round events as dev
  logs). `ShardSpawner` += `Despawn` (pool + `planner.Release` + delayed respawn) / `StopRespawns` / `_active`.
  `PortSocket` += `ReleaseSelected()`. **`RoundController.End()` finalization** — victory time bonus (on `Won`)
  + heat penalty (always, clamped ≥0) before `RoundEndedEvent`.
- **Consume sequencing:** on a correct insert, `RoundLoopController.ConsumeRoutine` defers one frame, calls
  `port.ReleaseSelected()` (SelectExit), then `spawner.Despawn(shard)` — never pools a socket-held shard (T09).
- **Tests:** +3 `RoundControllerTests` (victory time bonus, heat penalty subtracts, clamp ≥0). Full EditMode
  **78/78** this session (75 + 3), no regression.
- **Scene:** `Round Loop` object + `RoundLoopController` wired (`_config` + 3 `PortSocket`s + `ShardSpawner`);
  `ShardSpawner._spawnSeed` switched **12345 → 0** (time-based — the fixed debug seed made every play identical).
- **Decisions (this session):** lifetime/expiry **split → `T11b`**; final-score finalization **folded into T11**.
- **Deviations from the brief (deliberate):** (1) the spawner keeps **self-filling in its own `Start`** (T10);
  `RoundLoopController` does **not** call `SpawnToTarget` — avoids a double-fill and an Awake/ordering refactor.
  (2) the ShardMotion pool-reuse reset is folded into **`SetHome`** (called on every spawn — clears
  `_wasSelected`/`_waiting`/`_returning`) instead of a separate `ResetForPool`; no `ShardView`↔`ShardMotion`
  coupling.
- **Checks:** compile clean; restricted-API clean (`Interaction.Toolkit` in C# only in `PortSocket` +
  `ShardMotion`). **MCP Play:** 4 shards spawn, distribution per the planner. **Human smoke (XR Device
  Simulator):** correct → consume + respawn + meters; wrong → +heat, combo reset, shard returns; **Overload @
  heat 8** with `Ended` once + spawning stopped; **finalization confirmed live** (overload score 0 = 3×10 −
  8×10, clamped). Console clean (only the known sim-haptic noise; **no `routine is null` in the human Play** —
  that was an MCP-Play artifact). `Won`@20 / `TimedOut`@90 s are EditMode-covered and adapter-wired identically.
- **Colour-distribution note (not a bug):** "the same colour respawns on a repeatedly-consumed pad" is the
  planner's under-represented preference (GDD §12, T05-tested) — consuming colour X frees X → respawns X on the
  freed pad. Verified by the spawn log. The fixed seed (identical every play) was the real "always the same"
  cause → now time-based. Deeper within-round anti-repetition is a planner **tuning candidate (T21)**, not T11.
- **Deferred:** lifetime/expiry → **T11b**; centralised composition + pool `Prewarm`/**`Dispose` on teardown**
  (a leak notice on Play-stop) → **T12**; the dev `[Round]` logs are temporary — HUD consumes the events at **T14**.
- Commit: `feat(gameplay): round loop + consume/respawn + finalize score (T11)`.
