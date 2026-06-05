# T10 — Reactor core + feeder pads + spawner + lerp-back

| | |
|---|---|
| Branch | `feature/reactor-spawn` |
| Milestone | M2 — VR slice |
| Design ref | GDD §5 (loop), §9 (play space / reach), §10 (Star Core, Feeder Pads, Energy Shards), §12 (spawn rules, dropped-shard lerp-back); guardrails §6 (Reactor Core; Shard Views — `ShardMotion`), §11 (`ShardMotion.Update` for the ~4 shards is OK), §12 (pooling); [ADR 0001](../architecture/adr/0001-tech-baseline.md) |
| Depends on | T05 (`ShardSpawnPlanner`, `FeederPadSlot`, `SpawnArea`, `ShardSpawnPlan`, `IRandom`/`SeededRandom`), T08 (`ShardView`, `ShardPool`, `Shard.prefab`) |
| Touches scenes/prefabs | **yes** — new `ReactorCore` + 4 feeder pads committed to the scene under a `Reactor` root; the 3 T09 ports re-parented under it; new `FeederPad.prefab` + pad material; new `ShardSpawner` object. Extends `ShardView` indirectly (new `ShardMotion` component on `Shard.prefab`). Edits `PortSocket` (wrong-eject → return-to-pad). |
| Status | 🟡 in progress |

## Goal
The **spatial + spawn half** of the VR slice: shards actually appear and live on feeder pads in front of a
reactor core, and behave predictably when dropped. Wires the pure planner (T05) + pooled grabbable shard
(T08) to the scene, and gives the ports (T09) the **real return-to-pad** that replaces their temporary
wrong-eject shove. Leaves all *rule consequences* (score/heat/combo/progress, consume-on-accept, expiry) to
the round-loop adapter (T11).

## Scope cut (read — deviates from a T08 note)
The matrix scopes T10 as exactly **"core + feeder pads + spawner + lerp-back"**. T08's out-of-scope note
additionally parked *lifetime ticking/expiry* in T10; **this brief moves lifetime/expiry to T11** and keeps
T10 to the matrix title. Why: an expired shard's effects (**+1 heat, combo reset if held**) and the
**14→12 s lifetime drop after 10 accepts** depend on `HeatService`/`ComboTracker`/`StabilizationProgress`
state that only exists once `RoundController` is wired (T11). Splitting the *trigger* (T10) from the
*consequence* (T11) would be a half-mechanic. So T10 = the half that needs **no** RoundController and is fully
smoke-demonstrable without scoring; T11 = consume-on-accept + expiry + lifetime + all rules/events, built on
the spawner scaffolding T10 lays down. **`idle bob/rotate` and `grab-state` stay in T10** (the lerp-back needs
grab-state, and idle motion makes the smoke readable). *(If you'd rather pull lifetime/expiry back into T10,
say so at doc review — it's the one open fork.)*

## Acceptance criteria

### Scene — reactor + pads (committed)
- **`Reactor` root** (empty) at the play-space front; the **3 existing ports** (`Port Solar/Ion/Pulse`, currently
  scene-root) re-parented under it unchanged (same world pose: chest height y≈1.2, z≈0.8–0.85, facing player).
- **`ReactorCore`**: a primitive **sphere** ("toy-like", GDD §17) centred **just behind the ports** (ports are
  at z≈0.8–0.85, y≈1.2, within §9's 0.75–0.95 m), **sized/posed in-editor so the ports sit on/just in front of
  its face — the core mesh must not intersect the ports** (tune visually; do *not* hard-code an overlapping pose
  such as a 0.3–0.4 m sphere centred at z 0.9, which would swallow the z=0.8 ports). Placeholder material (no
  glow/emission — T19). Carries a **minimal `ReactorCoreView`** exposing a `Transform` anchor (future beam/charge
  target for T17); **no** state logic yet.
- **4 feeder pads** (GDD §12 MVP layout: front-left, mid-left, mid-right, front-right) instanced from a new
  **`FeederPad.prefab`**, committed under `Reactor`, in the comfortable forward arc (−50…+50°, GDD §9) at/just
  below shard height, **within reach** — never behind the player. Each carries a `FeederPadView` exposing a
  stable **`int PadId`** (= its index 0–3) and the spawn/return anchor `Transform`.

### `ShardSpawner` (MonoBehaviour adapter, `StarforgeRelay.Gameplay`) — the heart of T10
- Serialized: `RoundConfig`, the `Shard.prefab` (or a shard-prefab reference), the 4 `FeederPadView[]`
  (index = `PadId`), the pooled-shard container `Transform`, optional spawn seed.
- On init builds the pure services **by passing their deps into their constructors** (manual-DI; serialized refs
  in, no scene searches):
  - a **`ShardPool`** (T08) whose `Func<ShardView>` instantiates `Shard.prefab` under the container;
  - a **`ShardSpawnPlanner`** (T05) from `RoundConfig` (`ActiveShardsDefault`=4, `MaxShardsPerColor`=3,
    `RespawnDelayMin/Max`), the **`FeederPadSlot[]` derived from each pad's pose** — horizontal angle in the
    **`Reactor` root's local frame** (rotation-safe; *not* a hidden world-+z assumption) and **floor-relative
    height** (Floor origin) — a single source of truth, no separately-authored numbers to drift; a `SpawnArea`
    (the §9 −50…+50° arc + height band) sized to **encompass the 4 placed pads** (its job here is to catch a
    *gross* misplacement, not to clip our deliberately in-reach MVP pads); the 3 `ShardColor`s; a `SeededRandom`.
    Keep the `Reactor` root at play-origin / floor level so this frame matches §9's reach intent.
- **`Prewarm`s** the pool, then **fills to target**: repeatedly `planner.TryPlanNextSpawn(out plan)` →
  `pool.Get()` → place at the `PadId` pad anchor → `shardView.SetColor(plan.Color)` →
  `shardMotion.SetHome(padAnchor)`. Result: **4 shards, colour-distributed per the planner** (≤3 of a colour,
  ≥2 colours present), one per pad.
- **Under-fill guard — never fail silently:** if the fill loop ends with `ActiveCount < target` (a pad fell
  outside `SpawnArea`, or there are fewer pads than the target), **log a dev `LogWarning`** naming the cause
  (stripped from demo builds, guardrails §19). This — not an EditMode test — is the real guard against the
  planner silently dropping a misplaced pad; the smoke then also *shows* fewer than 4 shards.
- **T10 implements only what it uses:** `SpawnToTarget()` (the initial fill) + the **shard↔`PadId` bookkeeping**
  (so T11 can find a shard's pad). **Do not pre-build the unused T11 machinery** — `Despawn` / `pool.Release` +
  `planner.Release(padId)` + the `NextRespawnDelay()` refill arrive in T11 with consume/expiry; T10 just keeps
  the planner, pool, and the shard↔pad map reachable for it. **No XRI types** in `ShardSpawner` (it gets
  `ShardView`/`ShardMotion` off the prefab) — keeps it out of the XR-confined adapter set.

### `ShardMotion` (MonoBehaviour adapter on `Shard.prefab`) — idle + grab-state + return-to-pad
- Owns the **`Update`** for the ~4 shards (sanctioned, guardrails §11). It is the one new **XRI-touching** shard
  adapter (`ShardView` stays XRI-free): holds a serialized `XRGrabInteractable` ref and **polls `isSelected`**
  (verified on `XRBaseInteractable` in the installed 3.3.0 — `XRBaseInteractable.cs:506`). **No hand-vs-socket
  distinction needed:** *selected by anything* (a hand **or** a correctly-holding socket) ⇒ the interactor owns
  the transform ⇒ **suppress idle + return**; *not selected* ⇒ free.
- **Idle:** gentle bob + slow rotate while **free** at its home pad.
- **Return-to-pad (GDD §12 lerp-back) — poll-based, no event subscriptions** (so there's no subscribe/unsubscribe
  lifecycle to leak across pooling): in `Update`, detect the **selected→free** transition, wait **0.5–1.0 s**
  (serialized, ~0.7), then lerp **kinematically** to the `SetHome` anchor; a **grab cancels** it. No
  physics/ballistics (`throwOnDetach` already false, T08).
- `ReturnToPad()` — **immediate** return (skips the delay; shares the same return path → idempotent),
  **null-safe** if no home is set; called by `PortSocket` after a wrong-insert eject.
- `SetHome(Transform)` from the spawner at spawn. **T11 reset note:** once shards are pooled (T11),
  `ShardMotion` state (home, return timer, last-selected flag) must reset on release (extend
  `ShardView.ResetForPool` or add a sibling reset), or a reused shard keeps a stale home / mid-return.

### Port wrong-eject → return-to-pad (edit `PortSocket`)
- On a **Wrong** insert, **inside the existing eject routine and *after* the deferred-one-frame, non-obsolete
  `SelectExit`** force-release (keep it — `socketActive`/`keepSelectedTargetValid` alone don't drop a held shard),
  call the shard's **`ShardMotion.ReturnToPad()`** (fetch via `GetComponent` on the shard within the routine —
  on-select, not per-frame, so allowed; null-safe if absent) instead of the raw `_ejectDistance` teleport.
  **Order matters:** call it *after* `SelectExit`, never while the socket still selects the shard (the socket
  would fight/re-snap it). **Remove `_ejectDistance`** (the scripted return replaces the T09 placeholder shove,
  as T09 foretold). The re-select-loop guard (deferred exit + `recycleDelayTime`) stays; smoke must still show
  **no flicker loop**.

### Behaviour (smoke)
- Round start → **4 colour-distributed shards** appear on the 4 pads, bobbing.
- Grab a shard on **Grip** → idle motion stops, it follows the hand; **release in empty space** → after the
  delay it **lerps home** to its pad. Re-grab mid-return cancels the return.
- **Wrong-colour** insert → force-ejected and **returns to its pad** (no flicker/loop), retry possible.
- **Correct-colour** insert → accepted/stays socketed (the consume→pool→beam→respawn loop is **T11**).

## Implementation notes
- **New:** `Scripts/Gameplay/ShardSpawner.cs`, `Scripts/Gameplay/ShardMotion.cs`,
  `Scripts/Gameplay/ReactorCoreView.cs`, `Scripts/Gameplay/FeederPadView.cs`,
  `Prefabs/Gameplay/FeederPad.prefab`, a pad material. **Edit:** `Scripts/Gameplay/PortSocket.cs`
  (eject→return; drop `_ejectDistance`), the **scene** (`Reactor` root + core + 4 pads, ports re-parented,
  spawner object — **saved/committed**), `Shard.prefab` (add `ShardMotion`).
- **No `ReactorConfig` SO yet** (YAGNI): the pad slots come from the scene transforms; `SpawnArea` bounds + core
  pose live as `ShardSpawner` serialized fields / `RoundConfig`. Promote to a `ReactorConfig` SO at T17 when a
  second consumer (core distance/states) earns it (guardrails §7 names it; introduce on 2nd use).
- **XRI 3.3.0 (verified against the installed package source this session):** `XRBaseInteractable.isSelected`
  **exists** (`XRBaseInteractable.cs:506`) — `ShardMotion` polls it; the hand-vs-socket distinction is **not
  needed** (selected-by-anything suffices), so `ShardMotion` need not reference `XRSocketInteractor`.
  `PortSocket`'s non-obsolete deferred `SelectExit` stays as in T09. Verify any *further* XRI API at impl.
- **Pooling (§12):** `Prewarm(8)`-ish; **never `Destroy` shards** — T10 only `Get`s for the initial fill,
  `Release` is T11. Container reparent on release already handled by `ShardPool`.
- **MCP discipline (T08/T09):** `manage_gameobject create` may not apply `component_properties` → set via
  `manage_components set_property` + **re-read to verify**; `refresh scope=all` for new files; the asset-rename
  tool reports "failed" but succeeds on disk; **never edit scripts while the Editor is in Play** (domain reload
  corrupts the live XR session — the T09 lesson).
- **Style** (`csharp-style.md` + `.editorconfig`): C# 9 (block namespace, **no `record`/`init`**), **no
  `#nullable enable`** (match M1/T08/T09), `_camelCase` fields, `sealed`, `readonly` where possible, XML docs on
  public members, member order static→instance→props→ctor→methods, `var` only when RHS-apparent.

## Out of scope
- **RoundController wiring — score/heat/combo/stabilization + typed `CorrectInsert`/`WrongInsert`/`ComboMilestone`/
  round-end events** → **T11** (consumes T09 `PortSocket.InsertEvaluated` + T10 spawner).
- **Consume-on-accept** (accepted shard → beam → pool → free pad → respawn) and **shard lifetime + expiry**
  (countdown, fizzle, +1 heat / combo-reset-if-held, 14→12 s after 10 accepts) → **T11**.
- **Beam VFX (port→core), spark/reject/fizzle/combo-pulse VFX, core state visuals/glow** → T17; **emission/halos/
  shape markers** → T18–19; **audio/haptics** → T16.
- **Named "Shard" interaction layer + rig-mask separation** → T14 (still on **Default**; if smoke shows the UI
  ray or Poke grabbing shards, pull it earlier).
- **Composition root / `Prewarm` + seed wiring centralised** → T12 (T10 self-wires in the spawner for the slice).

## Verification
- **Tests:** T10 is **adapter-heavy with no new pure rule** (the planning logic is T05, already covered) → no new
  *required* EditMode unit test; covered by the smoke pass per the §17 gate (like T09). The silent "<4 active"
  risk is guarded at **runtime by the spawner's under-fill `LogWarning`** + the smoke (you can *see* 4) — an
  EditMode test can't read the scene pad transforms, so a hardcoded-values test would check the *design*, not
  reality, and give false comfort. If T10 introduces any genuinely pure helper, it ships EditMode tests in-change.
- **Regression:** **run** the full EditMode suite **this session** (don't cite the prior 75/75) — confirm green,
  no regression from the `PortSocket` edit / asmdef untouched.
- **Restricted-API:** `rg` over `Assets/_Project` for the guardrails-§10 forbidden set **plus** `Interaction\.Toolkit`
  confined to the adapter set — now **`PortSocket`, `PortView`, `ShardView`(XRI-free), `ShardMotion`** (the new
  XRI-touching shard adapter). `ShardSpawner`/`ReactorCoreView`/`FeederPadView` must **not** reference XRI.
- **Smoke (human — XR Device Simulator / Quest Link):** start scene → 4 colour-distributed shards on the 4 pads,
  bobbing; Grip-grab with each hand; drop in space → lerp home; wrong insert → returns to pad, **no flicker/loop**;
  correct insert → accepted. Console clean (the known benign XRI sim-haptic noise excepted, per T08; re-check on
  device at T20).
- **Done =** full EditMode suite green this session + restricted-API (incl. XRI-confinement) clean + the
  spawn/grab-drop-return/wrong-return smoke passes + no Console errors (sim-haptic excepted); then close docs
  (brief Status + matrix + current-status).

## What was actually done
`—` (not started; implementation begins after the docs commit lands).
