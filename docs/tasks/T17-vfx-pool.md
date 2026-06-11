# T17 — VFX pool + core state visuals

| | |
|---|---|
| Branch | `feature/vfx-pool` |
| Milestone | M4 — feedback |
| Design ref | GDD §10 (Star Core states + port correct/wrong visuals), §12 (RoundComplete per-result timing 2 / 1.5 / 1 s), §17 (Visual Direction — the VFX list + "keep particle counts low", fake glow), §22 (cut line — victory = scale-up + ring spin + burst, wrong = red flash, expired = disappear + sound), §26 (perf — bursts < 0.5–1.0 s, pool VFX, ≤6 shards, no real-time shadows, avoid transparent overdraw); guardrails §6 (Audio/VFX/Haptics are **peers** reacting to shared semantic events; `VfxPool`/`BeamVfx`/`SparkVfx`/`FizzleVfx`/`ComboPulseVfx`/`StabilizeVfx`/`CoreStatePresenter`; "core presents states driven by events — the view does not compute progress/heat"), §11 (short animation coroutines owned by views; cancel on disable), §12 (pooling — beam 4–6, VFX 6–10 per effect; reset on release; **don't** pool the single core/rings/ports), §15 (typed events), §16 (rule ownership — core/ring visuals = `ReactorCoreView`/`CoreStatePresenter`; Results reads final state), §18 (fake glow over bloom); [ADR 0001](../architecture/adr/0001-tech-baseline.md) (manual DI, `ObjectPool<T>`, Single Pass Instanced) |
| Depends on | T11 (round-loop events + the new spatial seams), T16 (the sibling-controller pattern). Soft: T13/T14 (`AppStateMachine.PhaseChanged` for Dormant; the RoundComplete delay seam) |
| Touches scenes/prefabs | **yes** — extend `ReactorCoreView` visuals; new pooled-VFX prefabs (beam / spark / fizzle) + core-anchored burst effects (combo-pulse / victory / overload); a `Vfx` object holding `VfxController` + `CoreStatePresenter`; new serialized refs on the composition root; 3 new `RoundConfig` delay fields |
| Status | 🟡 in progress (authoring — pending human validation; nothing implemented yet) |

## Goal
The **second feedback peer** (sibling to T16's audio/haptics): pooled, short-lived **VFX** for every GDD-§17
moment — correct **beam** (port → core), wrong **spark + red flash** (at the port), expired **fizzle** (at the
shard), **combo pulse**, **victory** burst, **overload** vent — plus the reactor core's **state visuals**
(Dormant → Charging → Heating → AlmostStable → Stabilized / Overloaded), all driven by the **same surfaced
round events** through their **own** sibling controllers. No gameplay-rule change; nothing routed through
`AudioService`; `FeedbackCue` is **not** widened (guardrails §6 — Audio/VFX/Haptics are peers, not a chain).
This task also lands the deferred **per-result RoundComplete timing** (GDD §12), which the composition root and
`RoundCompleteState` both park here. After T17 the reactor visibly reacts and **M4 (feedback) closes**; next is
**M5 art** (T18 import + dress) → T19 final glow, then M6 device.

> **Placeholder-now, art-later (matches the GDD dev order §32 and the as-built notes).** T17 builds the
> reactive **systems** (pool, controllers, the core state machine, the spatial seams, the timing) with
> **primitive emissive / simple-particle placeholders**. Final glow materials, halo billboards, and the
> particle art are **T18–T19** (`ReactorCoreView` already says "real glow/materials are T19";
> `ShardColorPalette` says the same). Don't block T17 on pretty VFX — build the wiring that T19 then re-skins.

## Decisions (resolved at authoring — validate before coding)

1. **Two new sibling adapters, peers of `FeedbackController` (guardrails §6 / §16).**
   - **`VfxController`** (MonoBehaviour, new) — maps the **positional** moments to **pooled** effects: beam,
     spark+flash, fizzle. Mirrors `FeedbackController` exactly: the composition root injects everything via
     `Initialize(...)`; it subscribes there and unsubscribes in `OnDestroy`.
   - **`CoreStatePresenter`** (MonoBehaviour, new) — maps round state to the **core-anchored** visuals (the
     steady glow state + the combo-pulse / victory / overload bursts, all at the one core locus). Drives
     `ReactorCoreView`; owns no rules and computes no progress/heat (guardrails §6/§16).
   - *Rejected:* extending `FeedbackController` to also do VFX, or routing VFX through `AudioService` /
     widening `FeedbackCue` into a VFX bus (guardrails §6; T16 decision 6 forward note — peers, not a chain).

2. **Responsibility split = positional (pooled) vs core-anchored (toggled), so we pool exactly what §12 says
   to pool and nothing more.**
   - **`VfxController` → pooled positional effects** (frequent, need a world origin): **beam** (port→core),
     **spark + red flash** (at the wrong port), **fizzle** (at the expiring shard). These are pooled
     (guardrails §12: "pool shards, **beams**, and **spark/fizzle** VFX").
   - **`CoreStatePresenter` → core-anchored effects** (rare, one locus): **combo pulse ring**, **victory**
     burst, **overload** vent, and the steady **state glow**. These live **on the core** as pre-placed
     instances toggled on (like the core/rings themselves) — **not** pooled and **not** Instantiated
     mid-round (guardrails §12 "do not over-pool the single core, rings, ports, static props").
   - Rationale: combo-pulse/victory/overload always play at the core, one at a time → a pool buys nothing;
     beam/spark/fizzle vary in position and can briefly overlap → pool them. Keeps the pool surface minimal.

3. **Spatial origin for the pooled effects comes from the already-gated `RoundLoopController`, via three new
   adapter-level companion events — NOT from raw port/shard events.** *(This is the one event-surface growth
   in T17 — validate the shape/naming.)*
   - The beam/spark need the **port**; the fizzle needs the **shard** — but **no existing event carries a
     Transform or a colour** (`CorrectInsertEvent` = combo/milestone/stabilization; `WrongInsertEvent` = heat;
     `ShardExpiredEvent` = heat/comboWasReset). Subscribing VFX directly to `PortSocket.InsertEvaluated` or
     `ShardSpawner.ShardLifetimeExpired` would **bypass the round's pause/phase gate** — XRI select still
     fires at `timeScale 0`, and shards linger socketable after the round ends (during RoundComplete/Results
     before `StopRound`) — producing **spurious sparks**. `RoundLoopController` is the single place that both
     **holds the port/shard** and **gates** on `_paused` / `Phase == Playing`.
   - So `RoundLoopController` raises, **inside its existing gated handlers**, three companion events that
     carry the spatial source (recommended shape — confirm at validation):
     - `event Action<PortSocket> CorrectInsertedAt;` — in `OnInsertEvaluated` when `outcome == Correct`.
     - `event Action<PortSocket> WrongInsertedAt;` — in `OnInsertEvaluated` when `outcome == Wrong`.
     - `event Action<ShardView> ShardExpiredAt;` — in `OnShardLifetimeExpired`, **raised before
       `_spawner.Despawn(shard)`** (the handler must read `shard.transform.position` synchronously, because
       `Despawn` pools/deactivates the shard the same call).
   - `VfxController` reads `port.transform` + `PortColor` and `shard.transform` + `Color`, tinting via the
     shared `ShardColorPalette.Resolve(...)`. The **existing** rule events (`CorrectInserted` etc.) stay
     **unchanged** — HUD (T14) and audio (T16) are untouched; the core-anchored visuals reuse them (combo
     pulse off `CorrectInserted.IsMilestone`, victory/overload off `RoundEnded.Result`).
   - *Rejected:* (a) putting a `Transform` on the rule structs — they're headless `RoundController` payloads,
     no `UnityEngine` allowed; (b) VFX subscribing to ports directly + self-gating on `IsPaused` — still
     misses the post-round phase gate and duplicates per-port wiring; (c) colour→port lookup from the payload
     — impossible (no colour in the payloads).
   - *Alternative to weigh:* fold data into the spatial event (`Action<PortSocket, CorrectInsertEvent>`) to
     avoid one controller subscribing to both the rule event and its companion. Recommended **against**: the
     beam needs only the port, the combo pulse needs only `IsMilestone` (already on `CorrectInserted`), and
     the core-anchored visuals are a different subscriber (`CoreStatePresenter`) anyway — the minimal
     `Action<PortSocket>` keeps the rule payload clean.

4. **Core state visuals = emissive + scale + ring spin driven by live meters, never bloom (GDD §17/§26;
   guardrails §18).** `CoreStatePresenter` derives a `CoreVisualState` enum
   `{ Dormant, Charging, Heating, AlmostStable, Stabilized, Overloaded }` (GDD §10) from the round events
   **as triggers**, reading `RoundLoopController`'s live `Heat` / `Stabilization` getters for the current
   values (never recomputing them — guardrails §16):
   - `RoundStarted` → `Charging` (the GDD "Active/Charging" working state).
   - `CorrectInserted` → stay `Charging`; cross a near-victory threshold → `AlmostStable` (GDD "Almost
     Stable: stronger glow, more rings lit"). Threshold from `RoundConfig` (e.g. `Stabilization >=
     StabilizationRequirement - 2`, or a ratio) — a **tuning lever** (confirm on device at T21; pick a
     sensible default now).
   - `WrongInserted` / `ShardExpired` → when heat is high (e.g. `Heat >= HeatCap - 2`) show **`Heating`**
     (GDD "Heating: core pulses red/orange"); Heating **overlays** the charge tint and yields back to
     Charging/AlmostStable when heat drops (combo-milestone relief). Threshold = tuning lever.
   - `RoundEnded.Result` → `Stabilized` (Won) / `Overloaded` (Overloaded) / hold last (TimedOut shows partial,
     GDD §12 — i.e. stay at the charge level reached, no special burst).
   - `AppStateMachine.PhaseChanged` → `Dormant` on `MainMenu` / `Calibration` / `Boot` (GDD "Dormant: dim
     sphere, slow idle rotation"). The machine is already injected into presenters (e.g. `ResultsPresenter`),
     so it's available.
   - **Read live, not from payloads.** Evaluate from `roundLoop.Heat` / `roundLoop.Stabilization` (the T14
     HUD getters — current post-apply values) on each event via one `RefreshState()`, **not** from the
     individual event payloads: they are partial (`CorrectInsertEvent` has no `Heat`;
     `WrongInsertEvent` / `ShardExpiredEvent` have no `Stabilization`). Reading payloads would miss the
     **combo-milestone heat relief** (heat −1 fires on a *correct* insert) and leave the core stuck `Heating`
     until the next wrong/expire — a subtle desync.
   - `ReactorCoreView` (extended) **owns** every core visual GameObject (glow renderer via
     `MaterialPropertyBlock` like `ShardView`, ring transform, the burst effects) and exposes **intent**
     methods — `SetVisualState(CoreVisualState)`, `PlayComboPulse()`, `PlayVictory()`, `PlayOverload()`. The
     **presenter decides when**, the **view decides how** (guardrails §6/§14/§16). View animations run on
     **scaled** time so they freeze naturally while paused (`timeScale 0`); the RoundComplete bursts use their
     own short coroutines. The view's prefab/default visual reads as **Dormant** — no bright-core flash before
     the first `PhaseChanged`.
   - **Boundary:** the **numeric heat bar / charge ring segments are the HUD's** (T14 `HUDView`/`HUDPresenter`,
     off `Heat`/`Stabilization`). T17's core "Heating"/"AlmostStable" is the **core glow** reaction, not the
     HUD readout — no duplication of the heat/stab source of truth (guardrails §16).

5. **`VfxPool` mirrors `ShardPool` (guardrails §12; ADR 0001 `ObjectPool<T>`).** A small generic
   `VfxPool<T>` (or `ObjectPool<T>` per effect) — factory `Func<T>` injected, `Get/Release/Prewarm/Dispose`,
   `collectionCheck` in dev, reset-on-release delegated to the effect's own `ResetForPool()`; `IDisposable`,
   disposed by the root (the `ShardPool` pattern verbatim). One pool per pooled effect (beam ~4–6; spark/fizzle
   ~6–10 each, §12 — tunable). A pooled effect plays on `Play(...)` and **auto-returns after its duration** via
   a short view-owned coroutine (≤1.0 s, GDD §26 / guardrails §11; cancel on disable). A generic `VfxPool` is
   justified by ≥3 pooled types (the "second real use" bar). Signatures differ by shape — beam spans two
   transforms, spark/fizzle are point bursts:
   - `BeamVfx.Play(Transform from, Transform to, Color tint)` — orient/scale a primitive between port and core.
   - `SparkVfx.Play(Vector3 position, Color tint)` / `FizzleVfx.Play(Vector3 position, Color tint)`.

6. **Per-result RoundComplete timing (GDD §12) — the one testable piece — lands in `RoundConfig` +
   `AppStateMachine`/`RoundCompleteState`.** Add `RoundConfig` fields `_victoryResultDelaySeconds = 2`,
   `_overloadResultDelaySeconds = 1.5`, `_timeoutResultDelaySeconds = 1`. `RoundCompleteState.Enter()` selects
   the delay from `_machine.LastResult.Result` (it is set **before** the state change — verified in
   `AppStateMachine.OnRoundEnded`). The composition root passes the three values (replacing the single
   `RoundCompleteDelaySeconds = 1.5f` const) into `AppStateMachine`; recommended carrier = a small
   `readonly struct RoundCompleteDelays { Won; Overloaded; TimedOut; For(RoundPhase); }` in `StarforgeRelay.App`
   (confirm shape at validation). **Covered by new `AppStateMachineTests` cases** (Won → 2 s, Overloaded →
   1.5 s, TimedOut → 1 s elapse before `AdvanceToResults`). *This is separable from the VFX* — flagged — but
   every doc (the composition-root comment, `RoundCompleteState`'s summary, T16's Out-of-scope) assigns it
   here with the feedback content, and the victory/overload bursts want the right beat. Recommend keeping it
   in T17.

7. **No new package, no ADR, XRI confinement unchanged.** Pool/controllers/effects reference **no XRI type**
   (they take `PortSocket` / `ShardView` / `Transform` — gameplay views, not XRI); `RoundLoopController`'s new
   companions pass those same gameplay types, so it stays XRI-free. The restricted-API grep set is
   **unchanged**: `{ PortSocket, ShardMotion, HapticService, StarforgeRelayCompositionRoot }`. Everything is
   reversible scene/material work inside the sanctioned baseline (`ObjectPool<T>`, emissive, manual DI) → no
   ADR. *(A custom SPI-correct glow shader would warrant a note, but MVP uses emissive + halo billboards per
   GDD §17 — no custom shader.)*
   - **Namespace/folder (minor — confirm):** new feedback-visual code under `Scripts/Vfx/`
     (`namespace StarforgeRelay.Vfx`), mirroring how T16 added `Scripts/Audio` / `StarforgeRelay.Audio`.
     `ReactorCoreView` + `CoreVisualState` stay in `Scripts/Gameplay` (`StarforgeRelay.Gameplay`) — the core
     view's existing home; `StarforgeRelay.Vfx` → `Gameplay` is a one-directional ref (as Audio already does).

## Acceptance criteria

### Spatial seams (`StarforgeRelay.Gameplay` — small edits, no rule changes)
- `RoundLoopController` += `event Action<PortSocket> CorrectInsertedAt`, `event Action<PortSocket>
  WrongInsertedAt`, `event Action<ShardView> ShardExpiredAt` — raised **inside** the existing gated handlers
  (`OnInsertEvaluated` for the two inserts; `OnShardLifetimeExpired` **before** `Despawn` for expiry). No
  payload struct; no per-frame allocation; stays XRI-free. The existing `CorrectInserted` / `WrongInserted` /
  `ShardExpired` / `RoundStarted` / `RoundEnded` / `IsPaused` are unchanged.

### `ReactorCoreView` (extend, `Scripts/Gameplay/`)
- Keeps `CoreAnchor` (beam target). Owns the glow renderer (tint via `MaterialPropertyBlock`), the ring
  transform, and the combo/victory/overload burst effects. Exposes `SetVisualState(CoreVisualState)`,
  `PlayComboPulse()`, `PlayVictory()`, `PlayOverload()`. **View only** — no event subscriptions, no rules,
  no reads of heat/stabilization. Animations on scaled time (freeze on pause). Placeholder emissive/particles
  now; final glow = T19.
- `CoreVisualState` enum (new, `Scripts/Gameplay/`): `Dormant, Charging, Heating, AlmostStable, Stabilized,
  Overloaded` (GDD §10).

### `CoreStatePresenter` (new MonoBehaviour, `Scripts/Vfx/`)
- `Initialize(RoundLoopController roundLoop, ReactorCoreView core, AppStateMachine machine, RoundConfig
  config)`; subscribe in `Initialize`, unsubscribe in `OnDestroy` (the `HUDPresenter`/`FeedbackController`
  pattern). **Init-order contract:** `Initialize` runs from the composition root's **`Awake`** (before
  `AppFlowController.Start` calls `machine.Begin()`), so it catches the initial `PhaseChanged` → Dormant —
  the established presenter pattern (HUD/Results subscribe the same way). Wiring it late misses the first
  state and leaves the core in its prefab default until the next transition.
- Maps events → `CoreVisualState` + burst calls per decision 4: `RoundStarted`→Charging;
  `CorrectInserted`→Charging/AlmostStable (+ `PlayComboPulse()` on `IsMilestone`); `WrongInserted`/`ShardExpired`
  → Heating when heat high; `RoundEnded`→`PlayVictory()`+Stabilized / `PlayOverload()`+Overloaded / hold
  (TimedOut); `PhaseChanged`→Dormant on MainMenu/Calibration/Boot. Thresholds read from `RoundConfig`
  (tuning levers). The result→state mapping is a trivial switch (no rule logic — T14 "trivial display
  mapping" precedent; no separate unit test). Reads `roundLoop.Heat` / `roundLoop.Stabilization` **live** for
  the threshold decisions, not the partial event payloads (decision 4).

### `VfxController` (new MonoBehaviour, `Scripts/Vfx/`)
- `Initialize(RoundLoopController roundLoop, ReactorCoreView core, <vfx pools/prefabs>)`; subscribe in
  `Initialize`, unsubscribe in `OnDestroy`. **No `SettingsService`** — there is no "VFX off" setting (GDD §16
  = Sound + Haptics only); VFX always plays.
- Subscribes the **three spatial companions only**: `CorrectInsertedAt(port)` → `BeamVfx.Play(port.transform,
  core.CoreAnchor, Resolve(port.PortColor))`; `WrongInsertedAt(port)` → `SparkVfx.Play(port.transform.position,
  Resolve(port.PortColor))` + the port's red flash; `ShardExpiredAt(shard)` →
  `FizzleVfx.Play(shard.transform.position, Resolve(shard.Color))` (read position synchronously). No pause
  gating needed — the companions are only raised from the round loop's already-gated handlers. Beam origin =
  `port.transform` (a dedicated port beam-anchor like `CoreAnchor` is a T19 polish, not needed now).

### `VfxPool` (new, `Scripts/Vfx/`) + pooled effects
- Generic `VfxPool<T>` mirroring `ShardPool`: `Func<T>` factory, `Get/Release/Prewarm/Dispose`, dev
  `collectionCheck`, reset-on-release → effect's `ResetForPool()`; `IDisposable`. Capacities: beam 4–6,
  spark/fizzle 6–10 (§12, tunable).
- `BeamVfx` / `SparkVfx` / `FizzleVfx` (pooled, positional) — `Play(...)` then **auto-return after duration**
  (≤1.0 s; view-owned coroutine, cancelled on disable). `ResetForPool()` clears transform/particles/tint and
  cancels the in-flight return.
- **Not pooled** (core-anchored, owned by `ReactorCoreView`): combo-pulse, victory, overload, the core/rings.

### Per-result RoundComplete timing
- `RoundConfig` += `_victoryResultDelaySeconds (2)`, `_overloadResultDelaySeconds (1.5)`,
  `_timeoutResultDelaySeconds (1)` with getters (GDD §12).
- `AppStateMachine` ctor takes per-result delays (recommended `RoundCompleteDelays` struct);
  `RoundCompleteState.Enter()` selects by `_machine.LastResult.Result`. Composition root builds the carrier
  from `RoundConfig` and drops the single `RoundCompleteDelaySeconds` const. The ctor-signature change is a
  **mechanical** update to existing `AppStateMachineTests` construction calls (no behavioural regression).
- **EditMode (`AppStateMachineTests`):** Won waits 2 s, Overloaded 1.5 s, TimedOut 1 s before
  `AdvanceToResults` (drive `Tick` with sub- and over-threshold unscaled deltas). No regression to existing
  cases.

### Composition root + scene
- Root: new serialized refs — `ReactorCoreView`, `CoreStatePresenter`, `VfxController`, the pooled-VFX prefabs
  (beam/spark/fizzle), and the 3 `RoundConfig` delays carrier. Construct/prewarm/inject the VFX pools in
  `Awake`; `Initialize` the two presenters; **dispose the VFX pools in `OnDestroy`** (alongside `ShardPool` +
  the audio/haptic services). Construction-only stays intact (no tick, no rules).
- Scene: a `Vfx` object with `VfxController` + `CoreStatePresenter`; extend the existing `Reactor Core`
  GameObject's `ReactorCoreView` with the glow/ring/burst children; wire every new ref and **re-read to
  verify** (MCP discipline). Saved/committed.

### Behaviour (smoke)
- Correct insert: a beam fires **from the inserted port to the core**, tinted to the port colour; the core
  steps up its charge glow; combo-5 adds a ring pulse. Wrong insert: a spark + brief red flash **on the wrong
  port**; the core shows Heating when heat is high. Expired shard: a fizzle **at the shard's last position**.
  Victory: core → Stabilized burst; Overload: red vent; both at the core. Dormant glow on the menu;
  Charging once a round starts. **No VFX while paused** (no new inserts/expiries fire through the gated loop);
  **no spurious spark** from socketing a leftover shard during RoundComplete/Results.
- RoundComplete beat lasts ~2 s on victory, ~1.5 s on overload, ~1 s on time-out before Results.
- Console clean (bar the known benign sim haptic-capability warnings from T16).

## Implementation notes
- **New:** `Scripts/Vfx/{VfxController, CoreStatePresenter, VfxPool, BeamVfx, SparkVfx, FizzleVfx}.cs`,
  `Scripts/Gameplay/CoreVisualState.cs`, the VFX prefabs + core burst children. **Edit:**
  `RoundLoopController.cs` (3 companion events), `ReactorCoreView.cs` (state + burst intent methods),
  `RoundConfig.cs` (3 delay fields), `AppStateMachine.cs` + `RoundCompleteState.cs` (per-result delay),
  `StarforgeRelayCompositionRoot.cs` (refs + construct/inject/dispose), `AppStateMachineTests.cs` (timing
  cases), the scene.
- **Verified this session (2026-06-11, code + MCP — not memory):** the scene has exactly **1 `ReactorCoreView`
  + 3 `PortSocket`**; `RoundLoopController.OnInsertEvaluated(port, shard, outcome)` and
  `OnShardLifetimeExpired(shard, wasHeld)` hold the port/shard and gate on `_paused`/`Phase == Playing`;
  the round events carry **no** Transform/colour (`CorrectInsertEvent`/`WrongInsertEvent`/`ShardExpiredEvent`);
  `ShardPool` is the `ObjectPool<T>`+`IDisposable` pattern; `ShardView.Color` + `PortSocket.PortColor` +
  `ShardColorPalette.Resolve` give the tint; the composition root passes a single `RoundCompleteDelaySeconds
  = 1.5f` (commented "per-result … lands in T17"); `AppStateMachine.LastResult` is set before the
  RoundComplete state change.
- **Pattern to mirror:** `FeedbackController` (Initialize-from-root, symmetric unsubscribe) and `ShardPool`
  (generic pool, factory injection, dispose by root).
- **Perf (GDD §26 / guardrails §12/§18):** bursts < 0.5–1.0 s; pool beam/spark/fizzle (no mid-round
  Instantiate — guardrails §10); emissive/`MaterialPropertyBlock`, **no URP bloom**, no real-time shadows,
  avoid stacked transparent overdraw across the HMD; no per-frame allocations (cache; reuse buffers; no `new
  WaitForSeconds` in loops).
- **MCP discipline (T08–T16):** `refresh scope=all` for new files; `manage_components` set + **re-read** for
  serialized refs (component arrays need `[{"instanceID":…}]`); never edit scripts while in Play; smoke from a
  settled editor (first Play after a recompile shows the benign domain-reload transient). **Scaled-canvas
  gotcha (T15) is N/A** unless a stray UI edit sneaks in. Prefab-create may leave a stray scene instance
  (delete it); asset-rename reports "failed" but succeeds on disk (verify).
- **Style** (`csharp-style.md` + `.editorconfig`): C# 9 (block namespace, no `record`/`init`), no `#nullable
  enable`, `_camelCase` fields, `sealed`, XML docs, `var` only when RHS-apparent; field-naming clean on
  touched files (IDE0044 on `[SerializeField]` is the known project-wide false positive).

## Out of scope
- **Final glow / materials / particle art** (halo billboards, real emissive look, Kenney Particle Pack
  sprites) → **T18 (import + dress)** / **T19 (final-ish glow)**. T17 is primitive/emissive placeholders.
- **Audio/haptics** for these moments already shipped in **T16** — VFX is a peer; it does **not** play sound
  or touch `AudioService`/`FeedbackCue`.
- **Final mix / VFX intensity / threshold tuning** (AlmostStable & Heating thresholds, burst sizes, beam
  duration) → **T21** on device (GDD §14/§24 tuning levers — defaults now, not changed speculatively).
- **HUD heat bar / charge-ring numeric readout** stays T14's (`HUDView`/`HUDPresenter`); T17 only adds the
  core **glow** reaction.
- **Recenter/calibration** (the T16 device "empty bay" finding) → **T20**. **Git LFS** → **T18**.
- **No gameplay-rule changes** — the tested T01–T06 core and all adapters' behaviour stay identical; T17 only
  listens (and adds the per-result *display* delay, which is app-flow timing, not a round rule).

## Verification
- **Tests:** the **per-result RoundComplete timing is the only pure-rule piece** → ships `AppStateMachineTests`
  cases in the same change (Won 2 s / Overloaded 1.5 s / TimedOut 1 s). The VFX/core adapters are
  Unity-visual boundaries (no pure rule) → covered by the smoke pass, per the §17 gate. **Re-run the full
  EditMode suite this session** (103 today per current-status → 103 + the new timing cases) — **no
  regression**.
- **Restricted-API:** clean over `Assets/_Project`; the `Interaction.Toolkit` grep set is **unchanged**
  (`{ PortSocket, ShardMotion, HapticService, StarforgeRelayCompositionRoot }`) — VFX code is XRI-free; no
  `Resources.Load` (prefabs come via serialized refs / the pool factory); field-naming clean on touched files.
- **Smoke (XR Device Simulator / MCP Play, human):** the Behaviour list above — beam from the correct port,
  spark+flash on the wrong port, fizzle at the shard, core charge/heat/stabilized/overload glow, combo pulse,
  per-result RoundComplete beat, no VFX while paused, no spurious spark post-round. Read the Console (clean bar
  the known benign sim haptic warnings).
- **Perf sanity:** no per-frame GC in an active round (pools + `MaterialPropertyBlock`); bursts short. Full
  on-device FPS/FFR profiling is **T21**, not here.
- **Done =** full EditMode suite green this session (incl. the new timing cases) + restricted-API clean +
  field-naming clean on touched files + the VFX/core smoke + no Console errors; then close docs — brief
  Status ✅ + **What was actually done**, matrix row, `current-status.md`. (No asset-ledger change — no new
  third-party asset in T17; that's T18.)

## What was actually done
— (not started; implementation begins only on explicit command after this brief is validated and committed.)
