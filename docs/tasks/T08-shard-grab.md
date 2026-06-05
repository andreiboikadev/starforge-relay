# T08 — Shard prefab + grab (Grip) + ShardPool

| | |
|---|---|
| Branch | `feature/shard-grab` |
| Milestone | M2 — VR slice |
| Design ref | GDD §10 (Energy Shards), §11 (Grip = grab), §26 (pool shards); guardrails §6 (Shard Views), §12 (pooling), §13 (XRI: `throwOnDetach=false`, kinematic, no ballistic throwing); [ADR 0001](../architecture/adr/0001-tech-baseline.md) (pooling = `ObjectPool<T>`; grab-interactor follow-up) |
| Depends on | T01 (`ShardColor`) |
| Touches scenes/prefabs | **prefab only** — new `Shard.prefab` + `Shard.mat`. **No committed scene change** (grab smoke uses transient, unsaved instances). |
| Status | ✅ done |

## Goal
First interactive object in the scene: a poolable, grabbable energy **shard**. Makes the existing
Near-Far/Grip rig (T07) actually *do* something, and gives the spawner (T10) a pooled shard to place.
No ports, no scoring, no spawning logic yet. T07 explicitly deferred controller **grab feel** to here.

## Architecture decision (reversing an earlier draft — read this)
T08 introduces **no XRI in C#**. ADR 0001 + guardrails §4 mandate a **single** `StarforgeRelay.Runtime`
asmdef + manual DI — there is *no* separate XR assembly. Adding XRI to `Runtime` would delete the
compile-time guard that *pure rules can't reference XR* for the whole assembly. T08 doesn't need XRI in
code: **grab is the prefab's `XRGrabInteractable` component + the rig**, not C#. So `Runtime` stays
`references: []` (XR-free). XRI first enters `Runtime` in **T09** (the `XRSocketInteractor` port adapter),
where it is genuinely required; from then on the pure/adapter split is held by discipline (pure rules =
plain C#, no `MonoBehaviour`/Unity statics) + the restricted-API check, exactly as the baseline intends.

## Acceptance criteria
- **asmdef UNCHANGED.** `StarforgeRelay.Runtime.asmdef` keeps `references: []`. No new asmdef.
- **`ShardView`** (`StarforgeRelay.Gameplay`, MonoBehaviour adapter, **XRI-free**):
  - `public ShardColor Color { get; private set; }`.
  - `void SetColor(ShardColor)`: stores `Color` and tints `_BaseColor` via a **cached `MaterialPropertyBlock`**
    (no per-instance `Material` → no leak). Null-safe (no `Renderer` → just stores `Color`).
  - `void ResetForPool()`: re-enables the shard's `Collider` (the §12 collider-enabled reset). Grab-interactable
    enable / lifetime / grab-state resets are added in T09/T10 when those mechanics exist. Null-safe.
  - `Awake` caches `Renderer` + `Collider` (`GetComponentInChildren` fallback). **No XRI refs, no `Update`**
    (zero per-frame cost). Constructible in an EditMode test without a prefab (all paths null-safe; `Awake`
    doesn't run in edit mode).
- **`ShardPool`** (`StarforgeRelay.Gameplay`, **infra adapter** — not a pure rule, so it may touch
  `UnityEngine` (`Transform`/`Object`/`Debug`/`Application`); it just must not be a `MonoBehaviour`):
  - Wraps `UnityEngine.Pool.ObjectPool<ShardView>`; **create-func injected** (`Func<ShardView>`) → headless-testable.
  - Optional `Transform container`; on release, reparent to it **if provided** (§12 parent-transform reset;
    harmless no-op when null). Create-func parents new shards under it.
  - `Get()` activates + returns; `Release(s)` → `ResetForPool()` + deactivate (+ reparent); `Prewarm(n)`
    pre-instantiates. `DefaultCapacity = 8`, `MaxSize = 12` (§12: max-6-active + buffer).
  - `collectionCheck` is a **`bool?` defaulting to `null` → resolved to `Debug.isDebugBuild`**: double-release
    guard ON in editor/dev, **OFF in release** so a stray double-release can't crash the demo. Tests pass `true`.
  - Destroy callback is **edit-mode-safe**: `Application.isPlaying ? Destroy : DestroyImmediate`.
- **`Shard.prefab`** at `Assets/_Project/Prefabs/Gameplay/Shard.prefab`:
  - ~**0.12 m** primitive (Sphere) + `Shard.mat`.
  - `Rigidbody` **isKinematic = true, useGravity = false** (so it never falls when not grabbed — the #1 smoke-killer).
  - `Collider` **larger than the visible mesh** (forgiving grab; GDD §10/§31), non-trigger.
  - `XRGrabInteractable`: `throwOnDetach = false`, `movementType = Kinematic`, `retainTransformParent = true`,
    `interactionLayers = Default` (the rig's Near-Far selects Default — named "Shard" layer is T09).
  - `ShardView` attached. (The grab component lives **on the prefab**, satisfying guardrails §6 "owns the grab
    interactable" without the C# referencing XRI.)
- **`Shard.mat`**: URP Lit, default (white) base; per-shard colour comes from `ShardView`'s MPB `_BaseColor`.
  **No emission/glow in T08** (real glow/halos = T19). *Note:* MPB bypasses the SRP Batcher for these renderers —
  negligible at ≤6 shards; T19 owns the final material strategy.
- **Grab works:** in the XR Device Simulator / Quest Link, either hand grabs a shard on **Grip**; it follows
  kinematically; on release it does **not** fly off (no ballistic throw).

## Implementation notes
- **New files:** `Scripts/Gameplay/ShardView.cs`, `Scripts/Gameplay/ShardPool.cs`,
  `Tests/EditMode/ShardPoolTests.cs`, `Prefabs/Gameplay/Shard.prefab`, `Materials/Shard.mat`
  (+ `Prefabs/Gameplay/` and `Materials/` folders). **No asmdef edits. No committed scene edits.**
- **Verified vs installed packages (XRI 3.3.0 / OpenXR 1.16; ADR 0001):** rig unpacked (T07), Near-Far ×2 +
  Poke ×2, one `XROrigin` (Floor) + one `XRInteractionManager`. Near-Far `selectInput` = action **"Select"**,
  bound to **`{GripButton}`** in `XRI Default Input Actions` (verified in the asset) → grab is on Grip.
  Interactor `interactionLayers = Default (value 1)` → a Default-layer shard is grabbable with no extra setup.
  `XRGrabInteractable` ∈ `UnityEngine.XR.Interaction.Toolkit.Interactables`; `throwOnDetach` / `movementType` /
  `retainTransformParent` exist (`gravityOnDetach` obsolete).
- **Pooling (§12):** warmup 8 / max 12; reset on release; **never `Destroy` shards mid-round**. `Prewarm` is
  called by the composition root (T12), not T08.
- **Style (`csharp-style.md` + `.editorconfig`):** C# 9 (block-scoped namespace; **no `record`/`init`**), **no
  `#nullable enable`** (matches the M1 files), explicit access modifiers, `sealed`, `readonly` fields, member
  order static→instance→properties→ctor→methods, XML docs on public members, `var` only when RHS-apparent.
- **ADR close (on done):** update ADR 0001 Follow-Up — record "grab interactor = **Near-Far**, confirmed by the
  T08 grab slice" (and tick the locomotion-strip follow-up already completed in T07).

## Out of scope
- **Named "Shard" interaction layer** → T09 (shard/UI separation). **NOTE for T09:** moving shards off Default
  also requires adding "Shard" to the rig interactors' `interactionLayers` mask, or grab breaks.
- **Port sockets + color validation wiring** → T09 (consumes `PortValidationService`, T04).
- **Feeder pads, `ShardSpawner`, lerp-back-to-pad, lifetime ticking/expiry, idle bob/rotate (`ShardMotion`),
  grab-state tracking (`IsGrabbed` / lifetime-slows-while-held)** → T10.
- **Composition root, pool `Prewarm` wiring, `collectionCheck` dev-flag wiring** → T12.
- **Real glow / emission / halos / shape markers** → T19. **Audio/haptics on grab** → T16.
- Transient grab-smoke instances are **not committed**; T10's spawner produces the real shards.

## Verification
- **Tests (EditMode, headless — fake `Func<ShardView>` factory that news `GameObject` + `ShardView`, tracked +
  `DestroyImmediate` in `[TearDown]`):** `Get` returns an active shard; `Release` deactivates + returns to pool;
  `Get` **reuses** a released instance; `Prewarm` fills; `SetColor` stores the color; null factory throws
  `ArgumentNullException`; double-release throws when checked *(confirm the exact exception type at write-time —
  expected `InvalidOperationException`)*. These protect **our** wiring, not Unity's `ObjectPool`.
- **Compile clean** (`read_console` — 0 errors).
- **Restricted-API check:** `rg` over `Assets/_Project` — `ShardView`/`ShardPool` use only `GetComponent` in
  `Awake` (allowed setup) + pooling; no `Find`/`Camera.main`/per-frame LINQ/`Update`.
- **Regression:** **run** the full EditMode suite this session (do **not** cite the prior 68/68) — confirm no
  regression + the new pool tests pass.
- **Smoke (human — XR Device Simulator / Quest Link; T07 deferred grab feel to here):** drop 1–3 `Shard`
  instances in reach (transient, **scene not saved**), grab on Grip with each hand, carry, release; confirm
  kinematic follow + **no throw**; Console clean. MCP can't drive a Grip-grab (no `execute_code`) → human pass.
- **Done =** compile clean + full EditMode suite green **this session** + restricted-API clean + grab smoke
  passed; then close docs (brief Status + matrix + current-status) **and** ADR 0001 grab-interactor follow-up.

## What was actually done
Implemented + verified 2026-06-05 (branch `feature/shard-grab`; human commits).

- **Code (XRI-free):** `ShardView` (colour identity + `MaterialPropertyBlock` `_BaseColor` tint + `ResetForPool`)
  and `ShardPool` (`ObjectPool<ShardView>`, injected factory, optional container reparent, `Prewarm`,
  `collectionCheck` → `Debug.isDebugBuild`, edit-mode-safe destroy). **`StarforgeRelay.Runtime.asmdef`
  untouched** — pure rules keep their XR-free compile guard; XRI enters in T09.
- **Prefab:** `Prefabs/Gameplay/Shard.prefab` — 0.12 m sphere + `Materials/Shard.mat` (URP Lit); Rigidbody
  `isKinematic`+no-gravity; SphereCollider r=0.65 (forgiving, non-trigger); `XRGrabInteractable`
  (`throwOnDetach=false`, `movementType=Kinematic`, `retainTransformParent`, Default layer); `ShardView`.
- **Tests:** `ShardPoolTests` — 7 EditMode cases. Full suite **75/75** green (68 prior + 7 new, no regression).
  Restricted-API scan of `Assets/_Project` clean.
- **Grab smoke (human, XR Device Simulator):** Grip-grab with both controllers, kinematic follow, no throw,
  no fall — confirmed. Console shows only 2 **benign** XRI haptic-capability errors on the *simulated*
  controllers (`HapticImpulseCommandChannelGroup`); not from T08 code, absent on real Touch controllers —
  device console re-checked at T20.
- **Deviations from plan:** none material. Named "Shard" layer stays on **Default** (T09 owns it); glow/emission
  deferred to T19 (T08 tints base colour only). The in-scene smoke shard is transient (scene not saved/committed).
- **Tooling caught:** `manage_gameobject create` silently didn't apply `component_properties` (set via
  `manage_components set_property` + re-read to verify); `refresh scope=scripts` doesn't import new files (used `scope=all`).
- **Commits:** `feat(gameplay): add shard prefab + grab + ShardPool (T08)` + this doc-close.
