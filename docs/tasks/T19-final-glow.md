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

## 🔻 SESSION HANDOFF — T19 STILL IN PROGRESS (next chat: read THIS + `../../reactor-build-recipe.md` first)

> Context ran out mid-task. T19 is **not closed**. This section is the full picture: **what's built, where it
> is, what's left to make it "дорого / богато / красиво / интерактивно", and exactly how to do it.** The
> companion `reactor-build-recipe.md` (repo root) has the exact MCP/ProBuilder steps + a verified ground-truth
> appendix — read it, then **extend it** for the remaining work below.

### A. What's DONE and committed (as-built)
All committed across several `feat(art)`/`build`/`chore` commits this session (see `git log`). No C#/gameplay
rule change anywhere; **EditMode stays 105/105**, Console clean, Play boots clean.

- **C# (committed earlier):** `ShardColorPalette` → HDR ×2.2 (Solar/Ion/Pulse); `PortView.FlashWrongInsert()`
  (MPB red flash, restores palette tint); `PortSocket.PortView` getter; `VfxController.OnWrongInsertedAt`
  fires the flash (null-guarded); `ReactorCoreView` state-palette retune. Stale "T19" comments dropped.
- **Materials (`Assets/_Project/Materials/`):** `ReactorCore.mat` (URP **Lit**, `_EMISSION` enabled **via
  hand-edited `.mat` YAML** — `m_ValidKeywords:[_EMISSION]` + `m_LightmapFlags:1` + non-black `_EmissionColor`;
  driven by the `ReactorCoreView` emission ramp). `CoreRing.mat`, `Shard.mat`, `Port.mat`, `BeamGlow/SparkGlow/
  FizzleGlow.mat`, `HaloGlow.mat`, `ShardHalo.mat`, `PortGlowSolar/Ion/Pulse.mat` — all **URP Unlit + HDR
  `_BaseColor`** (no keyword needed). `HaloGlow`/`ShardHalo` are **additive-transparent, also set via YAML**
  (the keyword/blend/queue edited in the `.mat` directly — Unity kept it on import). **KEY TRICK:** MCP can't
  toggle `_EMISSION` or the transparent surface keyword, but **editing the `.mat` YAML directly works** for
  both (verified twice this session). Use this.
- **Core:** sphere (0.25 scale, world (0,1.2,1.05)) with `ReactorCore.mat`; **`CoreRings` spin-node** (child of
  the core, `localScale (4,4,4)` to cancel the 0.25 → world-metric authoring) **assigned to
  `ReactorCoreView._ring`** → **the rings already ROTATE** (the view spins `_ring` about local Z: 10°/s Dormant
  → 140°/s Stabilized). Under it: **3 cyan ProBuilder tori** `Ring_0/1/2` (`outerRadius 0.18`, `tubeRadius
  0.012`, 8×24) in a **60° rosette** (local Euler 90/0, 90/60, 90/120), `CoreRing.mat`, collider-free.
  **`CoreHalo`** = ParticleSystem billboard (`HaloGlow.mat`, `light_01` sprite, additive, 1 looping particle).
- **Ports ×3** (white primitive spheres, 0.16): each has a **neon outline ring** (torus 0.6/0.06) + a
  **shape marker** — Solar **circle** (ProBuilder Pipe ring), Ion **triangle** (`create_poly_shape` 3-pt),
  Pulse **diamond** (4-pt) — palette-tinted `PortGlow{Solar,Ion,Pulse}.mat`, collider-free, facing the player
  (ports carry a 180° Y rotation — handled).
- **Shards:** `Shard.prefab` mesh → **`crystal_16`** (iPoly3D Crystal Pack, 28 FBX, CC0); grab collider /
  `XRGrabInteractable throwOnDetach=0` / kinematic Rigidbody preserved; `Shard.mat` Unlit-HDR (MPB-tinted at
  runtime); `ShardHalo` PS billboard child (`ShardHalo.mat`).
- **VFX** beam/spark/fizzle prefabs re-pointed off the shared URP default onto owned `*Glow.mat`.
- **Decor-collider audit clean** (no collider on the Default layer on any ring/marker/halo).
- Screenshots of the result: `Assets/Screenshots/` (`core_rosette_top.png`, `port_*_marker*.png`,
  `shard_crystal16_close.png`, `play_*`). The real **tinted+bloom** look only shows in a **Playing round**;
  Scene view shows MPB-driven things (ports/shards/core) white/untinted.

### B. Honest visual verdict right now (the gap to close)
The **core + 3 orbit rings + crystal shards + colour-coded shape markers** read well — a real reactor, not
primitives. **But it is NOT "дорого" yet**, for three concrete reasons:
1. **Ports are still plain white primitive SPHERES** with markers/rings glued on → read as "white blobs with
   dashes," not dear neon sockets. **The port BODY was never reshaped** (T19 only added glow+markers).
2. **Feeder pads are plain grey primitive DISCS** — never reskinned (deferred), look cheap.
3. **Over-bloom** — Global Volume Bloom (threshold 1, intensity 0.25) washes the scene hazy-white and blows
   the core out. Needs tuning **down** (GDD §17/§26: "fake glow over bloom").

### C. What's LEFT to make it дорого / богато / красиво / интерактивно (the next-chat work)
The user's bar: **MVP must look beautiful by the demo** (hard acceptance — GDD §25 demo bar: "shards, ports,
reactor use final-ish colours and glow" + "station bay has dressed art"). Concrete work + techniques (all
Quest-safe — keep fake-glow primary, low-poly, no transparent-overdraw stacks):

1. **Port bodies (biggest win).** Replace the white spheres with a real form: a **recessed glowing socket** (a
   dark ProBuilder Pipe/ring frame with an inner emissive disc), or mount the port on a **Kenney SSK console
   panel** (`Assets/ThirdParty/Kenney Space Station Kit/Models/FBX format/` has `computer*`, `display-wall`,
   `table-display*`). Keep the `XRSocketInteractor` trigger volume + `PortView` renderer wiring intact; just
   swap/augment the visible mesh. Tint from the palette.
2. **Feeder pads reskin + interactivity (GDD §10).** Dark-metal base + an **emissive rim ring** that **lights
   up to the spawning shard's colour before spawn** (GDD: "Pad lights up before spawning; pad colour can match
   the current shard"). `FeederPadView` is currently anchor-only (no glow) — this is both a *look* and an
   *interactivity* upgrade. May need a tiny `FeederPadView` glow method (pure adapter; if you add C#, add no
   pure-rule logic so the 105 tests are untouched — or drive it from the existing spawn event via VfxController).
3. **Bloom / brightness tuning.** Lower Global Volume Bloom intensity (≈0.25 → ~0.1) and/or raise threshold, and
   trim the hottest HDR values, so it reads crisp not washed. (`Assets/Settings/StarforgeRelayProfile.asset`,
   editable via YAML.) This is the GDD-§26 "bloom is T21 polish" lever — do a pass for the demo look.
4. **"Illusion of emitted light" shaders (the user explicitly wants this).** Options, cheapest first:
   - **Fresnel/rim emissive** on core + ports (a small custom URP shader or Shader Graph: `emission =
     baseHDR + fresnel * rimHDR`) → edges "bleed light." *Shader Graph authoring via MCP is hard — likely a
     hand-written `.shader` file or a human Shader-Graph pass; flag it.*
   - **Pulsing/breathing emission** (animate `_EmissionColor` intensity) — the core ramp already does
     per-state; add a subtle idle sine-pulse for "alive" energy.
   - **Scrolling energy texture** (UV-scroll an emissive mask on rings/core) for plasma flow.
   - **Additive halo layers** already on core/shard — **add one to each port** for the neon bloom.
5. **Ring motion polish.** Rings already rotate as a group (the `_ring` node). For a livelier atom, give each
   `Ring_i` its **own** independent spin (a tiny rotate script, or animate) on a different axis/speed.
6. **VFX reskin (T17 placeholders).** Beam (port→core), spark (wrong), fizzle (expire), combo-pulse, victory
   burst — wired but placeholder. Reskin with Kenney Particle Pack sprites (additive) for richer feedback
   (= the "интерактивно").
7. **(Optional) Crystal variety / core facet.** Try other crystals of the 28; consider a faceted gem core.

### D. HOW to do it — the proven workflow + technical playbook (reuse this)
- **Workflow that worked:** author/extend a precise MD recipe → run a **build subagent** with full MCP in its
  own context → it verifies + screenshots + cleans up → review. Keeps the main chat's context lean. **Extend
  `reactor-build-recipe.md`** with §C's port/pad/bloom/shader steps, then run the build subagent on it.
- **MCP gotchas (verified — do not relearn the hard way):**
  - **Instance IDs churn on every recompile/domain reload** → re-find via `find_gameobjects` (by_component/
    by_name) immediately before each op; never reuse an ID across a reload.
  - **`manage_material` CANNOT toggle `_EMISSION` / transparent keywords** → **edit the `.mat` YAML directly**
    (emission: add `_EMISSION` + `m_LightmapFlags:1`; transparent/additive: set the surface keyword + blend +
    render queue) → `refresh_unity` → it sticks. Proven for both this session.
  - HDR colour: `manage_material set_material_shader_property _BaseColor [r,g,b,a]` (0–1/HDR);
    `set_material_color` is **0–255**.
  - Mesh swap: `manage_prefabs modify_contents component_properties={"MeshFilter":{"m_Mesh":{"path":"<fbx>"}}}`
    auto-resolves the FBX main mesh.
  - **ProBuilder 6.0.9 installed.** Torus: `outerRadius`=major ring, `innerRadius`/`tubeRadius`=tube; flat in
    local XZ (normal +Y) → tilt for orbit. `create_poly_shape` (points + extrudeHeight) for triangle/diamond.
  - **All decor must be collider-free** on the Default layer (no custom layer exists) — a stray collider
    intercepts grab / the UI ray. Audit at the end.
  - **`execute_code` is broken.** Real tinted+bloom look only in a **Playing round** (re-Play past the benign
    first-Play `routine is null`/AABB cascade). Never edit scripts in Play.
- **Assets on hand:** iPoly3D Crystal Pack (crystals), Kenney Particle Pack (`light_*`/`flare_01`/`star_*`/
  `circle_*` sprites for halos/decals), Kenney SSK (consoles/panels for port housings/pads). **Need a dedicated
  pad/socket mesh?** Source CC0 from Quaternius / Poly Pizza (needs web) — record in `asset-ledger.md`.
- **⚠ PERMISSIONS (important):** a build **subagent** gets **denied** any MCP tool not in
  `.claude/settings.local.json` `permissions.allow` (subagents can't answer interactive prompts). This file is
  **gitignored** (the user can't see it — be transparent in chat about every add). For a build pass, ADD:
  `mcp__UnityMCP__find_gameobjects, manage_probuilder, manage_prefabs, manage_scene, manage_asset,
  refresh_unity, run_tests, get_test_job, manage_editor, manage_vfx`, and `ReadMcpResourceTool` (+ `WebSearch`/
  `WebFetch` if sourcing assets). **The baseline to restore afterward is exactly these 5:**
  `mcp__UnityMCP__read_console, manage_material, manage_components, manage_camera, manage_gameobject`.
  Revert to that baseline when done and show the user the file.

### E. Verification gate (non-negotiable — run before claiming done)
`refresh_unity(scope=all,force)` + Console 0 errors · **EditMode `run_tests` = 105/105** (no rule code should
change) · Play boots clean on a re-Play · decor-collider audit (no collider on Default) · `ReactorCoreView._ring`
still assigned · scene-view + in-Play screenshots · **no C#/gameplay/round/scoring/XR-logic change** · propose a
Conventional Commit, **human commits** (read-only git only).

### F. Scope note — TRACK the gap so it can't slip
Port-body reshape + pad reskin are currently a **scope gap**: T19's cut line deferred pads, and T21 is named
"profiling + tuning" (perf), not art. Decide explicitly: **widen T19** to "full reactor art incl. port
bodies + pads" (T19 is the art task — natural home) **or** add a short art-polish task. Either way it **must**
be a tracked task with the GDD-§25 demo bar as its acceptance, or the demo ships with white-sphere ports.

---

## What was actually done

— **T19 not closed (in progress).** As-built state, deviations, materials/prefabs/sprites, and the remaining
work are in **🔻 Session Handoff** above + `../../reactor-build-recipe.md`. Fill this section on final close.
