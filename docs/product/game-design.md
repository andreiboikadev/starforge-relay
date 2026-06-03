# VR Game Concept GDD: Starforge Relay

> **Purpose of this document.** This is a design brief for a small standalone VR game for Meta Quest 2 and Meta Quest 3. It describes the desired gameplay experience, loop, menus, content, assets, constraints, and acceptance criteria. It deliberately does not lock down exact Unity versions, XR package versions, or low-level architecture. The next technical chat should use this document as a clear spec and choose the simplest reliable implementation path.
>
> **Technical intent.** If the project is built in Unity, the baseline path is: Unity URP, OpenXR or the Meta XR provider, XR Interaction Toolkit, Quest controllers, a simple state machine, object pooling, explicit references to scene objects, minimal runtime lookups, no complex physics, and no locomotion in the MVP.

---

## Implementation Defaults for Claude Code

These defaults are part of the brief. If a future implementation chat needs to choose, choose this path unless the local project already has a working equivalent:

- Use Unity with URP and Android/Quest build target.
- Prefer OpenXR with Meta Quest support/features for a fresh project. Do not enable multiple XR providers for the Android build target at the same time.
- Use XR Interaction Toolkit Starter Assets or an equivalent existing XR Origin setup.
- Use controller input only for MVP.
- Use Grip for grabbing shards.
- Use Trigger for world-space UI selection.
- Do not implement hand tracking, locomotion, passthrough, networking, or physics throwing in MVP.
- Use fake glow with emissive materials, halo billboards, short beams, and small particle bursts. Do not depend on URP bloom or heavy post-processing for the MVP look.
- Build the primitive playable loop before importing art.

---

## 1. High-Level Concept

**Working title:** Starforge Relay

**Naming note:** Starforge Relay is a working title, not the final brand. Before publishing to the Meta Store, SideQuest, itch.io, or App Lab, the name should be separately checked against store listings, search results, and trademarks. Renaming should be cheap: the design does not depend on any specific name.

**One-sentence pitch:** The player stands at a space reactor, grabs glowing energy shards with their hands, and inserts them into matching colored ports to stabilize a mini-star before the station overheats.

**Genre:** Stationary VR arcade / tactile sorting game.

**Target experience:** A short, spectacular VR demo for a portfolio: the player immediately sees a beautiful core, glowing shards, reactor rings, particles, beams, and clear hands-on interaction.

**Estimated build target:** 1 focused day for a functional prototype built from primitives, if the Unity XR template, Quest build, and deployment already work. 2 days is more realistic for a showable demo with assets, sound, VFX, menus, and on-device testing. This does not include unforeseen setup of the Android SDK, drivers, Meta Quest Developer Hub, developer mode, or a first-ever Quest deployment.

**Primary platform assumption:** Standalone Meta Quest 2 and Meta Quest 3. Quest 2 is the minimum performance bar. Quest 3 may have slightly more particles or a higher render scale, but the mechanics must be identical.

**Input assumption:** Two Touch controllers. Hand tracking is not part of the MVP.

**Session length:** About 70 to 90 seconds per round. The round timer is 90 seconds and can end sooner on victory or overload.

**Core fantasy:** "I am repairing a glowing star reactor with my hands."

---

## 2. Why This Is a Good First VR Game

Starforge Relay is built around one small interactive scene in front of the player. The player does not walk around a level, does not teleport, does not shoot at distant targets, does not control a character, and does not solve complex physics puzzles.

VR-specific fun comes from:

- Grabbing objects with your hands.
- Quickly sorting colored shards.
- Physically moving your hands around a bright reactor.
- Instant tactile feedback: sound, haptics, a flash, an energy beam.
- A beautiful space station diorama around the player.

The simple technical shape is:

- One stationary play area.
- One central reactor object.
- Three colored sockets.
- 4 active grabbable shards by default; up to 6 only if extra feeder slots are added.
- Direct grab and trigger-volume validation.
- No locomotion.
- No enemy AI.
- No pathfinding.
- No networking.
- No procedural level generation.
- No throwing requirement.

---

## 3. Design Pillars

1. **Hands-first**
   - The main joy is grabbing, carrying, and inserting shards with your hands.
   - All important objects must be within comfortable reach.

2. **Readable VR spectacle**
   - Color, shape, and position must be readable instantly.
   - The reactor should look more expensive than it actually is under the hood: rings, light, beams, particles, sound.

3. **Quest 2 baseline**
   - Any graphics must work on Quest 2 first.
   - Quest 3 polish must not break the Quest 2 build.

4. **No motion sickness**
   - No smooth locomotion.
   - No artificial camera movement.
   - No required sharp turns.
   - All targets appear in the player's front zone.

5. **Asset-realistic**
   - The visuals are built on free low-poly sci-fi/space assets, emissive materials, and simple VFX.
   - The MVP must be playable even with primitives if asset import is delayed.

---

## 4. Player-Facing Summary

The player appears in a small bay of a space station. An unstable star core hovers in front of them. On a nearby reactor panel there are three large colored ports: yellow, cyan, and pink-magenta. Energy shards of the same colors appear to the left, to the right, and in front.

The player grabs a shard with a controller and releases it inside a port of the matching color. A correct shard turns into an energy beam, the core becomes more stable, and score and combo increase. A wrong shard sparks, flies back out, and adds heat. If the core is fully stabilized before the timer runs out, the player gets a victory burst of light and a results screen.

---

## 5. Core Game Loop

1. Start round.
2. Reactor core wakes up and shows three colored ports.
3. Energy shards spawn on feeder pads in front of the player.
4. Player grabs a shard with a controller.
5. Player moves the shard into a matching colored port.
6. Correct release:
   - Shard is accepted.
   - Beam fires into the core.
   - Reactor charge increases.
   - Score and combo increase.
   - Controller haptics pulse.
7. Wrong release:
   - Shard is rejected.
   - Small sparks play.
   - Combo resets.
   - Heat meter increases.
8. Expired shard:
   - If ignored too long, it fizzles out.
   - Heat meter increases slightly.
   - Combo resets only if that shard was being held when it expired.
9. When enough shards are accepted:
   - Reactor stabilizes.
   - Victory sequence plays.
   - Results screen appears.
10. If timer reaches zero or heat reaches max:
   - Round ends with partial stabilization or overload result.
   - Results screen offers replay.

---

## 6. MVP Feature Set

The MVP should include only these features:

- App launch into a simple main menu.
- One playable mode: **Stabilize Run**.
- Stationary VR play space.
- Controller-based direct grab.
- Three colored reactor ports.
- Three shard colors.
- 90-second timer.
- 20 correct shards required for full stabilization.
- 4 active shards by default; 5 to 6 only if extra feeder slots are added and readability remains good.
- Correct/wrong/expired feedback.
- Score, combo, reactor charge meter, heat meter.
- Victory, time-out, and overload result states.
- Basic settings: sound on/off and haptics on/off.
- Credits screen with asset sources and licenses.
- Quest 2 performance profile as default.

---

## 7. Explicit Non-Goals

Do not include these in the first version:

- Smooth locomotion.
- Teleport locomotion.
- Room-scale exploration.
- Large station level.
- Multiplayer.
- Account login.
- Cloud saves.
- Online leaderboards.
- Procedural missions.
- Enemy waves with AI.
- Guns or projectile combat.
- Complex throwing challenges.
- Physics puzzles with chains, joints, fluids, ropes, or destructibles.
- Hand tracking as a required input.
- Full body avatar.
- Inventory.
- Crafting.
- Story campaign.
- Ads or monetization.
- In-app purchases.
- More than one gameplay mode in MVP.

These can be considered after a stable device build exists, but they should not influence the MVP architecture.

---

## 8. Target Devices and Performance Target

**Primary target devices:**

- Meta Quest 2 standalone.
- Meta Quest 3 standalone.

**Baseline device:** Quest 2.

**Performance target:**

- Minimum acceptable target: stable 72 FPS on Quest 2.
- Preferred Quest 2 target: stable 72 FPS with headroom.
- Optional Quest 3 target: 90 FPS if it is easy after profiling.

**Important implication:** The game must look good through art direction, not heavy rendering. Avoid relying on expensive bloom, real-time shadows, dense transparent particles, high-poly meshes, or large animated environments.

**Quest 3 enhancement policy:**

- Quest 3 may enable slightly denser particles.
- Quest 3 may enable stronger fake glow or higher render scale if profiling says it is safe.
- Quest 3 may render at a slightly higher resolution.
- Quest 3 must not get exclusive mechanics, extra required objects, or different timing.

---

## 9. VR Play Space

**Player posture:** Standing stationary is the MVP target. Seated play is optional polish only if calibration/recenter support makes it comfortable without extra scope.

**Required physical space:** Stationary boundary. Recommended clear space about 1.5 m x 1.5 m.

**In-game interaction zone:**

- Reactor center should be about 0.75 to 0.95 m in front of the player.
- Reactor center height should be around chest height, roughly 1.1 to 1.3 m for standing mode.
- Feeder pads should stay within a comfortable forward arc, roughly -50 to +50 degrees from player forward.
- No required shard should spawn behind the player.
- No required interaction should be below waist height or above upper-chest height in MVP.

**Comfort constraints:**

- Keep all gameplay in front of the player.
- No artificial camera movement.
- No forced turning.
- No screen shake.
- No full-screen flash.
- No fast objects flying into the player's face.
- If tracking loss handling is cheap, pause the timer and show a calm overlay. Do not block the MVP on custom tracking-loss UI; the game should simply avoid punishing brief controller loss.

**Recenter requirement:**

- The main menu or calibration screen should include a **Recenter** button if it is easy.
- If not implemented, rely on the Quest system recenter action and keep the play area forgiving.

---

## 10. Main Objects

### Star Core

The Star Core is the central objective and main visual anchor.

**States:**

- Dormant: dim sphere, slow idle rotation.
- Active: three colored ports sit on a front-facing reactor panel around or below it.
- Charging: accepted shard creates a beam into the core.
- Heating: core pulses red/orange when heat rises.
- Almost Stable: stronger glow, more rings lit.
- Stabilized: bright controlled star, ring spin, upward particle burst.
- Overloaded: red sparks, smoke puff, lights dim briefly.

**MVP visual implementation:** A sphere with emissive material, two or three torus/ring meshes, fake glow billboards, and short particle bursts. It can be built from primitives if no model is ready.

### Reactor Ports

The three ports are the color-matching targets.

**Colors:**

- Solar: warm yellow.
- Ion: cyan/blue.
- Pulse: magenta/pink.

**Behavior:**

- Each port has a large trigger volume.
- Each port accepts only matching shards.
- Each port has a clear icon/shape as backup for color readability.
- Correct release pulls the shard into the port center and then into the core.
- Wrong release rejects the shard with a soft push-back animation.
- A release only counts as wrong if the shard is clearly inside a wrong port trigger. Dropping outside any port has no penalty.

**Shape markers:**

- Solar port: circle/sun ring.
- Ion port: triangle.
- Pulse port: diamond.

Shape markers should be present from the start if quick to implement. If not, keep them as the first accessibility polish task.

### Energy Shards

Energy shards are grabbable objects.

**Required properties:**

- Color.
- Shape marker or silhouette.
- Lifetime timer.
- Grab state.
- Accepted/rejected/expired state.

**Behavior:**

- Spawn on feeder pads.
- Float slightly above the pad.
- Bob gently.
- Rotate slowly.
- Can be grabbed with either hand.
- While grabbed, lifetime slows strongly but does not fully pause.
- Use kinematic/simple movement. Do not rely on ballistic throwing physics in MVP.
- If released in empty space, the shard lerps back to its original feeder pad after a short delay.
- If ignored until lifetime ends, it fizzles and increases heat.

**Visibility rules:**

- Shard mesh should be simple and readable.
- Shard must have a fake glow halo or small particle sparkle.
- Collider should be larger than the visible mesh for forgiving grabs.
- Shards should be at least 10 to 14 cm wide in world scale.

### Feeder Pads

Feeder pads are where shards appear.

**MVP layout:**

- Front-left pad.
- Mid-left pad.
- Mid-right pad.
- Front-right pad.
- Optional upper-left and upper-right pads only if reach remains comfortable and 5 to 6 active shards are enabled.

**Behavior:**

- Pad lights up before spawning a shard.
- Spawned shard floats above pad.
- Pad color can match the current shard for readability.

### Station Bay

The station bay sells the setting.

**Props:**

- Modular sci-fi floor/wall panels.
- Small side consoles.
- Cables or low-poly pipes.
- Window or open frame to starfield.
- Optional distant planets/asteroids/ships as non-interactive background.

**Performance rule:** The station bay should look rich from composition and lighting, not from high geometry density.

---

## 11. Controls

### Required Controls

- **Grip on shard:** Grab.
- **Release grip:** Drop or insert shard.
- **Point + trigger on UI:** Press menu buttons.
- **Menu button:** Pause if easy to bind reliably.

### Optional Controls

- **A/X button:** Quick pause.
- **B/Y button:** Recenter or return shard if held.
- **Two-hand hold on core after victory:** Trigger one extra sparkle moment.

Optional controls should only be added if the MVP loop is already stable.

### Input Notes

- The MVP should use controllers, not hand tracking.
- Both hands should be equivalent.
- Grabbing should be forgiving; exact finger poses are not required.
- Do not implement throwing for this version. Reliable VR throwing takes tuning and does not help the core portfolio demo.

---

## 12. Round Rules

**Mode name:** Stabilize Run

**Round duration:** 90 seconds.

**Stabilization requirement:** 20 correct shards.

**Heat cap:** 8 heat.

**Shard colors:** Solar, Ion, Pulse.

**Active shard count:**

- Start with 4 active shards, one per MVP feeder pad.
- Increase to 5 active shards after 8 correct accepts only if a fifth feeder slot exists.
- Increase to 6 active shards after 15 correct accepts only if readability remains good and six feeder slots exist.

**Spawn rules:**

- Spawn shards on feeder pads in the front arc.
- Use predefined pad slots rather than pure random positions.
- Avoid spawning more than 3 shards of the same color at once.
- Always keep at least 2 different colors active.
- Prefer spawning a color that currently has fewer active shards.
- Each feeder pad holds at most one shard at a time; the pad stays reserved while its shard is idle, grabbed, or returning, so no second shard spawns there.
- When a shard is accepted or expires, free its pad and spawn a replacement on a free pad after about 0.3 to 0.8 seconds, keeping the active shard count at its current target (4 by default) and honoring the color rules above.
- Shard lifetime starts at 14 seconds.
- After 10 correct accepts, shard lifetime may drop to 12 seconds.
- Never spawn required interactions outside comfortable reach.

**Correct insert:**

- +10 score.
- +1 stabilization progress.
- +1 combo.
- Correct shard animates into port, then into core.
- Short controller haptic pulse.
- At every 5-combo milestone, award +50 bonus points and reduce heat by 1 if heat is above 0.

**Wrong insert:**

- +1 heat.
- Combo resets to 0.
- Wrong shard sparks and is rejected.
- No harsh sound.
- The shard remains playable unless heat reaches max.
- After rejection, the shard is pushed out of the port and returns to its original feeder pad (the same behavior as an empty-space drop), so it can be retried.

**Expired shard:**

- +1 heat.
- Combo resets to 0 only if the shard was currently grabbed.
- Shard fizzles and respawns later.

**Dropped shard in empty space:**

- No score penalty.
- Shard lerps back to its original feeder pad after about 0.5 to 1.0 second if it is not grabbed again.
- If it stays unhandled until lifetime ends, it expires normally.

**Victory:**

- Triggered when stabilization progress reaches 20.
- Stop timer and shard spawning.
- Play stabilization animation.
- Show results after about 2 seconds.

**Time-out:**

- Triggered when timer reaches 0.
- Stop spawning.
- Core shows partial stabilization based on progress.
- Show results after about 1 second.

**Overload:**

- Triggered when heat reaches 8.
- Stop timer and spawning.
- Core vents sparks/smoke.
- Show results after about 1.5 seconds.

---

## 13. Scoring

**Base score:**

- Correct shard: 10 points.
- Combo bonus at every 5-combo milestone: 50 points.
- Victory time bonus: remaining seconds x 2.
- Heat penalty on results: heat x 10 subtracted from final score, but never below 0.

**Star rating:**

- 0 stars: 0 to 5 correct shards or overload before 6.
- 1 star: 6 to 11 correct shards.
- 2 stars: 12 to 19 correct shards.
- 3 stars: reactor fully stabilized.

**Best score:**

- Store locally if simple.
- If local storage is not ready on day one, omit best score rather than adding persistence complexity.

---

## 14. Difficulty and Balance

The first version should have one difficulty only.

Initial tuning targets:

- Average first-time player should understand the action within 15 seconds.
- Average first-time player should accept at least 5 correct shards in the first round.
- A focused player should stabilize the core in 70 to 90 seconds.
- Wrong inserts should feel like a mistake, not like a punishment.
- Heat should create tension but not end most first rounds too early.

If the game feels too easy:

- Reduce starting shard lifetime from 14 to 12 seconds.
- Increase active shards to 5 earlier only if a fifth feeder slot exists.
- Raise stabilization requirement from 20 to 24.

If the game feels too hard:

- Increase shard lifetime to 16 seconds.
- Keep active shards at 4 for the whole round.
- Raise heat cap from 8 to 10.
- Make ports larger.

If grabbing feels unreliable:

- Increase shard colliders.
- Increase direct interactor radius.
- Move feeder pads closer.
- Remove upper pads.

---

## 15. User Flow

### First Launch

1. Splash/title appears briefly.
2. Main menu appears in a simple station bay scene.
3. Player selects **Play**.
4. Short comfort/safety note appears once:
   - Stay inside your boundary.
   - Keep the play space clear.
   - Use controllers.
5. Calibration/recenter screen appears.
6. Player starts the round.

### Normal Launch

1. Main menu.
2. Play.
3. Calibration/recenter.
4. Round.
5. Results.
6. Play again or return to menu.

---

## 16. Screens and Menus

### Splash Screen

**Purpose:** Fast brand moment, not a loading-heavy intro.

**Content:**

- Title: **Starforge Relay**
- Small subtitle: **A VR reactor arcade**
- Optional rotating shard or core icon.

**Duration:** 1 to 2 seconds or skip instantly when loading is complete.

### Main Menu

**Layout:**

- Player stands in front of a simple holographic console.
- Title above the console.
- Primary button: **Play**
- Secondary buttons: **How To**, **Settings**, **Credits**
- Small decorative star core can idle behind the menu.

Menu should feel like an in-world game console, not a flat webpage.

### How To

Three compact steps with icons or tiny 3D examples:

1. Grab glowing shards.
2. Match shard color to reactor port.
3. Stabilize the core before heat maxes out.

Keep this screen visual and short.

### Settings

Required:

- Sound: On/Off.
- Haptics: On/Off.

Optional if trivial:

- Player height mode: Standing/Seated, only if it is already comfortable in the build.
- Brightness: Normal/High.
- Color assist: On/Off. When enabled, ports and shards use stronger shape markers.

### Credits

Show:

- Asset name.
- Creator/source.
- License.
- Link.

Keep the credits accessible from the main menu.

### Calibration/Recenter Screen

**Purpose:** Make sure the reactor appears in a comfortable place.

**Content:**

- UI text: **Stand or sit comfortably and face forward**
- Buttons:
  - **Start**
  - **Recenter**
  - **Back**

**Implementation note:** If custom recenter is not ready, show **Use the Quest recenter shortcut if needed** in this screen only. Avoid long tutorial copy elsewhere.

### Gameplay HUD

HUD should be diegetic and readable:

- Timer on top of central console.
- Score on side console.
- Combo as small glowing number near the core.
- Reactor charge as ring segments around the core.
- Heat meter as red/orange bar on the lower console.
- Pause button on console or controller menu button.

Avoid locking large UI to the player's face. World-space UI should be large enough to read without leaning.

### Pause Menu

Buttons:

- Resume.
- Restart.
- Settings.
- Main Menu.

Timer, shard lifetime, spawning, and core animation intensity should pause.

### Tracking Lost Overlay

If headset or controllers lose tracking and this is easy to detect:

- Pause gameplay timer.
- Dim interactive objects.
- Show: **Tracking lost. Hold still and face the reactor.**
- Resume automatically when tracking returns if possible.

Do not count tracking loss as failure. If this overlay is not implemented in MVP, brief tracking loss should still not create heat or wrong-insert penalties by itself.

### Results Screen

Show:

- Result title:
  - **Core Stable!** for victory.
  - **Partial Relay** for time-out.
  - **Overloaded** for heat failure.
- Star rating.
- Correct shards inserted.
- Final heat.
- Score.
- Best score if available.
- Buttons:
  - **Play Again**
  - **Main Menu**

---

## 17. Visual Direction

**Style:** Low-poly sci-fi station with a bright magical-tech reactor. The scene should feel like a compact VR diorama: clean geometry, strong silhouettes, glowing color accents, and a dark starfield background.

**Color palette:**

- Deep neutral station colors: dark gray, slate, near-black metal.
- Solar yellow.
- Ion cyan.
- Pulse magenta.
- Heat orange/red.
- White core highlights.

Avoid making the entire game blue/purple. The station can be dark, but the interactable objects must be bright and distinct.

**Lighting:**

- Use baked or static-looking lighting where possible.
- Prefer unlit/emissive materials for shards, ports, UI, and core.
- Use fake glow halos with small transparent sprites or billboard meshes.
- Real-time shadows are optional and should be off on Quest 2 unless profiling says they are safe.
- URP bloom and other post-processing are stretch polish after profiling, not part of the MVP target.

**Scale feel:**

- The reactor should feel close and toy-like, not massive.
- Shards should be large enough to grab confidently.
- Ports should look oversized and forgiving.

**VFX:**

- Correct insert: short beam from port to core.
- Wrong insert: small sparks and brief red flash on the port.
- Expired shard: quick fizzle/smoke puff.
- Combo bonus: ring pulse around the core.
- Victory: core becomes a bright stable star, rings spin faster, particles rise. Do not require a complex opening animation.
- Overload: red sparks, one smoke puff, lights dim and recover.

Keep particle counts low. The visual punch should come from timing, contrast, and light direction, not density.

---

## 18. Audio Direction

**Audio tone:** Clean sci-fi arcade, bright and satisfying, not harsh.

Required sounds:

- UI select.
- Round start / reactor wake.
- Grab shard.
- Release shard.
- Correct insert.
- Wrong insert.
- Expired shard fizzle.
- Combo bonus.
- Victory stabilization.
- Overload.

Optional:

- Quiet reactor hum loop.
- Subtle station ambience.
- Low warning beep when heat is high.

Volume recommendations:

- Grab/release sounds are frequent, so keep them short.
- Correct insert should be satisfying but not loud.
- Heat warnings should not annoy the player.
- Ambient loop must be quiet and easy to disable.

---

## 19. Suggested Free Assets

All listed assets were selected because they are free and suitable for a small Quest prototype. Links and license labels were rechecked on 2026-06-01. The implementation chat should still verify final downloaded files and include license text/credits in the project if required by the distribution workflow.

| Asset | Use | Source | License | Notes |
|---|---|---|---|---|
| Kenney Space Station Kit | Station bay walls, floor, sci-fi interior props | https://kenney.nl/assets/space-station-kit | Creative Commons CC0 | Primary station interior pack. Source page lists 90 files and CC0 license. |
| Kenney Modular Space Kit | Modular station pieces, rails, panels, animated/variant parts | https://kenney.nl/assets/modular-space-kit | Creative Commons CC0 | Useful if Space Station Kit pieces are not enough. Source page lists 40 files and CC0 license. |
| Kenney Space Kit | Background planets, asteroids, small ships, set dressing | https://kenney.nl/assets/space-kit | Creative Commons CC0 | Source page lists 150 files and CC0 license. Use background pieces sparingly. |
| Quaternius Ultimate Space Kit | Backup/alternate space props, planets, asteroids, sci-fi objects | https://quaternius.com/packs/ultimatespacekit.html | CC0 | Page states FBX/OBJ/glTF/Blend formats and free use in personal/commercial projects. Good backup if Kenney style is not enough. |
| Kenney Particle Pack | Glow sprites, sparkles, beam textures, fizzle effects | https://kenney.nl/assets/particle-pack | Creative Commons CC0 | Source page lists 80 VFX files, 512 x 512 tile size, CC0 license. |
| Kenney Interface Sounds | UI clicks and menu feedback | https://kenney.nl/assets/interface-sounds | Creative Commons CC0 | Source page lists 100 audio files and CC0 license. |
| Kenney Sci-fi Sounds | Reactor, laser, engine, energy feedback | https://kenney.nl/assets/sci-fi-sounds | Creative Commons CC0 | Source page lists 70 audio files and CC0 license. Good main SFX pack. |
| Magic Spell SFX by JaggedStone | Optional energy pickup/stabilization sweeteners | https://opengameart.org/content/magic-spell-sfx | CC0 | OpenGameArt page lists CC0 and no attribution needed. Use only if it blends with sci-fi sounds. |
| 80 CC0 RPG SFX by rubberduck | Backup short magic/item sounds | https://opengameart.org/content/80-cc0-rpg-sfx | CC0 | Useful backup. Use selectively to avoid audio mismatch. |
| Poly Haven HDRIs | Optional starry/night skybox or reflection source | https://polyhaven.com/ and https://polyhaven.com/license | CC0 | Poly Haven assets are CC0. Downscale HDRI/JPG for Quest; do not ship huge 8K textures unless needed. |

### Recommended Background Choice

For MVP, prefer a simple black/dark sky dome with procedural or hand-placed star dots and a few Kenney Space Kit props. Use Poly Haven only as optional polish, and only after downscaling to a Quest-friendly texture size.

### Recommended MVP Asset Choices

Use this minimal set first:

- Kenney Space Station Kit for station/interior.
- Kenney Space Kit for background space props.
- Kenney Particle Pack for fake glow and VFX sprites.
- Kenney Sci-fi Sounds for gameplay SFX.
- Kenney Interface Sounds for UI.

Skip Quaternius, Poly Haven HDRIs, and extra OpenGameArt sounds until the core loop is playable.

---

## 20. Asset Integration Notes for the Technical Chat

These are design-facing requirements, not a required implementation recipe:

- Prefer one visual family first. Mixing Kenney and Quaternius can work, but only after scale/materials are normalized.
- Keep total unique materials low.
- Use simple unlit or URP Lit materials.
- Make shard/core glow with emissive colors and separate halo planes instead of depending on bloom.
- Downscale large textures for Quest.
- Keep station props static.
- Keep background planets/ships far away and non-interactive.
- Avoid transparent VFX filling the player's full view.
- Keep dropped shard behavior scripted and predictable; do not tune throwing physics for MVP.
- Do not block the prototype on perfect art. Shards, ports, and core can all be primitives in the first pass.

---

## 21. MVP Implementation Acceptance Criteria

The prototype is acceptable when:

- App launches on Quest 2 or Quest 3.
- Player reaches a main menu.
- Player can start **Stabilize Run**.
- Player appears in a stationary station bay.
- Reactor core and three ports are visible in front of the player.
- Shards spawn on feeder pads.
- Player can grab shards with either controller.
- Player can release shards into ports.
- Correct, wrong, and expired shards behave differently.
- Stabilization progress can reach victory.
- Timer can reach time-out.
- Heat can reach overload.
- Results screen appears.
- Sound can be turned off.
- Haptics can be turned off if implemented.
- The game remains responsive and comfortable during normal play.
- Quest 2 performance is checked on device before calling it demo-ready.
- The build is still acceptable if custom tracking-lost UI is not implemented, as long as brief tracking loss does not create unfair penalties.

---

## 22. Practical MVP Cut Line

If time gets tight, protect the playable loop first. Cut or simplify features in this order.

**Must keep:**

- Stationary VR scene.
- Central reactor.
- Three colored ports.
- Grabbable shards.
- Correct insert, wrong insert, timer, progress, heat, results.
- Basic sound feedback.

**Can be simplified without breaking the game:**

- Station bay can be a small platform plus a few panels instead of a full room.
- Victory animation can be scale-up, brighter material, ring spin, and particle burst.
- Wrong insert can be a red flash instead of sparks.
- Expired shard can simply disappear with a sound.
- Heat UI can be simplified to 8 red segments around the core or on the console. Do not keep invisible heat failure; if heat cannot be shown clearly, disable overload and use timer-only failure.
- How To can be one overlay instead of a separate polished screen.
- Credits can be a plain scrollable text panel.

**Cut first if needed:**

- Splash screen.
- Best score.
- Seated/standing setting.
- Brightness setting.
- Ambient loop.
- Poly Haven skybox.
- Extra background ships/planets.
- Controller button shortcuts beyond grab and UI select.

This cut line keeps the result playable and portfolio-presentable instead of spreading effort across polish before the core loop works.

---

## 23. Practical State Machine

The implementation can be simple if it follows a small set of states:

1. **Boot**
   - Load settings and required assets.
2. **MainMenu**
   - Show Play, How To, Settings, Credits.
3. **Calibration**
   - Show Start/Recenter/Back.
4. **Playing**
   - Timer runs, shards spawn, grabs and socket releases are evaluated.
5. **Paused**
   - Timer, shard lifetime, and spawning stop.
6. **TrackingLost**
   - Optional state only if tracking-loss detection is easy. Gameplay pauses until headset/controllers recover tracking.
7. **RoundComplete**
   - Victory, time-out, or overload feedback plays briefly.
8. **Results**
   - Score and replay options are shown.

This avoids ambiguous transitions and keeps debugging manageable.

---

## 24. Practical Playability Checks

Before calling the prototype fun, run these checks on a real Quest device:

- A new player understands what to grab within 15 seconds.
- A new player inserts at least one correct shard in the first 20 seconds.
- The player does not need to walk or turn around.
- Shards are comfortable to reach while standing.
- If seated mode is implemented, test it separately; otherwise remove seated wording from the menu and treat standing as the supported MVP posture.
- Ports are big enough that wrong inserts feel like player choice, not precision failure.
- Colors remain readable against the station background.
- Heat creates urgency but does not instantly ruin the round.
- The reactor looks rewarding when correct shards are inserted.
- The victory moment is visually clear from inside the headset.

If these checks fail, tune object size, reach distance, port trigger volumes, shard lifetime, and VFX brightness before adding new features.

---

## 25. Demo Build Definition

The game is ready to show in a portfolio clip when it meets this stricter demo bar:

- It has been tested on Quest 2 or Quest 3 standalone, not only in editor simulation.
- Player can go from launch to gameplay without developer help.
- There is no visible debug UI.
- The station bay has at least basic dressed art.
- Shards, ports, and reactor use final-ish colors and glow.
- Grabbing feels reliable with both hands.
- Correct insert has sound, haptics, beam/VFX, and score feedback.
- Wrong insert has clear but non-annoying feedback.
- At least one victory round, one time-out round, and one overload round have been tested.
- The credits screen lists every imported third-party asset used in the build.
- A 15-second capture communicates the game instantly: grab glowing shards, match colors, power the reactor.

If any of these fail, the build may still be a useful prototype, but it is not yet a clean public demo.

---

## 26. Performance Guardrails

The game should prioritize stable VR frame rate over visual complexity.

Recommended guardrails:

- Quest 2 is the baseline.
- Keep active shards at 4 by default, never above 6.
- Keep particle bursts short, under about 0.5 to 1.0 second.
- Avoid multiple large transparent planes stacked in front of the camera.
- Avoid real-time shadows on Quest 2 unless profiling says safe.
- Avoid high-resolution textures for tiny props.
- Avoid heavy post-processing.
- Pool shards, beams, and particle effects.
- Use trigger volumes for sockets instead of complex collision logic.
- Keep physics simple and mostly kinematic.
- Do not implement throwable shard scoring in MVP.
- Do not use constant scene-wide searches during gameplay.
- Do not spawn gameplay objects behind the player.

Profiling should happen on Quest 2 if available. If only Quest 3 is available, keep extra headroom and assume Quest 2 will be less forgiving.

---

## 27. Accessibility and Comfort

MVP accessibility and comfort requirements:

- No locomotion.
- No artificial camera movement.
- No required quick spinning.
- No required high reach.
- Standing is the supported MVP posture; seated support is optional.
- Sound off setting.
- Haptics off setting if haptics are implemented.
- Large readable world-space UI.
- Clear color contrast.
- Wrong insert penalty is mild.
- If custom tracking-loss handling is implemented, it pauses gameplay. If not, brief tracking loss must not create unfair heat or wrong-insert penalties.

Recommended accessibility improvement if time allows:

- Color assist mode with shape markers:
  - Solar: circle.
  - Ion: triangle.
  - Pulse: diamond.

Comfort requirements:

- No jump scares.
- No objects flying rapidly into the player's face.
- No sudden full-screen flashes.
- No horror tone.
- No gameplay that encourages ignoring the physical boundary.

---

## 28. Tutorial Copy

Keep copy short. Suggested English UI text:

- **Grab energy shards**
- **Match color to port**
- **Stabilize the core**
- **Heat is rising**
- **Core Stable!**
- **Partial Relay**
- **Overloaded**
- **Tracking lost. Face the reactor.**
- **Stand or sit comfortably**

Do not add long tutorial paragraphs in the game UI.

---

## 29. Data and Persistence

MVP persistence:

- Sound setting.
- Haptics setting if implemented.
- Best score if trivial.
- Player height mode if implemented.

Do not add:

- User profiles.
- Cloud sync.
- Analytics.
- Remote config.
- Unlocks.
- Inventory.
- Mission progression.

---

## 30. Stretch Goals

Only consider these after the MVP is playable on a Quest device:

1. **Color Assist Shapes**
   - Stronger shape symbols on shards and ports.
   - Helps accessibility and readability in video capture.

2. **Speed Streak Pulse**
   - If the player inserts 5 correct shards quickly, trigger a brighter reactor pulse and a small score bonus.
   - This uses the existing combo system and avoids adding throwing physics.

3. **Unstable Black Shards**
   - Rare dark shard appears.
   - Player must place it into a disposal vent or tap it away.
   - Adds variety, but should not be in MVP if sorting is not fun yet.

4. **Photo/Capture Moment**
   - After victory, hide UI and keep the stable star visible for a clean portfolio capture.

5. **Quest 3 Visual Profile**
   - Slightly more particles.
   - Optional bloom-like polish after profiling, preferably still using fake glow before post-processing.
   - Higher render scale if profiling allows.

6. **Hand Tracking Mode**
   - Grab with hands instead of controllers.
   - Only after controller version feels reliable.

---

## 31. Risks and Mitigations

| Risk | Why it matters | Mitigation |
|---|---|---|
| Grabbing feels unreliable | The whole game depends on hand interaction | Use large colliders, XR Interaction Toolkit direct grab, close feeder pads, and forgiving sockets. |
| Visuals look too simple | Portfolio demo needs instant appeal | Make core, shards, ports, beams, and particles polished before adding more mechanics. |
| Quest 2 frame rate drops | VR comfort and store readiness depend on stable FPS | Use Quest 2 as baseline, fake glow, low particle count, static props, object pooling. |
| Player gets confused by colors | Sorting must be readable fast | Use three distinct colors plus shape markers. |
| Player moves outside boundary | Small VR demos should be safe | Keep all objects in front and within arm reach; no locomotion. |
| Scope grows into a full sci-fi game | Space theme invites enemies, guns, levels, missions | Keep only Stabilize Run in MVP; put extras in Stretch Goals. |
| Wrong inserts feel unfair | VR precision can be messy | Make ports large, only count wrong when released clearly inside wrong port, no penalty for empty drops. |
| Asset style mismatch | Mixed free packs can look messy | Start with Kenney only; add Quaternius only if needed and visually normalized. |

---

## 32. Development Order Recommendation

Suggested order for the technical chat:

1. Create Unity XR project setup for Quest using URP and controller input.
2. Create main menu and calibration flow.
3. Build a primitive reactor, three primitive ports, and primitive shards.
4. Implement direct grabbing with Grip.
5. Implement port trigger validation for correct/wrong release.
6. Implement timer, score, combo, progress, and heat.
7. Implement victory, time-out, overload, and results.
8. Add basic sounds and haptics.
9. Import Kenney station/space assets and dress the scene.
10. Add fake glow, beams, and particle feedback.
11. Test on Quest 2/3 and tune reach, sizes, timing, and performance.

This order ensures the game becomes playable before art polish.

---

## 33. Final Sanity Check

This design has been checked for common first-VR-project problems:

- **No oversized feature set:** One mode, one station bay, one interaction type, one round structure.
- **No locomotion dependency:** The player stays stationary.
- **No advanced VR requirement:** Controllers and basic grab/socket interactions are enough.
- **No complex physics requirement:** Sorting uses trigger volumes and short animations.
- **No unclear win condition:** Insert 20 correct shards before timer or heat failure.
- **No unclear player action:** Grab shard, match color, release in port.
- **No heavy asset dependency:** The MVP can be built with primitives, then dressed with free CC0 packs.
- **No Quest 3-only assumption:** Quest 2 is the baseline.
- **No contradiction between spectacle and performance:** The look relies on low-poly props, fake glow, short particles, sound, and haptics.
- **No hidden campaign scope:** Progression, enemies, multiplayer, and extra modes are explicitly out of MVP.

The final intended MVP is a compact, polished VR arcade toy: stand at a reactor, grab glowing shards, match them to ports, stabilize a star, replay for a better score.

---

## 34. Open design questions (resolve when the relevant feature is built)

Internal design ambiguities surfaced in a 2026-06-03 consistency review. **None affect the MVP** (active
shards = 4, standing-only, 20-shard requirement), so they are deferred — resolve each in the task brief
that implements the feature it touches; until then the MVP behaviour defined above is authoritative.

1. **Active-shard escalation (5–6) vs. "target 4".** §12 escalates to 5 (after 8 accepts) / 6 (after 15)
   "only if extra feeder slots exist," but the spawn rule and §6/§26 treat the target as 4, and the extra
   "upper" pads are optional (§10) and would sit above the §9/§27 reach ceiling. In the 4-pad MVP the
   escalation never fires. On implementation, either confirm 4-only, or define upper pads within reach and
   make the escalation target explicit in the spawn loop.
2. **Balance lever "20 → 24" vs. star bands.** §14's "raise the stabilization requirement to 24" would
   leave the §13 star bands (0–5 / 6–11 / 12–19 / 20) unscaled (20–23 unmapped). If ever used, rescale the
   bands to the requirement. MVP stays at 20.
3. **Lifetime lever "14 → 12".** §14's "reduce starting lifetime to 12" collides with the existing §12
   ramp (14 → 12 after 10 accepts). When tuning, clarify whether it lowers the *start* (flattening the
   ramp) or the late value.
4. **Seated wording.** UI copy "Stand or sit comfortably" (§16/§28) implies seated support, but §9/§24
   make standing the MVP posture and say to strip seated wording if seated isn't shipped. Use "Stand
   comfortably and face forward" until seated mode is actually implemented.
5. **Time-out wording.** §12 Time-out says "stop spawning" but — unlike Victory/Overload — doesn't say
   "stop timer"; harmless (the timer is already 0), but for parity say "stop timer (already 0) and
   spawning."
