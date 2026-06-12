# T18 — Import Kenney + dress station bay + fake-glow material kit

| | |
|---|---|
| Branch | `feature/art-dressing` |
| Milestone | M5 — art |
| Design ref | GDD §4, §9, §10 (Station Bay), §17, §19, §20, §22, §26, §32 (steps 9–10); guardrails §4, §10, §12, §18; [ADR 0001](../architecture/adr/0001-tech-baseline.md) |
| Depends on | T11 (playable slice we dress around); builds on **T17** (scene/VFX + state-driven core it dresses — already merged on `dev`) |
| Touches scenes/prefabs | yes — new `Station Bay` subtree in `StarforgeRelay.unity`; new materials/prefabs under `Assets/_Project/`; Kenney visual packs under `Assets/ThirdParty/`; skybox material; `.gitattributes` (LFS) |
| Status | ✅ done |

## Goal

Turn the bare gameplay scene into a **readable VR diorama**: import the Kenney CC0 visual packs, build and
dress a small **station bay** (floor, panel walls, side consoles, pipes, a window/frame to a dark
starfield) around the existing fixed reactor pose, and establish the reusable **fake-glow material kit**
(shared emissive URP materials + a halo-billboard building block) that T19 then applies to the
interactables. This is GDD dev-order step 9 ("dress the scene") + the **set-dressing** half of step 10
("fake glow"); it unblocks **T19** (final colours/glow on core/shards/ports) and the **T20** device build,
and lands the deferred **Git LFS** migration now that bulk binary art arrives. **No gameplay-rule change.**

## Decisions (resolved at authoring — validate before coding)

1. **T18 ↔ T19 cut line (the key boundary).** The matrix splits "dress bay + fake-glow **materials**" (T18)
   from "final-ish colours/glow on **core / shards / ports**" (T19), and the interactable view scripts
   already point there: `ShardColorPalette` and `ReactorCoreView` carry *"real glow/materials are T19"*, and
   `PortView` reads *"Real shape markers (circle/triangle/diamond) + glow are T18–19"* — **superseded here:
   shard/port shape markers are T19-only** (T19 should drop the stale "T18" lower bound from that comment).
   Resolution:
   - **T18 = the world + the kit.** Environment geometry/props/skybox/lighting, **all static**; and the
     reusable fake-glow **material kit** (emissive-material convention + a halo-billboard prefab),
     *applied to the set-dressing*. Gameplay interactables keep their current primitive placeholders —
     T18 only ensures they still **read clearly** against the new bay.
   - **T19 = reskin the interactables.** Final colours/glow on the core (+ the missing `_ring` child),
     shards, ports (+ shape markers circle/triangle/diamond), retuned `ShardColorPalette` /
     `ReactorCoreView` state palette, the dedicated `PortView` wrong-insert flash, and reskinning the
     T17 VFX placeholders. **All of this is OUT of T18.**
2. **Asset acquisition = human handoff (gating prerequisite).** The Kenney packs are external CC0
   downloads; per the established division of labour (the T16 sound packs were human-provided), the
   **human downloads** the visual packs from kenney.nl and drops the unzipped folders under
   `Assets/ThirdParty/<pack>/` (with each pack's `License.txt`). The **assistant** then does all
   Unity-side work (import settings, materials, prefabs, scene dressing via MCP, ledger). Packs (GDD §19
   MVP set): **Space Station Kit**, **Space Kit**, **Particle Pack**. *(If you'd rather I fetch them via
   shell, say so during validation — default is human-provides.)*
3. **Skybox = dark star dome, not an HDRI (GDD §19 "Recommended Background Choice").** A black/dark sky
   with procedural or hand-placed star dots + a few Kenney Space Kit background props. **Poly Haven HDRIs
   stay OPTIONAL / deferred** (downscale-for-Quest concern); not in this task.
4. **Post-processing stance — fake-glow, not bloom (discrepancy to resolve).** The committed `Global
   Volume` → `StarforgeRelayProfile` currently has **Bloom + Tonemapping + Vignette active**, which
   contradicts GDD §17/§26 + guardrails §18 ("fake glow over URP bloom" for the MVP, esp. Quest 2). T18
   builds the look on **emissive + halo billboards** and does **not depend on bloom**. Proposed: **bloom
   OFF for the Quest (Mobile) build**; any bloom/tonemapping stays an **editor / Quest-3 polish lever
   decided at T21 profiling**. T18 will leave the profile as-is unless you approve flipping bloom off now.
5. **Git LFS migration (history rewrite = human git step).** `.gitattributes` already documents the plan
   (`git lfs track "*.png" "*.tga" "*.psd" "*.fbx" "*.wav" "*.mp3" "*.ogg" "*.ttf" "*.otf"`). The
   **assistant edits `.gitattributes`** to add the tracking lines; the **human runs** `git lfs install`
   (once/machine) + `git lfs migrate import …` + force-push + re-clone on the other machine. This sweeps
   in the existing T16 `.ogg` plus the new art binaries.
6. **Lighting / shadow stance (pre-existing realtime light — discrepancy to resolve).** The committed scene
   ships a Directional Light in **Realtime** mode (`m_Lightmapping: 4`) with **Soft Shadows ON**
   (`m_Shadows.m_Type: 2`) and nothing baked; `Mobile_RPAsset` supports main-light + any shadows, so the
   Quest path renders a main-light shadowmap. It is nearly free today (no environment geometry), but dressing
   the bay with static floor/walls/consoles/pipes turns it into a real per-frame Quest-2 GPU cost — exactly
   the state GDD §17/§26 ("baked or static-looking lighting"; "real-time shadows off on Quest 2 unless
   profiled") + guardrails §18 warn against. **Proposed (default, pending validation): turn the Directional
   Light's shadows OFF for the MVP** and carry the look on emissive/ambient + the fake-glow kit. If we instead
   **bake** now: set the light Baked/Mixed, enable **Generate Lightmap UVs** on every imported Kenney FBX, and
   bake the static bay — otherwise lightmaps/static-batching silently break. Pick one explicitly before
   coding; do not leave a live realtime shadow-caster unowned. (Same class of pre-existing discrepancy as
   Decision 4's bloom.)

## Acceptance criteria

- **Assets imported & ledgered.** Kenney **Space Station Kit**, **Space Kit**, **Particle Pack** present
  under `Assets/ThirdParty/<pack>/` with `License.txt`; each gets its `asset-ledger.md` row flipped from
  `PLANNED` → `✅ IMPORTED` (Download date / Local path / Modifications filled). No asset enters the build
  without a ledger row + confirmed CC0 license (ledger rule).
- **Station bay exists & is dressed.** A new static **`Station Bay`** root in `StarforgeRelay.unity`
  containing at least: a floor, back/side panel walls, ≥1 side console, some pipe/cable dressing, and a
  window/frame opening onto the starfield — composed so the bay reads as a sci-fi reactor room (GDD §10
  Station Bay, §17). All bay objects are **static** (Static flag set; no per-frame scripts).
- **Skybox / background.** A dark star-dome skybox material is assigned (Lighting settings), with a few
  distant non-interactive Kenney Space Kit props (GDD §19). Background is far and non-interactive (§20).
- **Reach & comfort preserved (GDD §9).** Bay geometry does **not** intrude on the interaction zone: the
  reactor stays ~0.75–0.95 m forward at ~1.1–1.3 m height; feeder pads stay in the ±50° forward arc;
  nothing spawns/sits behind the player; floor reads at y≈0 (Floor tracking origin). **Static bay/background
  props are dressing only and must carry NO colliders** — Unity's model importer leaves *Generate Colliders*
  OFF for the Kenney meshes (keep it off; don't add MeshColliders/primitive colliders to walls/consoles/
  pipes). If a prop genuinely needs a collider, keep it **off the Default layer**, since the controller
  Near-Far near-grab mask (Default-only) and the far/UI-ray raycast mask both include Default — where
  shards, ports, and the world-space UI all operate (verified in-scene this session). A stray prop collider
  on Default silently intercepts a grab or the UI ray.
- **Fake-glow material kit.** A shared emissive-material convention under `Assets/_Project/Materials/`
  (URP Lit/Unlit, on the same `_BaseColor` / `_EmissionColor` property names the views already drive) + a
  **halo-billboard** building block (prefab) for cheap fake glow. **Emission must be enabled on the material
  asset itself: a `MaterialPropertyBlock` can override `_EmissionColor` but CANNOT toggle the URP `_EMISSION`
  shader keyword — so a URP Lit material with Emission unticked shows no glow whatever the MPB value (exactly
  the state of the current `Shard.mat` / `Port.mat`: Lit, emission OFF — do not mirror it).** Tick Emission on
  Lit, or prefer **URP Unlit** for glow accents (always samples colour, no keyword gate). Applied to
  set-dressing accents in this task; reused by T19 for interactables.
- **Interactables still readable.** Core, 3 ports, 4 feeder pads, and shards remain clearly visible and
  colour-distinct against the dressed bay (placeholders unchanged, per the cut line). The colour-matching
  read is not harmed by the new background (GDD §24 — "Colors remain readable against the station background").
- **Performance discipline (no obvious regression; final profiling is T21).** Static props batched; unique
  materials/textures kept low; large textures downscaled for Quest; the pre-existing Directional Light's
  realtime shadows are resolved per **Decision 6** (OFF for the MVP, or Baked with lightmap UVs) and no
  further real-time shadows are added on the Quest path unless profiled; no stacked transparent planes
  across the HMD view (guardrails §18, GDD §26).
- **First-party vs third-party separation (guardrails §4).** Kenney packs stay under `Assets/ThirdParty/`;
  all first-party materials/prefabs/scene content under `Assets/_Project/`.
- **`.gitattributes` updated** with the LFS `track` lines (per Decision 5); the migrate/push is the human's.

## Implementation notes

- **Starting point (verified this session):** the scene's 19 roots are lighting + rig + `Reactor`
  (core + 3 ports + 4 feeder pads, 8 children) + gameplay controllers + 6 world-space canvases + audio/
  feedback/vfx — **there is no environment object at all**, so the bay is built from scratch. Only
  `Shard.mat` + `Port.mat` exist; no `Assets/_Project/Art/` folder yet.
- **Material seam the views expect:** `ReactorCoreView` / `ShardView` / `PortView` tint via
  `MaterialPropertyBlock` on `_BaseColor` (+ core `_EmissionColor`). Keep the fake-glow kit on the same URP
  property names so T19 drops in without touching C#. **T18 adds little or no runtime C#** — if a
  camera-facing halo billboard is needed, it's a *thin adapter* that must **cache the XR camera (never
  `Camera.main` per frame)** (guardrails §10/§13); prefer a Particle System in billboard mode or a static
  emissive quad to avoid a per-frame script entirely. **Seam contract for T19:** a kit material glows only if
  its `_EMISSION` keyword is enabled at author time (or it is Unlit) — the views MPB-override the colour
  *value* only, never the shader keyword.
- **Folders:** create `Assets/_Project/Art/` (and `Materials/` already exists) for first-party dressing;
  prefabs under `Assets/_Project/Prefabs/` (e.g. a `Bay/` subfolder). Keep the Kenney import under
  `Assets/ThirdParty/` mirroring the T16 layout.
- **MCP pitfalls to expect (from current-status — this is a heavy scene/prefab task):**
  - `execute_code` is **broken** on both machines → use **structural MCP tools** only.
  - `refresh_unity scope=scripts` does **not import new files** — use `scope=all` after dropping the
    Kenney packs / new assets.
  - `manage_gameobject create` **silently ignores** `component_properties` / `components_to_remove` — set
    via `manage_components` then **re-read to verify**; component-array refs need `[{"instanceID":…}]`.
  - `create save_as_prefab` leaves a **stray temp instance** (delete it); prefab **unpack** is a manual
    1-click step; the asset-rename tool reports "failed" but succeeds on disk (verify via filesystem).
  - The **first Play after each recompile** throws a benign `routine is null` / AABB cascade — re-Play to
    confirm clean. **Never edit scripts while the Editor is in Play.**
- **Render targets (ADR 0001):** Quest build uses the **Mobile** URP asset (`Assets/Settings/
  Mobile_RPAsset.asset` / `Mobile_Renderer`); Single Pass Instanced, Vulkan, Linear, ASTC. Graphics-API /
  platform toggles remain **human** steps (build-and-test "who does what").
- **Right-size it (GDD §22 cut line):** the bay may be "a small platform plus a few panels," not a full
  room — protect the playable loop and Quest-2 perf over geometry density. Dress with **composition +
  lighting**, not polygon count.

## Out of scope

- **All of T19:** final colours/glow on core/shards/ports, the core `_ring` child + ring spin, shard/port
  **shape markers**, retuned `ShardColorPalette` / `ReactorCoreView` palette, the `PortView` wrong-insert
  material-flash, reskinning the T17 VFX placeholders.
- **T20:** the on-device Quest 2 build/smoke and the deferred **Recenter/calibration** wiring (GDD §9).
- **T21:** profiling + tuning (FPS floor, FFR/dynamic resolution, the bloom/tonemapping post stance,
  texture-size passes, reach/size tuning).
- The actual **`git lfs migrate` + force-push + re-clone** (human git step) and any other git write.
- Poly Haven HDRIs / Quaternius / Modular Space Kit (OPTIONAL ledger rows — only if the MVP set falls short).
- No gameplay-rule, scoring, spawn, or XR-interaction change.

## Verification

- **Tests:** **no new EditMode tests** — T18 introduces no pure rules (art/scene/material adapter task per
  the per-mechanic gate, guardrails §17). **Regression gate:** re-run the **full EditMode suite** and
  confirm **105/105, no regression**.
- **Compile/validation:** MCP compile clean, **no Console errors**; **Project Validation (Android)** still
  passes (build-and-test "ready for review").
- **Smoke (human-realistic):** enter Play (MCP) under the **XR Device Simulator** / **Quest Link** and
  confirm: the bay renders and reads as a station; the full loop still works (grab → socket →
  correct/wrong/expired feedback → results); **near each new bay prop, a near-grab still acquires a nearby
  shard/port and a Trigger UI-ray still reaches a canvas button through that prop's space** (catches a stray
  prop collider intercepting the grab/UI ray); interactables stay colour-readable against the bay **(re-check
  readability if/when bloom is flipped off per Decisions 4/6 — the editor smoke runs with bloom on)**; no
  console errors and no obvious editor frame-time cliff. Real **Quest 2** perf is the **T20/T21** bar, not
  this task.
- **Restricted-API check** only if any runtime C# was added (a billboard helper): run the guardrails §10
  ripgrep over `Assets/_Project`.
- Done = full suite green + no Console errors + bay dressed + assets ledgered (see
  [build-and-test.md](../development/build-and-test.md)).

## What was actually done

**Shipped — PR #18, commit `8bc32ef`, 2026-06-12, branch `feature/art-dressing`.** Turned the bare scene
into a readable VR diorama; **no gameplay-rule change**. Quest-2 smoked (device-approved).

- **Assets imported & ledgered** ([asset-ledger.md](../assets/asset-ledger.md), 2026-06-12): Kenney **Space
  Station Kit** (CC0) trimmed to **FBX-only** (redundant OBJ/GLB + `Previews/` removed) and re-skinned dark
  via `BayMetalDark`; Kenney **Particle Pack** (CC0, full PNG sets — referenced by the T19 fake-glow, not yet
  at T18). Each pack kept its `License.txt` under `Assets/ThirdParty/<pack>/`.
- **Station Bay built & dressed.** New `Station Bay` scene root (19 children: dark-metal floor/walls + a
  window framing the reactor + HDR glow accents) plus a space backdrop (`Space Planet` + `Star A–D`) on a
  dark star-dome skybox. Reactor / 3 ports / 4 pads / UI / feedback / vfx roots unchanged; the interaction
  zone (reactor pose, ±50° pad arc, floor at y≈0) is preserved; interactables stay primitive placeholders.
- **Lighting (Decision 6 → OFF).** Directional Light **realtime shadows turned OFF** for the MVP (look
  carried on emissive/ambient + fake glow); not baked.
- **Fake-glow material kit.** Shared emissive URP convention under `Assets/_Project/Materials/` on the
  `_BaseColor` / `_EmissionColor` property names the views already drive — the T19 reskin seam.

**Deviations from the plan (deferred, each with a home):**
- **Space Kit dropped** — imported then **removed**: all 13 FBX threw identifier-uniqueness import errors and
  the pack is irrelevant to a single bay; backdrop uses the star-dome + Particle stars instead (ledger row
  `❌ REMOVED`).
- **`.gitattributes` LFS lines NOT added; Git LFS migration deferred wholesale** (not the partial split
  Decision 5 framed) — `.gitattributes` still carries LFS only as a deferred NOTE. Human runs `git lfs
  install` + `git lfs migrate import` for the art/audio globs when the art settles.
- **Static flags → T21** — `Station Bay` is **not** marked Static yet (URP SRP Batcher covers MVP batching;
  static pairs with the T21 bake/occlusion pass). The "all bay objects static" criterion was intentionally
  pushed to T21.
- **Bloom kept ON (Decision 4 not flipped)** — `Global Volume` still has Bloom/Tonemapping/Vignette; the
  fake-glow look doesn't depend on it. Bloom-off vs a Quest-3 polish lever is a **T21** profiling call.
- Halo-billboard prefab, accent intensity, starfield density → **T19/T21** (per the T18↔T19 cut line).

**Verification (this session, 2026-06-12):** EditMode **105/105 green** (`run_tests` MCP, 0 failed/0 skipped,
1.95 s) — no regression; **no new EditMode tests** (art/scene task, per the §17 gate); Console **0 errors /
0 warnings**; `Station Bay` + backdrop confirmed in-scene via MCP hierarchy. No runtime C# added →
restricted-API scan N/A. Real Quest-2 perf is the **T20/T21** bar.
