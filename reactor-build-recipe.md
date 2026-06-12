# Reactor Build Recipe — Starforge Relay (T19 final glow)

> **Working artifact, not a `docs/` doc.** A one-shot, self-contained recipe for an MCP-for-Unity build
> agent to sculpt the reactor's *look* (core + orbiting rings + halo, 3 neon ports with shape markers,
> faceted crystal shards). The executing agent has the **same MCP tools** but **none** of the authoring
> chat's context — so every path, name, number, and verification step is spelled out here.
>
> **Engine:** Unity 6.3 LTS (`6000.3.16f1`), URP (Universal 3D), OpenXR + XRI 3.x, Meta Quest 2/3,
> Android/Vulkan/Single-Pass-Instanced, mobile low-poly, **fake glow over bloom**.
> **Scope owner:** `docs/tasks/T19-final-glow.md` (read it; stay inside its cut line).
> **Do NOT:** change any C# script, gameplay/round/scoring/XR-interaction logic, or run any `git` write.
> **EditMode test suite must stay 105/105.** This is art/material/scene work only.

---

## 0. TL;DR of the chosen design (so you know the target before the steps)

| Element | Decision (exact) |
|---|---|
| **Core** | Keep the existing emissive sphere (`ReactorCore.mat`, the `ReactorCoreView` ramp). **Optional** swap its sphere mesh for a faceted icosphere "gem core". Add **one halo billboard** child (Unlit-HDR quad or ParticleSystem). |
| **Rings** | **3 tori** (electron-orbit look), parented under a new `_ring` spin-node at the core. Even **60° tilt rosette** (planes at 0°, 60°, 120° about local Y, each tipped out of XY). Material `CoreRing.mat` (cyan) + optional palette variants. |
| **Ports (×3)** | Per-port **neon outline ring** (thin torus) + a **shape marker** child: Solar = **circle**, Ion = **triangle**, Pulse = **diamond** (GDD §10/§30). Unlit-HDR, tinted from the palette. **No collider on the Default layer.** |
| **Shards** | Recommend swapping `crystal_2` → a cleaner faceted gem (candidate **`crystal_16`**; confirm by screenshot, fallback `crystal_5`/`crystal_20`). Optional sparkle/halo child. `Shard.mat` already Unlit-HDR. |
| **Glow path** | URP **Unlit + HDR `_BaseColor`** everywhere except the core (URP **Lit + `_EMISSION`**, already set). MCP cannot toggle `_EMISSION`/transparent keywords — see §1 gotchas. |
| **Ring count / tilt / size** | 3 rings; tilts 0/60/120° rosette; ring **major radius ≈ 0.18 m** (≈1.44× the 0.125 m core radius), **tube radius ≈ 0.012 m**. See §4 for exact ProBuilder params. |

---

## 0.5 Validated decisions (LOCKED — these override any "optional / pick one" wording below)

Validated by the project owner; do **not** deviate:

1. **Halo = ParticleSystem billboard** — camera-facing, soft, **Additive blend set on the Particle System *Renderer*** (not the URP surface keyword), bloom-independent. If PS configuration proves unreliable via MCP, **skip the halo and flag it for a human pass** — do **NOT** ship an opaque Unlit quad (black-square corners). Apply the same choice to core (§3.3) and shard (§6.3).
2. **Rings = all cyan** (`CoreRing.mat`). Do **not** add per-ring palette variants now (that's a T21 lever).
3. **Shape markers = ProBuilder geometry** (crisp, palette-tinted Unlit-HDR, collider-free), NOT sprite quads: circle = thin ProBuilder **Pipe** ring; triangle = `create_poly_shape` 3-pt; diamond = `create_poly_shape` 4-pt square-on-point (§5.2).
4. **Gem-core mesh swap = SKIP** (§3.2). Leave the smooth emissive sphere; faceting is a T21 lever.
5. **Docs are NOT in your remit.** Do **not** edit `docs/` or `asset-ledger.md`. The Particle Pack "referenced" ledger flip is handled by the human after the build.

---

## 1. Hard constraints & verified MCP gotchas (read before touching anything)

These are **verified against this exact project** (Unity-MCP `com.coplaydev.unity-mcp@efaf786e8772`,
ProBuilder `com.unity.probuilder@6.0.9`) and the T19 brief. Violating them silently breaks grab/UI or glow.

1. **Instance IDs churn on every recompile / domain reload.** **Always** re-find a target via
   `find_gameobjects` (by_name / by_component) **immediately before** operating on it; never reuse an ID
   across a reload. Core → `find_gameobjects by_component ReactorCoreView`. Ports →
   `by_component PortSocket`.
2. **MCP cannot toggle shader keywords.** `manage_material` **cannot** flip URP `_EMISSION`, nor the
   transparent surface keyword (`_SURFACE_TYPE_TRANSPARENT` / render-queue). `execute_code` is broken.
   - **Emissive Lit** (core): already done in `ReactorCore.mat` (keyword baked into the `.mat` YAML — see
     §3). If you need a *new* emissive-Lit material, write the `.mat` YAML directly (add `_EMISSION` to
     `m_ValidKeywords`, set `m_LightmapFlags: 1`, non-black `_EmissionColor`), then `refresh_unity`.
   - **Glow without a keyword** (everything else): use **URP Unlit + HDR `_BaseColor`** (values > 1). No
     keyword needed; MCP-authorable via `set_material_shader_property _BaseColor [r,g,b,a]`.
   - **Transparent/additive halo:** do **NOT** rely on MCP to set the transparent keyword. Use an
     **Unlit-opaque HDR quad** (preferred on Quest — no overdraw) **or** a **ParticleSystem billboard**
     (set blend mode on the *Particle System Renderer*, not the URP surface). A true additive material is
     a **human 1-click** — flag it, don't fake it.
3. **Colour value scales differ in `manage_material`:**
   - `set_material_color` reads RGB as **0–255**.
   - `set_material_shader_property` with `_BaseColor [r,g,b,a]` reads **0–1 / HDR (>1)**. **Use this for
     all HDR colours.**
4. **`manage_gameobject create` quirks:** it **adds a default collider** to primitives and **ignores**
   `component_properties` / `is_static` on the create call. So after creating: strip the collider
   (`manage_components remove`) and set transform/props with a follow-up call, then re-read to verify.
5. **Decor meshes must NOT carry a collider on the Default layer.** The XRI Near-Far **near-grab** mask
   *and* the world-space **UI ray** both use the **Default** layer. A stray collider on a ring/marker/halo
   will intercept a grab or the UI ray. **This project has NO custom "Decor"/"Ignore" layer** (verified —
   only built-in `Default/TransparentFX/Ignore Raycast/Water/UI` exist). Therefore: **strip the collider
   off every decorative mesh** (rings, markers, halos). Do not try to "move it to a safe layer" — stripping
   is the robust, layer-independent rule the T19 brief mandates.
6. **ProBuilder Torus params — VERIFIED, this differs from generic docs (see §2.3 for the probe to confirm):**
   In this MCP build, `create_shape shape_type:"Torus"` calls `ShapeGenerator.GenerateTorus` with explicit
   radii (it does **not** use a bounding-box `size` for the ring). Param names accepted:
   - `outerRadius` (synonyms `ringRadius`/`outer_radius`/`ring_radius`) = **major ring radius** (centre →
     tube centre). Default `0.5`.
   - `innerRadius` (synonyms `tubeRadius`/`tube_radius`/`inner_radius`) = **tube (minor) radius**. Default
     `0.1`. ⚠ In *this* MCP, `innerRadius` means the **tube**, not a second ring radius — the handler
     deliberately remaps. Treat `innerRadius` and `tubeRadius` as **synonyms for the tube**.
   - `rows` (int, default 8), `columns` (int, default 16) = mesh resolution.
   - `smooth` (bool, default true), `horizontalCircumference`/`verticalCircumference` (default 360).
   - The torus is generated **flat in the local XZ plane** (ring normal = local **+Y**). To make it an
     orbit ring around the core's Z-spin, you must **tilt it** (see §4).
7. **Screenshots are the shape/placement check, not the colour check.** `manage_camera screenshot
   capture_source=scene_view, view_target=<obj>, include_image=true, max_resolution≈512`. The real
   **tinted + bloomed** look only appears in a *Playing* round (`SetColor`/`SetVisualState` run at runtime;
   ports/shards are white until then). For a headless build, trust the material values for colour and use
   Scene-view screenshots for **silhouette, scale, tilt, and placement** only.
8. **After any file/YAML/asset edit:** `refresh_unity(scope=all, mode=force, wait_for_ready=true)`, then
   `read_console(types=["error"])`. The **first Play after a recompile** throws a benign
   `routine is null` / AABB cascade — re-Play to confirm clean. **Never edit scripts while in Play.**
9. **Read transforms/components** via the resource `mcpforunity://scene/gameobject/{id}` (and
   `/components`). Use `manage_components set_property` to assign serialized fields (e.g. `_ring`).

---

## 2. Pre-flight (do these first, in order)

### 2.1 Confirm the editor is healthy
- `read_console(action=get, types=["error"], count="20")` → expect **0 errors**. If there are pre-existing
  errors, stop and report; do not build on a broken project.
- Confirm the open scene is `Assets/_Project/Scenes/StarforgeRelay.unity` (via
  `mcpforunity://scene/active` or `manage_scene get_info`). If not, load it.

### 2.2 Confirm the materials exist and are correct
Run `manage_material get_material_info` on each and verify:

| Material path | Expect |
|---|---|
| `Assets/_Project/Materials/ReactorCore.mat` | shader `Universal Render Pipeline/Lit`; `_EmissionColor` ≈ (0.3, 0.85, 1.3). *(Emission keyword is in the `.mat` YAML — confirm via §3.)* |
| `Assets/_Project/Materials/CoreRing.mat` | shader `Universal Render Pipeline/Unlit`; `_BaseColor` ≈ (0.3, 0.85, 1.3) HDR cyan. |
| `Assets/_Project/Materials/Shard.mat` | shader `Universal Render Pipeline/Unlit`; `_BaseColor` (1,1,1,1). |
| `Assets/_Project/Materials/Port.mat` | shader `Universal Render Pipeline/Unlit`; `_BaseColor` (1,1,1,1). |

These are **already prepared** — do not recreate them. (Shard/Port stay white in the asset; they are tinted
per-instance at runtime via MaterialPropertyBlock.)

### 2.3 Confirm ProBuilder + the Torus params (throwaway probe — REQUIRED)
The recipe's Torus numbers (§6 above lists the source-verified semantics) **must be confirmed live**,
because the param mapping is non-obvious. Do this once:

1. `manage_probuilder action=create_shape, target="__ProbeTorus", properties={"shape_type":"Torus",
   "outerRadius":0.18, "innerRadius":0.012, "rows":8, "columns":20}`.
2. Read it back: `find_gameobjects by_name "__ProbeTorus"` → `mcpforunity://scene/gameobject/{id}`.
   Confirm a torus mesh exists and its **bounding box** is ≈ `0.384 × 0.024 × 0.384` m
   (= 2·(outer+tube) wide and flat in XZ, ≈2·tube tall). This proves `outerRadius`=ring, `innerRadius`=tube,
   and that the ring lies in **XZ** (thin in Y).
3. **DELETE it immediately:** `manage_gameobject action=delete, target="__ProbeTorus"`. Leave no probe.

> If the probe's geometry disagrees with the above (e.g. a different MCP version remaps names), **trust the
> probe** and adjust the §4/§5 torus calls accordingly, keeping the same *physical* radii (major ≈ 0.18 m,
> tube ≈ 0.012 m for core rings; see §5 for port-ring radii).

### 2.4 Re-find the live targets (note: IDs are valid only until the next reload)
- Core: `find_gameobjects by_component "ReactorCoreView"` → expect **1** hit, name **"Reactor Core"**.
- Ports: `find_gameobjects by_component "PortSocket"` → expect **3** hits (one per colour). To learn each
  port's colour, read its `PortSocket` component (`mcpforunity://scene/gameobject/{id}/component/PortSocket`)
  → `_portColor`: **0 = Solar, 1 = Ion, 2 = Pulse** (enum order in `ShardColor`).
- (Optional) Shard prefab lives at `Assets/_Project/Prefabs/Gameplay/Shard.prefab` (edited headless, §6).

**Ground-truth transforms (verified from the scene/prefab files; use as expected values):**
- Root **"Reactor"**: world (0, 0, 0), scale 1, no rotation.
- **"Reactor Core"** (child of Reactor): world position **(0, 1.2, 1.05)**, uniform scale **0.25** → the
  sphere mesh (Unity built-in, radius 0.5) renders at **≈0.125 m radius / 0.25 m diameter**. `_glowRenderer`
  = its own MeshRenderer (material = `ReactorCore.mat`). **`_ring` is currently unassigned (`{fileID: 0}`).**
- Each **Port** instance: layer **Default (0)**, mesh = built-in **Sphere** at scale **0.16** (≈0.16 m),
  material `Port.mat`, a **BoxCollider trigger size 1.5** (the socket volume — leave it), `XRSocketInteractor`,
  `PortView`, `PortSocket`. Ports sit near **y ≈ 1.2, z ≈ 0.85** (closer to the player than the core at
  z 1.05). The **player/origin side is −Z**; markers face **−Z** (toward the player).

---

## 3. CORE

### 3.1 Confirm the emissive ramp is live (no change expected)
The core glow is driven every frame by `ReactorCoreView.ApplyGlow` →
`_EmissionColor = stateColour × intensity` (Dormant 0.15 … Stabilized 1.6). For that to *show*,
`ReactorCore.mat` must have the `_EMISSION` keyword enabled. **Verify** by reading the file
`Assets/_Project/Materials/ReactorCore.mat` and confirming:
```yaml
m_ValidKeywords:
- _EMISSION
m_LightmapFlags: 1
...
- _EmissionColor: {r: 0.3, g: 0.85, b: 1.3, a: 1}
```
This is **already true** in the repo. If a future revert drops it, re-add those three (keyword + lightmap
flag + non-black emission), `refresh_unity`, and re-check. **Do not edit `ReactorCoreView.cs`.**

### 3.2 (Optional) Faceted "gem core" mesh swap
The default core is a smooth sphere. For a more crystalline star, swap its mesh to a **low-subdivision
icosphere** (faceted, reads as a cut gem under emission):

- Create a probe icosphere to harvest a faceted mesh is **not** needed — simpler: leave the sphere. The
  emission ramp + halo already sell the "star". **Recommendation: SKIP the swap for the headless pass**
  (a sphere under bloom reads as a star; faceting is a marginal gain and risks disturbing `_glowRenderer`).
  If you do want it: `manage_probuilder create_shape shape_type:"Sphere", radius:0.5, subdivisions:1`
  parented at the core, move the `ReactorCoreView._glowRenderer` to that mesh's renderer, strip its
  collider, match scale 0.25. **This is a T21 polish lever — only do it if explicitly asked.**

### 3.3 Core halo billboard (the bloom-independent glow anchor — Decision 4)
Add **one** halo as a child of "Reactor Core" so it inherits the core transform.

**Preferred (Quest-friendly, MCP-authorable): Unlit-HDR opaque quad.**
1. Re-find the core (`by_component ReactorCoreView`).
2. `manage_gameobject create primitive_type:"Quad", name:"CoreHalo", parent:"Reactor Core"`.
3. Set its local transform (the core is scale 0.25, so child local units are ×0.25 in world):
   `manage_components set_property` Transform → `localPosition (0,0,0)`, `localScale (3,3,3)`
   (≈0.75 m world quad — a soft glow disc a bit larger than the 0.25 m core),
   `localRotation` so the quad faces **−Z** (Euler `(0,180,0)`) *if* you use a single static quad; or better,
   make the halo a **ParticleSystem** (below) so it always faces the camera.
4. **Strip the collider:** `manage_components remove component_type:"MeshCollider"` (Quad gets a
   MeshCollider on create) — verify via the components resource that **no collider remains**.
5. Create + assign a halo material: `manage_material create` →
   `Assets/_Project/Materials/CoreHalo.mat`, shader `Universal Render Pipeline/Unlit`, then
   `set_material_shader_property _BaseColor [0.6,1.7,2.6,1]` (cyan HDR, ~2× the core hue) and assign the
   Particle-Pack glow sprite as `_BaseMap` (use **`Assets/ThirdParty/Kenney Particle Pack/PNG (Transparent)/light_01.png`**
   or `flare_01.png`). Assign via `assign_material_to_renderer target:"CoreHalo"`.
   - ⚠ **Opaque quad caveat:** without the transparent keyword the quad's black corners will show as a hard
     square. Two acceptable resolutions: **(a)** prefer the ParticleSystem halo (below), which blends via the
     PS renderer; or **(b)** leave the opaque quad and **flag a one-click human step** to switch
     `CoreHalo.mat` Surface Type → Transparent + Blend → Additive. Document which you chose.

**Alternative (best soft glow): ParticleSystem halo billboard.**
- `manage_gameobject create name:"CoreHalo", parent:"Reactor Core"`, add a `ParticleSystem` component.
- Configure (via `manage_components set_property` on the `ParticleSystem` modules, or `manage_vfx` if
  available): `startLifetime` large + looping with `rateOverTime: 0` and a single **emit at start** (or a
  constant 1-particle "always on" via `maxParticles:1`, `rateOverTime:0`, a Burst of 1 looping), billboard
  render mode, `startSize ≈ 0.6` (world), `startColor` cyan HDR (0.6,1.7,2.6). Set the **Particle System
  Renderer** material to an Unlit sprite material using `light_01`/`flare_01`, **blend = Additive on the PS
  renderer** (not the URP surface). Billboard mode means **no camera-facing script** is needed.
- This carries the glow even if **bloom is later disabled** (the T21 bloom-off check).

> **Pick ONE** halo approach and apply it consistently to the core (and reuse it for shards in §7). Note your
> choice and any human-1-click flag in the final report.

---

## 4. RINGS (the orbiting electron-orbit look)

**Target:** 3 thin glowing tori orbiting the core, evenly tilted (60° rosette), so the `ReactorCoreView`
Z-spin reads as orbital motion. **Why tilt:** a torus generated flat in XZ, spun about its own symmetry
axis (local Z after we orient the spin-node), looks static — tilting each torus out of that plane makes the
spin read as orbiting (this is exactly the gotcha called out in `ReactorCoreView`'s `_ring` usage).

### 4.1 Create the `_ring` spin-node (the parent `ReactorCoreView._ring` will rotate about local Z)
1. Re-find the core (`by_component ReactorCoreView`); read its world transform to confirm (0,1.2,1.05).
2. `manage_gameobject create name:"CoreRings", parent:"Reactor Core"`.
   - ⚠ The core is **scale 0.25**. A child's *world* size = child localScale × 0.25. To make the rings'
     **world** major radius ≈ 0.18 m while authoring tori in world-metric ProBuilder units, set the
     **`CoreRings` localScale = (4,4,4)** to cancel the core's 0.25 (4 × 0.25 = 1.0), so 1 ProBuilder unit
     under `CoreRings` = 1 world metre. Then author tori at their true world radii.
   - Set `CoreRings` localPosition `(0,0,0)`, localRotation `(0,0,0)`.
   - `CoreRings` has no renderer/collider — it is a pure pivot. (Created empty GameObjects get no collider.)
3. **Assign it to the view:** re-find the core, then
   `manage_components set_property target:<coreId>, component_type:"ReactorCoreView", property:"_ring",
   value:{"path":"... or instanceID of CoreRings ..."}`. Use the CoreRings instanceID
   (`find_gameobjects by_name "CoreRings"`). Re-read the `ReactorCoreView` component to confirm `_ring` is
   no longer `{fileID: 0}`.
   - The view rotates `_ring` about its **local Z**. `CoreRings` inherits the core's identity rotation, so
     local Z points along world +Z. Tilts below are relative to this node.

### 4.2 Create the 3 tori (exact params)
Author all three as ProBuilder tori, parented under **`CoreRings`**. Because `CoreRings` cancels the core
scale (4 × 0.25 = 1), use **world-metre radii** directly:

- **Major (ring) radius `outerRadius` = 0.18 m** (≈1.44× the 0.125 m core radius — rings sit just outside
  the core surface, clearly orbiting it, not hugging it).
- **Tube radius `innerRadius` (= tubeRadius) = 0.012 m** (thin neon wire; ≈6.7% of the ring radius).
- **`rows` = 8, `columns` = 24** (smooth-enough circle, low poly for Quest: 8×24 ≈ 384 tris/torus, ×3 ≈
  1.1k tris total — fine).
- **`smooth` = true.**

For each torus `i` (0,1,2):
1. `manage_probuilder create_shape, properties={"shape_type":"Torus","outerRadius":0.18,
   "innerRadius":0.012,"rows":8,"columns":24,"smooth":true}`. (It is created at the world origin; you will
   reparent + place it next.)
2. Reparent + place + tilt via `manage_gameobject` / `manage_components set_property` on its Transform:
   - `parent:"CoreRings"`, `localPosition (0,0,0)`, `localScale (1,1,1)`.
   - **Tilt rosette (the orbit planes):** a torus is flat in its local XZ. To make it an orbit ring, rotate
     it up onto an edge and fan the three around. Use these **local Euler rotations**:
     - Ring 0: `(90, 0, 0)` — stands the torus up into the XY plane (faces ±Z).
     - Ring 1: `(90, 60, 0)` — same, rotated 60° about Y.
     - Ring 2: `(90, 120, 0)` — rotated 120° about Y.
   - (Result: three rings tipped vertical and fanned 60° apart — the classic 3-orbit atom rosette. When
     `CoreRings` spins about local Z, all three sweep, reading as orbiting electrons.)
3. **Strip the collider** ProBuilder adds: `manage_components remove component_type:"MeshCollider"` on each
   torus. Verify none remains.
4. **Assign the ring material:** `assign_material_to_renderer target:"<torus name>",
   material_path:"Assets/_Project/Materials/CoreRing.mat"`.
   - **Optional palette variants** (nice but not required): make Ring 1 Ion-cyan and Ring 2 a warmer tint by
     creating `CoreRingWarm.mat`/`CoreRingMagenta.mat` (Unlit-HDR) from the palette (Solar
     `(2.2,1.716,0.44)`, Pulse `(2.2,0.55,1.76)` — see §8 palette table) and assigning per ring. **Default:
     all three use `CoreRing.mat` cyan** to match the core hue — cleanest, do this unless asked otherwise.
5. **Rename** the tori `Ring_0` / `Ring_1` / `Ring_2` for legibility (`manage_gameobject modify new_name`).

### 4.3 Verify the rings
- `manage_camera screenshot capture_source=scene_view, view_target="Reactor Core", include_image=true,
  max_resolution=512` → confirm 3 thin rings forming a rosette *around* (not intersecting/engulfing) the
  core sphere; rings clearly outside the core surface.
- **Smoke (later, in Play):** the rings spin slowly when Dormant and fast when Stabilized (driven by the
  view). No need to verify motion headless.

---

## 5. PORTS (×3 — Solar / Ion / Pulse)

Each port currently is a white sphere (≈0.16 m) with the socket trigger. Add **two** decorative children
**per port instance** (in the scene, NOT the shared prefab — because the shape differs per colour and we are
forbidden from adding selection C#): a **neon outline ring** + a **shape marker**. Both Unlit-HDR, both
**collider-free**, facing the player (**−Z**).

> **Why per-instance and not in `Port.prefab`:** the prefab is shared by all 3 ports, so a marker added to
> the prefab would be the *same* shape on every port. Per-GDD each colour needs a *different* marker, and
> we cannot add a "select the right marker" script (no C# change). So add markers to the **3 found scene
> instances** individually. The neon outline ring *could* go in the prefab (same on all), but to keep all
> port decor in one place and avoid prefab churn, add it per-instance too.

For **each** of the 3 ports (re-find via `by_component PortSocket`, read `_portColor` to know which colour):

### 5.1 Neon outline ring (all ports)
1. `manage_probuilder create_shape, properties={"shape_type":"Torus","outerRadius":0.6,
   "innerRadius":0.06,"rows":6,"columns":24,"smooth":true}`. *(Authored in the port's local space; the port
   is scale 0.16, so localScale 1 → world ring major radius 0.6 × 0.16 = 0.096 m, slightly larger than the
   0.08 m port sphere radius — a tight neon rim.)*
2. `parent:"<port name>"`, `localPosition (0,0,0)`, `localScale (1,1,1)`, **tilt to face the player**:
   localRotation `(90,0,0)` (torus stands up, normal along ±Z; the ring is a flat halo around the port
   facing −Z/+Z). Rename `PortRing`.
3. Strip the `MeshCollider`. Verify none.
4. Assign a per-colour Unlit-HDR material (create once, reuse across same-colour ports — see §5.3 material
   table). E.g. Solar port ring → `PortRingSolar.mat`.

### 5.2 Shape marker (per colour — circle / triangle / diamond)
Sit the marker just in **front** of the port toward the player: localPosition `(0, 0, -0.55)` (×0.16 ≈
−0.088 m world, just proud of the sphere's −Z face). Author flat in XY facing −Z.

- **Solar = circle.** Two options:
  - (a) ProBuilder pipe ring: `create_shape shape_type:"Pipe", radius:0.45, height:0.04, thickness:0.12,
    subdivAxis:24` → a flat ring/annulus; rotate `(90,0,0)` so the disc faces −Z. **Or**
  - (b) a **Quad** with the Particle-Pack **`circle_05.png`** (Transparent) as `_BaseMap` on an Unlit-HDR
    material (cleanest crisp circle). Strip the MeshCollider.
- **Ion = triangle.** Use `create_poly_shape` with a 2D triangle footprint, then a tiny extrude:
  `manage_probuilder create_poly_shape, properties={"points":[[0,0,0.5],[-0.43,0,-0.25],[0.43,0,-0.25]],
  "extrudeHeight":0.04}` → an equilateral triangle prism (~1 m base in local units; ×0.16 ≈ 0.16 m, a touch
  smaller than the port — adjust points ×0.8 if you want it inside the ring). It is created flat in XZ;
  rotate `(90,0,0)` to face −Z. Strip collider.
- **Pulse = diamond.** Same as triangle but a 4-point square rotated 45°:
  `create_poly_shape, properties={"points":[[0,0,0.5],[0.5,0,0],[0,0,-0.5],[-0.5,0,0]],
  "extrudeHeight":0.04}` → a square-on-point (diamond) prism. Rotate `(90,0,0)`. Strip collider.

Rename each marker `Marker` (or `MarkerSolar`/`MarkerIon`/`MarkerPulse`). Assign the matching per-colour
Unlit-HDR material (§5.3).

### 5.3 Per-colour port materials (create once, reuse)
Create these Unlit-HDR materials under `Assets/_Project/Materials/` (use `manage_material create` shader
`Universal Render Pipeline/Unlit`, then `set_material_shader_property _BaseColor [...]`). Values = the
shared palette (×2.2 HDR, see §8) so port decor matches the runtime port tint and shards:

| Material | `_BaseColor [r,g,b,a]` | Used on |
|---|---|---|
| `PortRingSolar.mat` / `MarkerSolar.mat` | `[2.2, 1.716, 0.44, 1]` | Solar port ring + circle marker |
| `PortRingIon.mat` / `MarkerIon.mat` | `[0.44, 1.76, 2.2, 1]` | Ion port ring + triangle marker |
| `PortRingPulse.mat` / `MarkerPulse.mat` | `[2.2, 0.55, 1.76, 1]` | Pulse port ring + diamond marker |

> You may collapse ring+marker into one material per colour (e.g. `GlowSolar.mat`-style) if you prefer
> fewer assets — there is already `Assets/_Project/Materials/Bay/GlowSolar.mat` (Unlit `_BaseColor`
> 4.2,2.6,0.3) etc. as a colour reference, but those are **Bay dressing** assets; prefer new
> `Assets/_Project/Materials/Port*` materials at the palette values above so port decor exactly matches the
> shard/port runtime palette (guardrails §6 single-source).

### 5.4 Verify each port
`manage_camera screenshot capture_source=scene_view, view_target="<port name>", include_image=true,
max_resolution=512` → confirm the outline ring rims the port and the **correct** marker shape sits in front
(circle on Solar, triangle on Ion, diamond on Pulse), no collider on either (check components resource), and
the marker faces −Z (toward where the player stands).

---

## 6. SHARDS

The shard is a faceted crystal (currently `crystal_2`) at scale 0.12, tinted at runtime by `ShardView` via
`Shard.mat` (already Unlit-HDR — confirm in §2.2). The forgiving grab `SphereCollider` (radius 0.65, **not**
trigger) and `XRGrabInteractable` (`throwOnDetach=false`, kinematic) must be preserved.

### 6.1 Pick the best crystal silhouette
Recommendation: a clean, multi-faceted single point reads best as an "energy shard." Candidate
**`crystal_16`** (faceted gem); good fallbacks **`crystal_5`** and **`crystal_20`**. **Confirm by
screenshot** before committing:
1. For each candidate, temporarily swap the prefab mesh (next step) **or** drop the FBX into the scene and
   screenshot. Easiest non-destructive check: instantiate the FBX directly
   (`manage_gameobject create` referencing the FBX) at the core's eye level, screenshot
   `capture_source=scene_view`, then delete. Compare 2–3 silhouettes.
2. Pick the one with a clear single-axis gem silhouette (not too flat, not too busy) at ~0.12 m.

Crystal FBX paths (28 total under `Assets/ThirdParty/iPoly3D Crystal Pack/`; most in `Crystal/`, eight in
hashed subfolders). Examples: `Crystal/crystal_16.fbx`, `Crystal/crystal_5.fbx`, `Crystal/crystal_20.fbx`,
`Crystal/crystal_2.fbx` (current).

### 6.2 Swap the mesh on `Shard.prefab` (confirmed-working path)
Use the **path method** the brief verified:
```
manage_prefabs action=modify_contents, prefab_path="Assets/_Project/Prefabs/Gameplay/Shard.prefab",
  component_properties={"MeshFilter": {"m_Mesh": {"path":"Assets/ThirdParty/iPoly3D Crystal Pack/Crystal/crystal_16.fbx"}}}
```
The `path` auto-resolves the FBX's **main mesh** (confirmed working). Then `refresh_unity(scope=all,
mode=force)` and `read_console(types=["error"])`.

**Verify after swap:**
- Re-read the prefab (`manage_prefabs get_info` or read the `.prefab` file): `MeshFilter.m_Mesh` now points
  at the new FBX guid; **scale still 0.12**; `SphereCollider` radius **0.65, not trigger** (unchanged);
  `XRGrabInteractable.m_ThrowOnDetach = 0` (unchanged); Rigidbody kinematic (unchanged).
- Screenshot a scene instance (or temporarily instantiate the prefab) to confirm the new silhouette.

### 6.3 Optional shard halo/sparkle child
Per Decision 4 the shard halo is a **child of the Shard prefab** (so it inherits `ShardMotion` bob/spin) and
carries **no collider on Default**. Mirror the core halo choice (§3.3):
- Add a child `ShardHalo` to `Shard.prefab` (via `manage_prefabs modify_contents create_child`): either a
  small Unlit-HDR quad (`light_01`/`star_05` sprite) or a 1-particle billboard PS. Keep it **small**
  (world ≈ 0.1 m; the prefab is scale 0.12 so child localScale ≈ 0.8). **No collider.**
- Tint: leave the halo white-HDR or a neutral warm white — it sits *over* the runtime-tinted crystal, so a
  bright neutral halo reads as energy without fighting the per-colour tint. (Do **not** try to drive the
  halo colour from `ShardView` — that would need C#; out of scope.)
- **Pool-reset note (guardrails §12):** the shard is pooled. The existing `OnResetForPool` doesn't clear the
  MPB tint (fine — `SetColor` re-drives it). A *static* halo child needs no per-spawn reset (it's not tinted
  per-event). If you make the halo a PS that you `Play()` on spawn, ensure it stops on release — but the
  simplest static-quad/looping-PS halo needs nothing. Document if you add anything stateful.

> If time-boxed, the halo is optional polish — the mesh swap + glow material is the core deliverable. Note in
> the report whether you added the shard halo.

---

## 7. HALOS (summary of the §3.3 / §6.3 building block)

- **One halo approach, used twice** (core + shard). Preferred: **Unlit-HDR opaque quad** with a
  Particle-Pack glow sprite (`light_01` / `flare_01` / `star_05`) as `_BaseMap` — Quest-cheap, MCP-authorable,
  no transparent keyword. **Caveat:** opaque → visible black square corners unless the sprite is fully
  premultiplied or you flag a **human 1-click** Surface→Transparent/Additive switch. The cleaner soft glow
  is a **ParticleSystem billboard** (blend on the PS renderer). **Pick one, be consistent, report the
  choice + any human-1-click flag.**
- Halos must carry **no collider** (Default-layer rule, §1.5).
- Halos are the **bloom-independent** glow anchor: if bloom is later turned off (T21), the halo still reads
  as glow. Keep them modest in count — no stacked transparent planes across the HMD view (perf).

---

## 8. Palette reference (the single colour source — do NOT diverge)

`ShardColorPalette.Resolve` (static, HDR ×2.2) is the **single** source for shard + port + VFX tints. Your
decor materials must match it so a Solar shard, Solar port, and Solar decor all read identically:

| Colour | Base (0–1) | **HDR `_BaseColor` (×2.2)** to set in materials |
|---|---|---|
| **Solar** (warm yellow) | (1.00, 0.78, 0.20) | **`[2.20, 1.716, 0.44, 1]`** |
| **Ion** (cyan) | (0.20, 0.80, 1.00) | **`[0.44, 1.76, 2.20, 1]`** |
| **Pulse** (magenta) | (1.00, 0.25, 0.80) | **`[2.20, 0.55, 1.76, 1]`** |
| **Core hue** (cyan-ish, set in `ReactorCore.mat` / `CoreRing.mat`) | — | `_EmissionColor` / `_BaseColor` ≈ `[0.3, 0.85, 1.3, 1]` (the view multiplies the core's by intensity at runtime) |

> Bloom is currently **ON** (Global Volume → `StarforgeRelayProfile`: Bloom active, threshold 1, intensity
> 0.25). HDR values > 1 bloom past the threshold. The exact intensities and the bloom on/off stance are a
> **T21** tuning lever — do not chase final brightness here; just match the palette.

---

## 9. VERIFICATION (run all; record results in the report)

1. **Refresh + console clean:** `refresh_unity(scope=all, mode=force, wait_for_ready=true)`, then
   `read_console(types=["error"])` → **0 errors** (warnings tolerated). Investigate any error before
   proceeding.
2. **EditMode tests still 105/105:** `run_tests(mode="EditMode")`, poll `get_test_job` → confirm **105
   passed, 0 failed**. (This task touches no rule code; any regression means something went wrong — stop and
   report.)
3. **Play boots clean:** enter Play (`manage_editor` play), let it initialize, read console. The **first**
   Play after a recompile may show a benign `routine is null` / AABB cascade — **stop Play, re-enter Play**,
   confirm **no errors**, then stop. (Do not edit scripts while in Play.) This also confirms the core glow
   ramp, port tints, and ring spin actually drive (visuals only — no need to play a full round headless).
4. **Scene-view screenshots** (`manage_camera screenshot capture_source=scene_view, include_image=true,
   max_resolution=512`):
   - `view_target="Reactor Core"` → core sphere + 3-ring rosette + halo, rings clearly orbiting (outside the
     core), halo soft.
   - Each port (`view_target="<port name>"`) → neon outline ring + **correct** marker (circle/triangle/
     diamond) facing −Z.
   - A shard (instantiate the prefab or screenshot a scene shard) → faceted crystal + (optional) halo.
5. **Decor-collider audit:** for every new decorative object (rings ×3, port rings ×3, markers ×3, halos),
   read `mcpforunity://scene/gameobject/{id}/components` and confirm **no Collider** component is present.
   (A single stray collider on Default breaks grab/UI — this audit is mandatory.)
6. **`_ring` assigned:** re-find the core, read `ReactorCoreView` → `_ring` ≠ `{fileID: 0}` (points at
   `CoreRings`).

### Final checklist (all must be true)
- [ ] Console 0 errors after final refresh.
- [ ] EditMode **105/105**, no regression.
- [ ] Play boots clean on the re-Play.
- [ ] Core: emissive ramp intact + halo child + (optional) gem mesh.
- [ ] `CoreRings` spin-node created, **assigned to `ReactorCoreView._ring`**, 3 tilted tori under it, all
      collider-free, `CoreRing.mat` (or palette variants).
- [ ] 3 ports each have a neon outline ring + the **correct** shape marker (Solar circle / Ion triangle /
      Pulse diamond), palette-tinted Unlit-HDR, collider-free, facing the player.
- [ ] Shard mesh swapped to the chosen crystal (scale 0.12, grab collider + grab interactable preserved),
      `Shard.mat` Unlit-HDR; optional halo child collider-free.
- [ ] No new colliders on the Default layer anywhere (audit done).
- [ ] No C# / gameplay / round / scoring / XR-logic change; no `git` write.

---

## 10. CLEANUP

- **Delete `__ProbeTorus`** (the §2.3 probe) if it still exists: `find_gameobjects by_name "__ProbeTorus"`
  → `manage_gameobject delete`. Confirm gone.
- Delete any temporary FBX instances you spawned to screenshot crystal candidates (§6.1).
- Leave **no** scaffolding GameObjects, temp materials, or probe meshes. Re-run
  `find_gameobjects by_name "__"`/`Probe`/`Temp` to be sure.
- Save the scene (`manage_scene save`) and the prefab (the headless `modify_contents` saves itself; verify).

---

## 11. NOTES / DEFERRED / KNOWN LIMITATIONS

- **Instance-ID churn:** every recompile/domain reload invalidates IDs. Re-find before every operation
  (§1.1). If a step fails with a missing target after a refresh, re-find and retry.
- **Bloom dependency:** the glow currently *reads* partly because bloom is ON (threshold 1, intensity 0.25).
  The **halo** (not the HDR magnitude) must carry the glow if bloom is later disabled — that's the **T21**
  bloom-off readability check, out of scope here.
- **Decor-collider / layer rule:** there is **no** custom non-interactive layer in this project. The rule is
  **strip the collider** on all decor (rings/markers/halos). Only as a last resort, if some decor *must*
  have a collider, use built-in **"Ignore Raycast" (layer 2)** — but no decor here needs a collider, so
  strip.
- **Real tinted+bloom look is only visible in a Playing round.** Ports/shards are white in the asset; the
  view scripts tint them at runtime. Headless screenshots verify **shape/scale/placement/tilt**, not final
  colour — trust the material HDR values for colour (they match the palette).
- **No gameplay change:** all `*View` / `PortSocket` / `VfxController` C# is already wired (the
  `FlashWrongInsert` path, the `_ring` spin, the emission ramp). This recipe only **assigns the `_ring`
  reference** and **adds decorative meshes/materials**. Do not edit scripts.
- **T21 tuning levers (do NOT chase here):** final HDR intensities, the bloom/tonemapping stance, exact ring
  radii/tilts and marker sizes, faceted gem-core swap, feeder-pad glow, FFR/dynamic-resolution, texture
  sizes, Static flags. Leave these as knobs; "final-ish" (GDD §25) just means it reads well and distinct
  in-editor.
- **Particle Pack ledger:** if you reference Kenney Particle Pack sprites (halos/markers), the T19 brief asks
  to flip their `docs/assets/asset-ledger.md` note to "referenced." That doc edit is part of T19 but is a
  **docs change** — do it only if your remit includes it; otherwise flag it for the human.

---

## Appendix A — Target aesthetic & cited references

**Look:** a glowing star/atom **core** (emissive sphere — have it) ringed by **3 orbiting tori**
(electron-orbit/Bohr look) + a soft **halo**; **3 colour-coded neon ports** with GDD shape markers (Solar
yellow **circle**, Ion cyan **triangle**, Pulse magenta **diamond**); **faceted glowing crystal** shards; a
**dark metal bay** (have it). Low-poly, Quest-friendly, **fake glow** (Unlit/emissive + halo, leaning on the
bloom volume).

**Chosen parameters (rationale):**
- **Ring count = 3** — the canonical stylized-atom / Bohr icon uses three orbits; reads instantly as
  "energy/atom" and stays cheap (~1.1k tris total).
- **Tilt = 60° rosette** (planes at 0°/60°/120° about local Y, each stood vertical) — the standard even
  fan of the 3-orbit atom symbol; guarantees the Z-spin reads as orbiting rather than a static torus.
- **Ring size ratio ≈ 1.44× core radius** (major 0.18 m vs 0.125 m core) with a **thin tube (0.012 m,
  ~6.7% of ring)** — rings sit just outside the core surface, clearly orbiting, neon-wire thin.
- **Colour relationship:** rings + core share the **cyan** core hue (`0.3,0.85,1.3`); ports/shards use the
  three saturated palette hues. Cool core + warm/cool port accents = clear figure/ground on the dark bay.

**References (visual direction):**
- Stylised three-orbit Bohr atom (orbit count + even rosette of tilted ellipses):
  https://commons.wikimedia.org/wiki/File:Stylised_atom_with_three_Bohr_model_orbits_and_stylised_nucleus.svg
- Bohr model (orbit-tilt / shells concept):
  https://en.wikipedia.org/wiki/Bohr_model
- Sci-fi reactor / energy-core look (glowing sphere + luminous orbit rings, neon blue/orange):
  https://www.dreamstime.com/illustration/futuristic-scifi-reactor.html
- Sci-fi crystal/energy core in a dark high-tech chamber (faceted glowing core + halo, dark bay context):
  https://www.blenderkit.com/asset-gallery-detail/9600ddea-e7db-48b4-b9be-d0338769738e/

---

## Appendix B — Verified ground-truth (so you can sanity-check the live project)

| Fact | Verified value |
|---|---|
| Reactor root | world (0,0,0), scale 1, no rotation; 8 children (core + 3 ports + 4 feeder pads). |
| Reactor Core transform | world (0, 1.2, 1.05), uniform scale **0.25** → ≈0.125 m radius sphere. |
| Core renderer/material | `ReactorCoreView._glowRenderer` = own MeshRenderer; material `ReactorCore.mat` (guid `d4b8aafa6f8491649abe7dbb21c6ab2a`). |
| Core `_ring` | **Unassigned** (`{fileID: 0}`) — you assign it (§4.1). |
| `ReactorCore.mat` | URP **Lit**, `_EMISSION` in `m_ValidKeywords`, `m_LightmapFlags:1`, `_EmissionColor (0.3,0.85,1.3)`, MOTIONVECTORS pass disabled. **Ramp is live.** |
| `CoreRing.mat` | URP **Unlit**, `_BaseColor (0.3,0.85,1.3)` HDR cyan. |
| `Shard.mat` / `Port.mat` | URP **Unlit**, `_BaseColor (1,1,1,1)` (tinted per-instance at runtime). |
| Port instance | layer **Default(0)**, built-in **Sphere** mesh @ scale **0.16**, `Port.mat` (guid `cd7abc9b702a269419845922c20fac54`), **BoxCollider trigger size 1.5** (socket — keep), `XRSocketInteractor` (`m_InteractionLayers` all), `PortView`(`_renderer` auto), `PortSocket`(`_portColor` 0/1/2). |
| Shard prefab | `crystal_2` mesh (guid `2fe0c9dd6c56822468eadee638d369dd`) @ scale **0.12**; **SphereCollider radius 0.65, not trigger** (forgiving grab); `Shard.mat` (guid `45f0e7431a9cdbf4a8e56af52d2f3e62`); kinematic Rigidbody; `XRGrabInteractable` `m_ThrowOnDetach 0`, interaction layer bit 1; `ShardView`, `ShardMotion`. |
| Layers available | Built-in only: Default(0), TransparentFX(1), Ignore Raycast(2), Water(4), UI(5). **No custom Decor layer.** |
| ProBuilder | `com.unity.probuilder@6.0.9` installed. Torus → `GenerateTorus(outerRadius=ring, innerRadius/tubeRadius=tube, rows, columns, smooth, hCirc, vCirc)`; flat in XZ (normal +Y). |
| Console at authoring | 0 errors. |
| Particle Pack | `Assets/ThirdParty/Kenney Particle Pack/PNG (Transparent|Black background)/` — `circle_01..05`, `light_01..03`, `flare_01`, `star_01..09`, `magic_01..05`, `spark_*`, `twirl_*`. |
| Crystal Pack | 28 FBX `crystal_1..28` under `Assets/ThirdParty/iPoly3D Crystal Pack/` (`Crystal/` + 8 hashed subfolders). |

*Authoring note: `find_gameobjects` / `manage_probuilder` / `manage_prefabs` were read-only-denied to the
authoring agent, so live transforms above were read from the scene/prefab YAML and the MCP material/console
tools. The **build agent has full MCP** — re-find/verify everything live before operating.*
