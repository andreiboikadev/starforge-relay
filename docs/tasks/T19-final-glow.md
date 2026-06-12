# T19 — Final-ish colours / glow on core / shards / ports

| | |
|---|---|
| Branch | `feature/final-glow` |
| Milestone | M5 — art |
| Design ref | GDD §10 (Star Core states, Reactor Ports shape markers, Energy Shards halo), §13/§17 (palette + visual direction), §22 (cut line — red flash), §24 (readability checks), §25 (demo bar — "final-ish colors and glow"), §27/§30 (colour-assist shapes), §32 step 10; guardrails §6 (Core/Ports/Shard views), §10 (forbidden APIs), §12 (pool reset), §13 (XR/SPI), §17 (test gate), §18 (fake-glow/perf); [ADR 0001](../architecture/adr/0001-tech-baseline.md) (Mobile URP, SPI, Vulkan) |
| Depends on | **T18** (done — the dressed bay + fake-glow material kit + Kenney **Particle Pack** import this builds on); T11/T17 (the interactables + VFX placeholders being reskinned) |
| Touches scenes/prefabs | yes — `Shard`/`Port` materials + prefabs, the `Reactor` core (new `ReactorCore.mat` + `_ring` child) in `StarforgeRelay.unity`, the `BeamVfx`/`SparkVfx`/`FizzleVfx` prefabs (re-pointed off the shared URP default + new particles); new first-party materials/prefabs under `Assets/_Project/`; **small runtime C#** (`PortView` flash + `PortSocket` getter + `VfxController` wiring; comment fixes) |
| Status | 🟡 in progress |

## Goal

Reskin the gameplay **interactables** so the demo reads as the GDD's "bright magical-tech reactor": give the
**core, shards, and 3 ports** final-ish colours + fake glow (emissive/unlit + a halo building block), add the
GDD **shape markers** (Solar circle / Ion triangle / Pulse diamond) for colour-blind readability, add the
core's missing **`_ring`** child, wire the dedicated **port wrong-insert flash**, and reskin the **T17 VFX
placeholders** (beam / spark / fizzle) with the Kenney Particle Pack sprites imported at T18. This is the
**interactable half of GDD dev-order step 10** (fake glow / beams / particles) and the T18↔T19 cut line's T19
side. It takes the build to the §25 demo bar's "shards, ports, and reactor use final-ish colors and glow" and
unblocks the **T20** device build / **T21** look-tuning. **No gameplay-rule change** — visuals + thin adapters
only; the pure rules and the 105 EditMode tests are untouched.

## Decisions (resolved at authoring — validate before coding)

1. **Glow path = URP Unlit + HDR `_BaseColor` (MCP-authorable), matching the T18 kit; reserve a human 1-click
   only where a keyword truly needs it.** This is the central decision, forced by a verified constraint: the
   views tint via `MaterialPropertyBlock`, and **an MPB cannot toggle the URP `_EMISSION` shader keyword** —
   the current `Shard.mat` / `Port.mat` are URP **Lit with `_EmissionColor {0,0,0}` and no `_EMISSION`
   keyword** (verified in-file), so they show **no glow** whatever the MPB writes. GDD §17 says "prefer
   **unlit/emissive** materials for shards, ports, UI, and core," and T18 already proved the Unlit-HDR path via
   `GlowSolar.mat` (Unlit, `_BaseColor` 4.2/2.6/0.3). Resolution per surface:
   - **Shards & ports** drive only `_BaseColor` → reskin their materials to **URP Unlit** and raise the palette
     to **HDR** (see Decision 5). **Zero C# change**, fully MCP-authorable, GDD-sanctioned. Lit bay lighting no
     longer dims them (intended — they're light sources).
   - **Beam / spark / fizzle** (`BeamVfx`, `SparkVfx`/`FizzleVfx`) **also ride that same shared URP default
     material** today → each needs its **own new** first-party material (beam = **Unlit-HDR**; base carries the
     glow, emission ignored, no C# change). **Never edit the shared default in place** — it would change the
     core, both bursts, and the feeder pad at once. (Spark/fizzle particle work is Decision 6.)
   - **Core** (`ReactorCoreView`) is the exception: its whole state model is an **emission-intensity ramp**
     (`_EmissionColor = color × intensity`, `ReactorCoreView.cs:180`). **Verified gotcha (this corrects the v1
     plan):** the core's `_glowRenderer` currently points at the **URP package _default_ `Lit.mat`** (guid
     `31321ba15b8f8eb4c954353edc038b1d`, the URP `m_DefaultMaterial`) — there is **no owned core `.mat`**, and the
     same default also backs `FeederPad` + all 3 VFX prefabs. So "tick `_EMISSION` on the core material" is
     **impossible/unsafe as a 1-click**: the package default is read-only (reverts on package reimport) and
     editing it would flip emission for 4 other renderers. **Recommend (new work, not an in-place tick): create a
     dedicated first-party `Assets/_Project/Materials/ReactorCore.mat` (URP Lit), assign it to `_glowRenderer` in
     the scene (replacing the package default), then the human ticks `_EMISSION` once on that owned asset** — only
     then does the existing ramp glow, with **no `ReactorCoreView` C# change**. *Rejected alternative:* refactor
     `ReactorCoreView` to fold intensity into `_BaseColor` on an Unlit material — more C# for no visible benefit.
     **Validate the create-and-assign approach before I code the core.**
2. **Wrong-insert port flash routed through the existing gated path (not PortSocket's own select event).** Add
   `PortView.FlashWrongInsert()` (MPB flash → red/white **then back to the steady tint**, **scaled time** so it
   freezes on pause, no material instance); expose `PortSocket.PortView`; have **`VfxController.OnWrongInsertedAt`**
   call it alongside the spark (**null-guard `PortView` first** — `PortSocket._portView` is serialized null and
   only `GetComponentInChildren`-resolved in `Awake`). **The flash restores by re-resolving
   `ShardColorPalette.Resolve(_portColor)`** (the socket's `_portColor` is the single source — guardrails §6),
   **not** a read-back of the MPB (which returns the flash colour) or a hard-coded white. *Why this seam:* the
   spark already fires from the round loop's **gated** `WrongInsertedAt`, so the
   flash inherits the same gating (no spurious flash from a shard socketed after the round ends / while paused).
   *Rejected:* flashing from `PortSocket.OnSocketSelectEntered` directly — that event is **not** gated, so it
   would flash at the wrong times. (This matches the `VfxController` code comment that anticipated touching
   `PortView`/`PortSocket` at T19.)
3. **Shape markers = a child mesh/decal per interactable, present on both shard and port of a colour** (GDD §10: Solar = circle/sun-ring, Ion = triangle, Pulse =
   diamond; §27/§30 give the optional Colour-Assist context). Built from primitives/quads with an
   Unlit material (no per-frame script). Always-on (not gated behind a Colour-Assist toggle) for MVP readability;
   the §15 Settings "Color assist" toggle stays **out of scope** (optional GDD item, no consumer wired).
4. **Halo billboard (the T18-deferred fake-glow building block) = Unlit-HDR quad or a ParticleSystem billboard,
   NOT a hand-authored additive-transparent material.** The **shard halo is a child of the `Shard` prefab** (so it
   inherits the grab/bob/spin transform from `ShardMotion`), carries **no collider on the Default layer**, and —
   unlike the pooled positional VFX (beam/spark/fizzle) — is **not** a separate pooled object. MCP cannot flip the
   URP `_SURFACE_TYPE_TRANSPARENT` keyword / render-queue (verified T18) and `execute_code` is broken, so an
   additive halo material can't be authored via MCP. Use an **Unlit opaque HDR quad** (MCP-authorable, no
   transparent overdraw — best for Quest) or a **ParticleSystem** in billboard mode with a Kenney glow sprite —
   the renderer handles facing, so **no per-frame camera-facing script** is needed (nor wanted: no XR-camera
   reference is plumbed in `Assets/_Project` today, so a facing script would have to inject a cached XR camera via
   the composition root — never `Camera.main` per frame, guardrails §10/§13). If we decide we want a true additive
   halo, that's a single human 1-click material toggle — flag it at validation.
5. **Palette retune lives in code now (`ShardColorPalette` HDR consts + `ReactorCoreView` `s_*Color`);
   device-final numeric tuning is T21.** "Final-ish" (§25) = the colours/intensities read well in-editor and
   distinct on the bay; exact HDR intensities, bloom interplay, and on-Quest brightness are **T21** levers
   (deliberately not chased speculatively here). Keeping the palette a shared single source preserves the
   shard=port colour-match invariant (guardrails §6).
6. **VFX reskin scope = the 3 pooled placeholders + the core bursts, kept low-density (GDD §17/§26).** Beam,
   spark, fizzle get final materials/sprites (Kenney Particle Pack). The core `_comboPulse` / `_victoryBurst` /
   `_overloadVent` (currently optional/unassigned, glow-only fallback) get real low-count bursts **or** keep the
   documented glow fallback (GDD §22 explicitly allows victory = brighter material + ring spin + a burst). No new
   pooled effect *types* (reuse the T17 `VfxPool<T>` + prefabs).
7. **Feeder pads stay as-is (`FeederPadView` is anchor-only — no view glow exists yet, and pads still ride the
   shared URP default material); their final glow is optional polish → T21.** T19's title and the cut line are
   core/shards/ports; pads are not regressed, just not reskinned here.

## Acceptance criteria

- **Shards glow & are colour-distinct.** `Shard` material is **URP Unlit (or Lit with `_EMISSION` ticked)** so
  the `ShardView._renderer` `_BaseColor` MPB tint actually glows; the 3 colours (Solar/Ion/Pulse) read as
  bright, saturated, and mutually distinct against the dark bay (GDD §24). Shard keeps its forgiving grab
  collider (larger than the visible mesh, GDD §10) and a **halo/sparkle** (Decision 4). No change to
  `ShardView`'s C# contract (still tints `_BaseColor`).
- **Ports glow, carry shape markers, and flash on wrong insert.** Each port renders its palette colour with
  glow + a **shape marker** (Solar circle / Ion triangle / Pulse diamond, GDD §10). `PortView` gains
  **`FlashWrongInsert()`** (MPB, scaled-time, no material instance); `PortSocket` exposes its `PortView`;
  `VfxController.OnWrongInsertedAt` calls it (Decision 2). The flash fires **only** on a real gated wrong insert
  (not after round end / paused), verified in the smoke pass.
- **Shards & ports share one colour source.** `ShardColorPalette.Resolve` remains the single source for shard,
  port, and VFX tints; a Solar shard and Solar port read identically (guardrails §6). Palette raised to HDR
  (Decision 5).
- **Core has final glow + a ring.** A **dedicated first-party core material** (`Assets/_Project/Materials/ReactorCore.mat`,
  URP Lit, `_EMISSION` human-ticked) is created and assigned to `ReactorCoreView._glowRenderer`, **replacing the
  shared URP package default** (Decision 1) — only then does the `CoreVisualState` emissive ramp show. A
  **`_ring`** child mesh is added and assigned to `ReactorCoreView._ring`, so the idle/active ring spin (already
  coded) is no longer a no-op. Victory/overload/combo read clearly — via real
  low-count bursts assigned to `_victoryBurst`/`_overloadVent`/`_comboPulse` **or** the documented glow fallback
  (Decision 6). State transitions (Dormant→Charging→Heating→AlmostStable→Stabilized/Overloaded) still come from
  `CoreStatePresenter` unchanged (the view computes no heat/progress, guardrails §6/§16).
- **T17 VFX placeholders reskinned (net-new authoring, not a material swap).** `BeamVfx`/`SparkVfx`/`FizzleVfx`
  today render off the **shared URP default material** and have **no ParticleSystem** (`SparkVfx`/`FizzleVfx` are
  mesh-only spheres, `_particles` unassigned). So: give each its **own** first-party material (beam = Unlit-HDR),
  and **add + wire a billboard `ParticleSystem`** (Kenney sprite, short low-count burst) to spark/fizzle, assigning
  `BurstVfx._particles`. Each still tints to the event colour and **auto-returns to its `VfxPool<T>`**.
  **Pool-reset caveat:** `OnResetForPool` resets scale (beam) / stops particles (burst) but does **not** clear the
  MPB tint — fine today because `Play()` re-drives it; if the reskin adds a visual `Play()` doesn't re-set (e.g. a
  child halo renderer, a once-set particle start-colour), extend that type's `OnResetForPool` to reset it too
  (guardrails §12).
- **Particle Pack ledgered as used.** `Assets/ThirdParty/Kenney Particle Pack/` was imported at T18 as
  `✅ IMPORTED` but "not yet referenced"; flip its [asset-ledger.md](../assets/asset-ledger.md) note to record
  the sprites now referenced by the T19 VFX (no new third-party download — packs already present + CC0).
- **Perf discipline (no obvious regression; final profiling is T21).** Fake glow via emissive/Unlit-HDR + the
  **halo billboard (the bloom-independent glow anchor)**. **Honest bloom note:** the scene's `Global Volume` Bloom
  is currently **ON** (threshold 1, intensity 0.25) and HDR `_BaseColor` only *blooms* because of it — T19 targets
  that bloom-on volume; the **bloom-off readability pass is T21** (where the bloom on/off stance lives), so the
  halo (not the HDR magnitude) must carry the glow read if bloom is later disabled. No stacked transparent planes
  across the HMD view; unique materials/textures kept low; particle bursts short (~0.5–1.0 s, GDD §26);
  Single-Pass-Instanced correct. Any new runtime C# is MPB-based, allocation-free per frame, no `Camera.main` /
  scene-search per frame.
- **Stale T18/T19 comments corrected.** Drop the now-done forward-references: `PortView` "Real shape markers …
  are **T18–19**" → done; `ShardColorPalette` / `ReactorCoreView` "real glow/materials are **T19**" → done.
- **First-party vs third-party separation** (guardrails §4): new materials/prefabs under `Assets/_Project/`;
  Kenney sprites stay under `Assets/ThirdParty/`.

## Implementation notes

- **The MPB seam (do not break it).** `ShardView` / `PortView` / `ReactorCoreView` / `BeamVfx` / `BurstVfx` all
  tint via a cached `MaterialPropertyBlock` on `_BaseColor` (+ core/beam `_EmissionColor`). Keep the new
  materials on those same URP property names so the reskin is **mostly material/asset work, not C# rewrites**.
  The only runtime C# expected: `PortView.FlashWrongInsert()` (+ private flash state), a public
  `PortSocket.PortView` getter, the one `VfxController.OnWrongInsertedAt` call, and the comment fixes.
- **Files — existing to extend:** materials `Shard.mat`, `Port.mat` (→ Unlit/HDR or emission-ticked);
  `ShardColorPalette.cs` (HDR consts), `ReactorCoreView.cs` (`s_*Color` retune — no code change to assign the
  `_ring`/material; both are in-scene), `PortView.cs` (+`FlashWrongInsert`), `PortSocket.cs` (+`PortView` getter),
  `VfxController.cs` (+null-guarded flash call); prefabs `Shard.prefab`, `Port.prefab`, `BeamVfx.prefab`,
  `SparkVfx.prefab`, `FizzleVfx.prefab` (re-point each off the shared URP default + add the spark/fizzle
  `ParticleSystem`); the `Reactor` subtree in `StarforgeRelay.unity` (assign the new core material + the new
  `_ring` child + shape markers).
- **New `*(new)*`:** **`ReactorCore.mat`** (URP Lit, emission-ticked — replaces the core's shared default) + a
  **new material per VFX type** (beam/spark/fizzle, off the shared default) + per-colour glow materials +
  shape-marker meshes/materials + a halo prefab, under `Assets/_Project/Materials|Prefabs/`. **⚠ Never edit the
  URP default material (`31321ba…`) in place** — it backs the core, all 3 VFX, and the feeder pad at once.
- **MCP pitfalls (verified at T18 — this is a heavy material/scene task):** `manage_material set_material_color`
  reads RGB as **0–255**; set 0–1 / HDR via `set_material_shader_property _BaseColor [r,g,b,a]`. `manage_material`
  **cannot** flip the URP `_SURFACE_TYPE_TRANSPARENT` keyword / render-queue **nor tick the `_EMISSION`
  keyword** (`execute_code` is broken) → transparent-additive + Lit-emission materials need a **human 1-click**
  (Decisions 1 & 4). `assign_material_to_renderer` resolves `target` **by name**. `manage_gameobject create`
  ignores `component_properties`/`is_static` and adds a default collider to primitives (strip it on dressing
  meshes; shape markers must carry **no collider on the Default layer** — it would intercept a grab or the UI
  ray, the T18 finding). Re-read after every set to verify. Never edit scripts while the Editor is in Play; the
  first Play after a recompile throws a benign `routine is null`/AABB cascade — re-Play to confirm clean.
- **Render targets (ADR 0001):** Quest build uses **Mobile** URP asset / Single Pass Instanced / Vulkan / Linear
  / ASTC. Graphics-API / platform / any bloom toggle remain **human** steps (build-and-test "who does what");
  the bloom on/off stance is **T21**, not here — T19 targets the current **bloom-on** volume; the bloom-off
  readability pass is T21 (see the Perf-discipline acceptance criterion).
- **Git LFS** is still pending (T18 carry-forward) — the new T19 sprites/materials commit as plain binary and get
  swept in by the same human `git lfs migrate` when the art settles. **Do NOT add `filter=lfs` `track` lines to
  `.gitattributes` ahead of that migrate** — adding them early creates a mixed plain+LFS state for the same globs
  and risks broken pointer files on a machine without `git lfs install`; the human adds the `track` lines **as one
  operation with** `git lfs install` + `git lfs migrate import` (per the `.gitattributes` NOTE + the handoff).

## Out of scope

- **T20:** the on-device Quest 2 build / 3-end-state smoke and the deferred **Recenter/calibration** wiring (GDD §9).
- **T21:** profiling + tuning — final HDR intensities, the **bloom/tonemapping** post stance, FFR/dynamic
  resolution, texture-size passes, reach/size tuning, **Static flags** on the bay, accent intensity / starfield
  density, feeder-pad final glow.
- **Colour-Assist Settings toggle** (GDD §15/§30) — shape markers are always-on here; no settings consumer.
- **Feeder-pad reskin** beyond keeping them readable (Decision 7).
- The actual **`git lfs migrate` + force-push + re-clone** (human git step) and any other git write.
- **No gameplay-rule, scoring, spawn, validation, or XR-interaction-logic change** — the 105 EditMode rules and
  the round/socket flow are untouched.

## Verification

- **Tests:** **no new pure rules expected → no new EditMode tests** (art/material/adapter task, per the §17
  gate — same as T18). *If* coding introduces any testable pure logic (e.g. a `ShardColor → shape/marker`
  mapping as data), add EditMode coverage in the same change. **Regression gate:** re-run the **full EditMode
  suite** and confirm **105/105, no regression** (the reskin must not touch rule code).
- **Compile/validation:** MCP compile clean, **no Console errors**; **Project Validation (Android)** still passes.
- **Restricted-API check** (runtime C# is added): run the guardrails §10 ripgrep over `Assets/_Project` — no
  `GameObject.Find` / `Camera.main` / per-frame scene search in the new flash/halo code.
- **Smoke (human-realistic):** enter Play (MCP) under the **XR Device Simulator** / **Quest Link** and confirm:
  core/shards/ports glow and read colour-distinct against the bay; **shape markers** are visible and correct
  (circle/triangle/diamond); the full loop still works (grab → socket → correct beam / **wrong spark + port
  flash** / expired fizzle → win/overload/time-out); the **wrong-insert flash fires only on a gated wrong insert**
  (not paused / post-round); VFX pools still recycle (no leak/￼stuck instances); **toggle the `Global Volume`
  Bloom override OFF once and confirm core/shards/ports still read as glowing via the halo (Decision 4 bloom-off
  check), then restore bloom**; no console errors and no obvious editor frame-time cliff. **Real Quest 2
  readability/perf is the T20/T21 bar**, not this task.
- Done = full suite green + no Console errors + core/shards/ports reskinned with glow + shape markers + wrong
  flash + VFX reskinned + Particle Pack ledger updated (see [build-and-test.md](../development/build-and-test.md)).

## What was actually done

— (filled on close: what shipped, deviations, materials/prefabs/sprites used, commit/PR, date)
