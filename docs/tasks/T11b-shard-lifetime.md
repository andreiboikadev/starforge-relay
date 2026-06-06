# T11b — Shard lifetime + expiry

| | |
|---|---|
| Branch | `feature/shard-lifetime` |
| Milestone | M2 — VR slice |
| Design ref | GDD §5 (loop step 8 "expired"), §10 (Energy Shards: lifetime timer; "while grabbed, lifetime slows strongly but does not fully pause"), §12 (lifetime 14 s → 12 s after 10 accepts; expired = +1 heat, combo reset only if held, fizzle + respawn), §14 (lifetime levers); guardrails §6 (Shard Views — lifetime/expired state; Round Gameplay — ApplyExpired), §11 (tick the ≤6 active shards; no `new WaitForSeconds` in loops), §16 (rule ownership), §17 (per-mechanic gate) |
| Depends on | T06 (`RoundController.ApplyExpired(bool)` — exists; combo/heat already EditMode-tested, first *called from the adapter* here), T11 (`ShardSpawner.Despawn`/respawn + `_shardPads`; `RoundLoopController`) |
| Touches scenes/prefabs | minor — set the new `RoundConfig._heldLifetimeFactor` on the existing `RoundConfig` asset; no structural scene/prefab change (the shard prefab already carries `ShardView` + `ShardMotion`) |
| Status | ✅ done |

## Goal
Add the pressure layer that completes the M2 vertical slice: each shard runs a lifetime countdown
(14 s, → 12 s after 10 accepts; slowed while grabbed), and on expiry fizzles → `RoundController.ApplyExpired(wasHeld)`
(+1 heat; combo reset only if it was held) → consumed + respawned via T11's `Despawn`. Closes GDD loop
step 8 and turns "lose only by wrong-insert / time-out" into the full correct / wrong / **expired** slice.

## Acceptance criteria

### Pure rule — `ShardLifetime` (`StarforgeRelay.Gameplay`, plain C#, EditMode-tested)
- `float Remaining`; ctor `ShardLifetime(float duration)` + `Begin(float duration)` for pool reuse.
- `Tick(float deltaTime, bool isHeld, float heldFactor)` — decrement by `deltaTime` when free, by
  `deltaTime * heldFactor` when held; clamp ≥ 0.
- `bool IsExpired => Remaining <= 0`.
- `static float DurationForAccepts(int accepts, float start, float late, int lateAfterAccepts)`
  → `accepts >= lateAfterAccepts ? late : start`.
- **EditMode tests:** counts down by dt (free); held drains slower but never below 0 and not fully
  paused (0 < heldFactor < 1 still decreases); not expired before 0, expired at/after 0;
  `DurationForAccepts` 9 → 14, **10 → 12** (GDD "after 10 correct accepts"), 20 → 12.

### `RoundConfig` — held-slow factor
- Add `[SerializeField] private float _heldLifetimeFactor = 0.25f;` + `public float HeldLifetimeFactor`
  (GDD §10 "slows strongly but does not fully pause"; tunable §14). Set & confirm the value on the existing
  `RoundConfig` asset after adding the field.

### `ShardMotion` — expose held state + force-release (keep XRI confined here)
- `public bool IsHeld => _grabInteractable != null && _grabInteractable.isSelected;` (reuse the value it
  already computes each frame). The spawner reads this so it stays XRI-free — XRI in C# remains confined
  to `PortSocket` + `ShardMotion`. (`isSelected` is true for a hand grab **or** a holding socket — fine for
  MVP: a socketed shard is transient and won't sit long enough to expire.)
- `public void ForceRelease()` — if `_grabInteractable.isSelected`, cancel the selection via
  `interactionManager.CancelInteractableSelection(...)` (**verify the exact XRI 3.3.0 API**); **no-op when not
  selected**. Releases a hand-held shard before it is pooled (the held-expiry hazard).

### `ShardSpawner` — own the per-shard countdown + expiry
- **Track lifetime per active shard in one place (no desync):** widen the existing `_shardPads` value from
  `int` to a small `ActiveShard { int padId; ShardLifetime lifetime; ShardMotion motion }` (class) — same dict,
  richer value. Add it in `SpawnAt`, **remove it in `Despawn`** so a pooled shard is never ticked again.
- In `SpawnAt`: `Begin` the lifetime at `ShardLifetime.DurationForAccepts(accepts, cfg.ShardLifetimeStart,
  cfg.ShardLifetimeLate, cfg.LateLifetimeAfterAccepts)` and cache the shard's `ShardMotion` (one
  `GetComponent` at spawn, never per-frame). `accepts = _acceptsProvider?.Invoke() ?? 0` (the injected
  `Func<int>` seam — RoundLoopController sets it; default 0, which is correct at the initial fill, so
  spawner/loop `Start` order is safe — late lifetime only matters at ≥10 accepts).
- `Update` (**only while `_active`**, so `StopRespawns` halts ticking at round end): tick each active shard's
  lifetime with `(Time.deltaTime, motion.IsHeld, cfg.HeldLifetimeFactor)`. **Collect the newly-`IsExpired`
  shards into a reusable buffer during the loop, then raise `event Action<ShardView, bool> ShardLifetimeExpired`
  (bool = wasHeld) *after* the loop** — the handler's `Despawn` removes the shard from `_shardPads`, so raising
  mid-`foreach` would mutate the collection being iterated (throws). Reuse the buffer + struct enumerator —
  no per-frame allocation (§11/§18).
- Reuse `Despawn(shard)` for the expiry consume (pool + free pad + scheduled respawn) — identical to the
  accept path; fizzle-vs-beam is feedback (T17). **`Despawn` calls the cached `motion.ForceRelease()` before
  `pool.Release`** — the single guard that no interactor holds a shard being pooled (hand on expiry; the
  socket is already released by `ConsumeRoutine` on a correct insert, so it is a no-op there).

### `RoundLoopController` — route expiry
- In `Start`: set `_spawner.AcceptsProvider = () => _round.Stabilization` (the late-lifetime seam) and
  subscribe `_spawner.ShardLifetimeExpired += OnShardLifetimeExpired` (unsubscribe in `OnDisable`).
- `OnShardLifetimeExpired(shard, wasHeld)` (ignored when `Phase != Playing`): `_round.ApplyExpired(wasHeld)`,
  then `_spawner.Despawn(shard)`. The hand-release-before-pool lives inside `Despawn`
  (`motion.ForceRelease()`), so the controller needs no `ShardMotion` ref and stays XRI-free.
- Also subscribe `_round.ShardExpired` and forward it (dev `Debug.Log`, like Correct/Wrong) for HUD/audio/VFX.
  (`RoundController.ApplyExpired` self-guards on `Phase`, so a late call is already a safe no-op.)

### Behaviour (smoke)
- Leave a shard untouched → fizzles at ~14 s → heat +1, combo unchanged (not held), respawns after delay.
- Grab and hold → countdown slows (doesn't pause); hold past expiry → expires, releases from the hand,
  fizzles, respawns; combo resets to 0. (To observe this without a ~50 s hold, temporarily shorten
  `ShardLifetimeStart` or raise `HeldLifetimeFactor` in the sim, then revert.)
- 14 → 12 switch is unit-covered; smoke confirms the expiry path + heat/combo.
- Heat 8 via expiries → **Overloaded**, `Ended` once. Console clean (sim-haptic excepted).

## Implementation notes
- **`ApplyExpired` is built *and* tested** (T06): +1 heat, combo reset only if held, raises `ShardExpired`,
  can end in overload; `RoundControllerTests.Expired_Held/Not_Held` cover the combo branch. T11b only
  *calls* it (first adapter use) and adds the countdown that decides *when* it fires.
- **Key hazard — never pool a selected interactable** (the T09/T11 lesson). Centralise the guard in
  `ShardSpawner.Despawn`: it calls the cached `ShardMotion.ForceRelease()` (no-op if unselected) right before
  `pool.Release`. Covers expire-while-hand-held without `RoundLoopController` touching XRI, and is a no-op for
  a correct insert (the socket was already released in `ConsumeRoutine`). Expiry needs **no** one-frame defer —
  it is raised from `Update`, not from a socket select callback.
- **Lifetime ownership (decision):** pure `ShardLifetime` ticked by the spawner (single owner of the active
  set, ≤6 shards — §11-ok) — chosen over a new per-shard MonoBehaviour or folding the countdown into
  `ShardMotion` (untestable math in a MonoBehaviour + an event on its deliberately event-free design). The
  math stays in the tested pure class; the spawner is the thin driver.
- **New:** `Scripts/Gameplay/ShardLifetime.cs`, `Tests/EditMode/ShardLifetimeTests.cs`. **Edit:**
  `RoundConfig.cs`, `ShardSpawner.cs`, `ShardMotion.cs`, `RoundLoopController.cs`, the `RoundConfig` asset.
  **Style:** C# 9, `_camelCase`, `sealed`, XML docs (as M1/T08–T11). **MCP:** `refresh scope=all` for new
  files; re-read serialized refs after `manage_components`; never edit scripts in Play; **verify the XRI
  cancel-selection API against the installed XRI 3.3.0** before relying on it.

## Out of scope
- Fizzle/smoke **VFX** + core "heating" visual → T17 (T11b consume is an instant pool, no fizzle FX).
- HUD lifetime/heat display → T14. Composition root (centralise the `Func<int> accepts` seam + pool
  `Prewarm`/`Dispose`) → T12. Audio/haptics → T16.
- **Pause** (T13): when the round can pause, the lifetime tick must pause with it (GDD §16). No pause state
  exists yet — out of scope here, flagged so T13 wires it.

## Verification
- **Tests:** new `ShardLifetimeTests` ship in-change (cases above); re-run the **full** EditMode suite
  (85/85 — was 78 pre-T11b) — no regression. Combo-on-expire is already covered (`RoundControllerTests`); **add the gap case** —
  expired shards driving heat to `heatCap` → `Ended(Overloaded)` fires once.
- **Restricted-API:** clean over `Assets/_Project`; XRI in C# still only `PortSocket` + `ShardMotion`
  (`ShardSpawner`/`RoundLoopController` stay XRI-free — held-state via `ShardMotion.IsHeld`, release via
  `ShardMotion.ForceRelease`).
- **Smoke (XR Device Simulator):** the three behaviours above; **re-verify the T11 correct-insert consume +
  respawn still works** (the `_shardPads`→record refactor touches that shared path); reach **Overloaded** via
  expiries; `Ended` once; Console clean (sim-haptic excepted).
- **Done =** full EditMode green this session + restricted-API clean + expiry smoke + no Console errors;
  then close docs (brief ✅ + matrix + current-status).

## What was actually done
Implemented + verified 2026-06-06 (branch `feature/shard-lifetime`; human commits).

- **Pure rule:** `ShardLifetime` (countdown; held-slow via `heldFactor`; `DurationForAccepts` 14→12 after 10 accepts)
  + `ShardLifetimeTests` (6 cases). One extra `RoundControllerTests.Expired_Shards_Drive_Overload_End_Once`.
- **Adapters:** `ShardSpawner` ticks each active shard's lifetime and (per the brief) **collects expiries during the
  loop and raises `ShardLifetimeExpired` after it** (no enumerator-mutation crash); `Despawn` now
  `ForceRelease`s any holding interactor before pooling. `RoundLoopController` routes expiry →
  `RoundController.ApplyExpired(wasHeld)` + `Despawn`, sets `AcceptsProvider = () => Stabilization`, forwards
  `_round.ShardExpired` as a dev log. `ShardMotion` += `IsHeld` (non-XRI read for the spawner) + `ForceRelease`
  (verified `CancelInteractableSelection(IXRSelectInteractable)` against XRI 3.3.0 via reflection — the explicit
  cast avoids the obsolete overload). `RoundConfig` += `_heldLifetimeFactor` (0.25). Active-shard tracking
  widened `_shardPads` value `int` → `ActiveShard {padId, lifetime, motion}` (one record, no desync).
- **Checks:** compile clean; EditMode **85/85** this session (78 + 6 + 1); restricted-API clean; XRI in C# still
  only `PortSocket` + `ShardMotion` (spawner/loop XRI-free via `IsHeld`/`ForceRelease`); `_heldLifetimeFactor:
  0.25` persisted on the asset.
- **Smoke (XR Device Simulator, human):** passive expiry (heat climbs, shards fizzle + respawn), **held shard
  expires → pops out of the hand** (`[Round] expired … comboReset True` at heat 7), Overload@8 fires `Ended`
  once; 8 individual expiry events (no mass-despawn bug). Console clean (only the known sim-haptic noise).
- **Deferred (tuning → T21, not a correctness item):** the initial 4 shards share spawn time + lifetime, so when
  left untouched they expire in a **synchronized wave** (looks like "all vanish at once"); desyncs in real play
  via staggered respawns. Lifetime jitter / cadence is a T21 tuning candidate. Held-slow `0.25` (≈56 s held) is
  generous — confirm/tune on device.
- Commit: `feat(gameplay): shard lifetime countdown + expiry (T11b)`.
