# Unity VR Implementation Guardrails for Starforge Relay

> **Purpose.** This document is an implementation contract for the **Starforge Relay** VR prototype. It exists to prevent low-quality LLM-generated Unity code: giant scripts, hidden global state, excessive singletons, per-frame scene searches, untestable gameplay logic, ballistic-throwing hacks, and architecture that collapses as soon as the prototype grows.
>
> **Scope.** These rules apply *after* the technical chat sets up the concrete Unity + XR stack (OpenXR + XR Interaction Toolkit on URP, Android/Quest target). They do **not** replace the game design document. They define how the code should be structured while implementing it.
>
> **Companion documents:** `docs/product/game-design.md` (the GDD — design source of truth) and the VR project setup plan (OpenXR + XRI + MCP for Unity).
>
> **Design numbers live in the GDD.** Where this doc cites values (90 s round, 20 stabilization, heat cap 8, 4–6 active shards, 14→12 s lifetime, combo +50 / 5, respawn 0.3–0.8 s), the **GDD is the source of truth**. Keep them in `RoundConfig`, not hard-coded in logic.

---

## 1. Non-Negotiable Engineering Goals

The project should be small, but not sloppy.

The implementation **must optimize for**:

- **Clear ownership:** each system has one reason to change.
- **Low coupling:** gameplay, XR rig/interaction, UI, audio, and persistence do not directly own each other.
- **High cohesion:** related rules live together.
- **Inspectable behavior:** important runtime state can be seen in the Inspector or a small debug view during development.
- **Testable gameplay logic:** scoring, combo, heat, stabilization, timer, and shard spawn planning can be tested **without a headset**.
- **VR performance:** stable **72 FPS on Quest 2** (the baseline), no avoidable per-frame allocations, no per-frame scene searches, no uncontrolled instantiate/destroy loops.
- **VR comfort:** no locomotion, no artificial camera movement, no forced turning in the MVP. In VR, lag and unexpected motion = discomfort.
- **Fast MVP delivery:** simple architecture that protects the codebase; do not introduce enterprise patterns that slow down a short prototype.

The implementation **must not optimize for**:

- "Everything is a manager."
- "Everything is a singleton."
- "Everything talks through one global event bus."
- "All logic lives in `MonoBehaviour.Update`."
- "It ran once on my Quest, so the architecture is fine."
- "Use physics throwing to insert shards." (Explicitly out — see §13.)

---

## 2. Source-Backed Principles

These rules are based on official Unity, Meta, and Microsoft guidance. Prefer these sources over random blog posts.

| Topic | Source | Practical rule for this project |
|---|---|---|
| XR Interaction Toolkit architecture | https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.0/manual/architecture.html | Use the XRI Interaction Manager + interactors/interactables. Do not reinvent grabbing/socketing with raw `OnTriggerEnter`. |
| XR Grab Interactable | https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.0/manual/xr-grab-interactable.html | Shards are grab interactables. Disable throw-on-detach (no ballistic throwing in MVP). |
| Near-Far Interactor (XRI 3.x) | https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.0/manual/near-far-interactor.html | Modern single-component interactor combining near grab + far ray; an alternative to separate Direct + Ray interactors. |
| XR Socket Interactor | https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.0/manual/xr-socket-interactor.html | Ports are socket interactors. Accept the shard on release, then validate color **in code** (§6) — do **not** hard-filter color at the socket, or wrong-insert events never fire. |
| Interaction layers | https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.0/manual/interaction-layers.html | Use interaction layer masks to separate shards from UI/other interactors — not to gate gameplay color matching (§6). |
| OpenXR + Meta Quest | https://developers.meta.com/horizon/documentation/unity/unity-xr-plugin/ | One XR Origin, OpenXR provider with Meta Quest Support, Oculus Touch interaction profile. Do not enable multiple XR providers for Android at once. |
| Input System | https://docs.unity3d.com/Packages/com.unity.inputsystem@1.11/manual/index.html | XRI runs on the Input System. Use action-based input (XRI Default Input Actions), never legacy `Input.GetButton` or direct `UnityEngine.XR` polling in gameplay. |
| Single Pass Instanced stereo | https://docs.unity3d.com/Manual/SinglePassInstancing.html | Render Mode = Single Pass Instanced (Multiview on Quest). Custom shaders must be SPI-correct. |
| Meta Quest performance | https://developers.meta.com/horizon/documentation/unity/po-unity/ | 72 FPS floor (72 Hz default); budget draw calls; avoid transparent overdraw; fake glow over bloom; use Fixed Foveated Rendering / dynamic resolution when GPU-bound; profile on device. |
| ScriptableObject architecture | https://unity.com/how-to/architect-game-code-scriptable-objects | Use ScriptableObjects for stable config, event channels, designer-editable data. Not as uncontrolled mutable global state. |
| Design patterns | https://learn.unity.com/course/design-patterns | Use patterns only where they reduce complexity. Do not copy patterns for decoration. |
| State pattern | https://learn.unity.com/tutorial/develop-a-modular-flexible-codebase-with-the-state-programming-pattern | Small state machine for app/round flow. Avoid one massive switch owning UI, XR, and gameplay. |
| Observer / events | https://learn.unity.com/tutorial/create-modular-and-maintainable-code-with-the-observer-pattern | Use typed, feature-scoped events to decouple systems. |
| MVP UI pattern | https://learn.unity.com/tutorial/build-a-modular-codebase-with-mvc-and-mvp-programming-patterns | Keep UI views dumb. Presenters translate game state into UI. |
| Object pooling | https://docs.unity3d.com/ScriptReference/Pool.ObjectPool_1.html | Pool shards, beams, and short-lived VFX. |
| `GameObject.Find` | https://docs.unity3d.com/ScriptReference/GameObject.Find.html | No scene-wide name searches in gameplay or per-frame code. Prefer serialized references and cached dependencies. |
| Per-frame optimization | https://docs.unity3d.com/Manual/MonoBehaviour-optimization.html | Avoid hundreds of idle `Update` methods. Only tick objects that need it. |
| Garbage collection | https://docs.unity3d.com/Manual/performance-garbage-collector.html | Aim for near-zero GC allocations per frame during the active round. |
| Microsoft DI guidelines | https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection/guidelines | Use DI instead of static/global access. Avoid service-locator behavior and incorrect lifetime capture. |
| VContainer (if a container is chosen) | https://vcontainer.hadashikick.jp/ | If DI container is needed, prefer a Unity-focused container with clear scoping and constructor injection. |
| C# conventions | https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions | Clear, consistent, specific exceptions, simple readable code. |

---

## 3. Target Architecture Shape

Use a thin Unity/XR layer over testable plain C# logic.

Recommended dependency direction:

```text
UI Views / XR Views / Unity MonoBehaviours
        depend on
Presenters / Controllers / Scene Adapters
        depend on
Gameplay Services / Pure C# Rules
        depend on
ScriptableObject Config / Plain Data
```

Rules:

- MonoBehaviours adapt Unity lifecycle, scene references, XR interactors/interactables, prefabs, and animations.
- Gameplay rules live in plain C# classes where practical.
- ScriptableObjects hold configuration, event channels, definitions, and stable shared data.
- UI reads state through presenters or view models, not by querying gameplay objects directly.
- The XR rig and interaction layer publish *intents* (grabbed, released-into-socket) to gameplay; they do not own scoring/heat/round rules.

**Good example:**

```text
ControllerInputAdapter (XRI actions) -> ShardGrabInteractable selected
-> PortSocket receives the shard on release; PortValidationService confirms the color matches
-> RoundController.OnCorrectInsert(shard, port)
-> ScoreService / HeatService / StabilizationProgress update state
-> RoundEvents raise CorrectInsert / ScoreChanged / HeatChanged
-> HUDPresenter updates HUDView; AudioService + HapticService react
```

**Bad example:**

```text
ShardView finds GameManager singleton -> changes score, heat, timer, UI text,
plays audio, recolors the core, fires controller haptics, and spawns the next shard directly.
```

---

## 4. Recommended Folder Layout

Use the project convention if one already exists. Otherwise:

```text
Assets/
  _Project/
    Art/
    Audio/
    Materials/
    Prefabs/
      XR/          (XR Origin rig, controllers, interactors)
      Gameplay/    (core, ports, shards, feeder pads)
      UI/
      VFX/
    Scenes/
    ScriptableObjects/
      Config/
      Events/
      Definitions/
    Scripts/
      App/
      XR/
      Gameplay/
      UI/
      Audio/
      Persistence/
      Infrastructure/
      Composition/
      Debugging/
    Tests/
      EditMode/
      PlayMode/
```

Optional assembly definitions:

- `StarforgeRelay.Runtime`
- `StarforgeRelay.Tests.EditMode`
- `StarforgeRelay.Tests.PlayMode`

Keep first-party code/content under `_Project/` and **separate from imported assets** (Kenney/Quaternius packs land in their own folders). Do not fight assembly definitions during MVP if the project is not ready for them, but keep the folder boundaries either way.

---

## 5. Namespaces and Naming

Use one root namespace:

```csharp
namespace StarforgeRelay.Gameplay
namespace StarforgeRelay.XR
namespace StarforgeRelay.UI
namespace StarforgeRelay.Audio
namespace StarforgeRelay.Persistence
namespace StarforgeRelay.Infrastructure
namespace StarforgeRelay.Composition
```

Naming rules:

- `*Controller` coordinates user/game actions in a specific feature.
- `*Service` performs reusable non-visual work with no scene ownership.
- `*Presenter` maps state/events to UI views.
- `*View` owns scene objects, visuals, animations, and Unity references.
- `*Config` is a ScriptableObject or immutable data object.
- `*Definition` describes a game item (shard color/type, port).
- `*State` is runtime state or a state-machine state.
- `*EventChannel` is a typed ScriptableObject event channel.
- `*CompositionRoot`, `*LifetimeScope`, or `*Installer` wires dependencies and contains no gameplay rules.

Avoid vague names: `GameManager`, `MainManager`, `XRManagerCustom`, `SystemController`, `Utils`, `Helper`, `Data`. If a class needs "Manager", it must be obvious what it manages and why no more specific name works.

**Casing, formatting, member order, `var` / `using` rules, and the rest of the C# convention live in
[`csharp-style.md`](csharp-style.md)** (an engine-agnostic spine + a Unity overlay), machine-enforced by the
repo-root `.editorconfig`. The essentials: `PascalCase` for types / methods / properties / constants;
**private, protected, and internal *fields* are `_camelCase`** (static `s_camelCase`, thread-static
`t_camelCase`) — **not** bare `camelCase`; locals and parameters are `camelCase`; interfaces `IPascal`; one
public type per file; Allman braces; `using` directives outside the namespace. Don't restate the full list
here — one home per fact, see `csharp-style.md`.

---

## 6. Core Systems for Starforge Relay

These conceptual systems must exist with **separated responsibilities**. Exact file names may vary. The systems trace directly to the GDD objects: **Star Core**, **Reactor Ports**, **Energy Shards**, **Feeder Pads**, **Station Bay**.

> **Right-size this — do not create 50 files on day one.** The lists below are *responsibility boundaries*, not a file mandate. It is fine to start with related responsibilities collapsed into fewer classes (e.g. one `RoundController` that holds score/combo/heat/progress) and split them out only when a class earns it. The playable vertical slice (§21) comes before the file taxonomy. The point is "no single GodObject that mixes XR + UI + scoring + spawn + audio," not "maximize the class count."

### Composition

Owns dependency wiring only.

Recommended classes: `StarforgeRelayCompositionRoot` (manual DI), or `GameLifetimeScope` / `StarforgeRelayInstaller` if an approved DI container is used.

Responsibilities: create plain C# services; connect ScriptableObject configs to services; connect scene views/adapters to presenters/controllers; create factories and pools; expose one obvious place to inspect wiring.

Must not: contain scoring/heat formulas, XR interaction logic, UI formatting, or become a gameplay manager.

### App Flow

Owns high-level state (from GDD §23):

- Boot
- MainMenu
- Calibration (recenter / comfort note)
- Playing
- Paused
- TrackingLost *(optional — only if detection is cheap)*
- RoundComplete (victory / overload / time-out feedback)
- Results

Recommended classes: `AppStateMachine`, `IAppState`, `BootState`, `MainMenuState`, `CalibrationState`, `PlayingState`, `PausedState`, `TrackingLostState`, `RoundCompleteState`, `ResultsState`.

Keep each state small. A state enters, exits, and coordinates active systems. It must not bundle scoring formulas, UI formatting, XR details, and audio lookup together.

### XR Rig & Interaction *(replaces the AR "Placement" system)*

Owns the headset/controller rig and the XRI plumbing only.

Recommended classes: `XRRigController` (thin wrapper over the XR Origin (VR) rig), `ControllerInputAdapter`, `RecenterController`, `XRTrackingMonitor`, `WorldSpaceUIInput` (XR UI Input Module wiring).

Responsibilities: own the single XR Origin, XR Interaction Manager, and Input Action Manager; expose grab/release intents and UI-ray (Trigger) selection; perform recenter; optionally detect headset/controller tracking loss and notify app flow.

Must not: calculate score/heat; own the round timer; decide which port is correct; update HUD directly; spawn gameplay shards except through a gameplay-facing command such as `StartRound(sceneRoot)`.

Best practice: use the **XRI Starter Assets** rig as the base; exactly **one** XR Interaction Manager in the scene; set the **XR Origin tracking-origin mode to Floor** (standing play — real floor maps to y = 0, reactor sits at chest height); controller grab on **Grip**, world-space UI select on **Trigger**; **no locomotion providers** in MVP. For an arm's-reach game a **Direct Interactor** (near grab) + **Ray Interactor** (UI) per controller is sufficient; XRI 3.x's **Near-Far Interactor** is a modern single-component alternative that combines near grab and far ray.

### Round Gameplay

Owns the Stabilize Run loop.

Recommended classes: `RoundController`, `RoundState`, `RoundTimer`, `ScoreService`, `ComboTracker`, `HeatService`, `StabilizationProgress`, `ShardSpawnPlanner`, `ShardSpawner`, `ShardPool`, `PortValidationService`.

Responsibilities: start/restart/end a round; tick the round timer; validate inserts (color match); apply correct/wrong/expired rules; maintain score, combo, heat, and stabilization progress; request shard spawns from the pool; raise typed events for UI/audio/VFX/haptics; resolve concurrent end states (**victory > overload > time-out**, see §16).

Must not: call XRI interactor APIs directly for selection results beyond a thin adapter; read UI button components; use `GameObject.Find`; depend on scene object names.

### Shard Views & Interaction

Own visual/grabbable shard objects only.

Recommended classes: `ShardView`, `ShardVisual`, `ShardGlow`, `ShardMotion`, plus the XRI `XRGrabInteractable` component.

Responsibilities: display color/shape; own collider + grab interactable (collider larger than the visible mesh for forgiving grabs); play idle bob/rotate; expose a `ShardId`/color identity to gameplay; play local accepted/rejected/expired animation when requested; return to pool when finished.

Interaction rules:

- Shards are `XRGrabInteractable` with **throw-on-detach disabled** and kinematic/instantaneous movement (no ballistic flight — see §13).
- Either hand can grab. Grabbing must be forgiving; exact finger poses are not required.
- Released in empty space → gameplay lerps the shard back to its feeder pad (scripted, not physics).

Must not: modify score/heat directly; choose the target/correct port; find UI or core objects.

### Reactor Core & Ports

Own the central objective visuals and the color-matching sockets.

Recommended classes: `ReactorCoreView`, `CoreStatePresenter`, `RingView`, `PortView`, `PortSocket` (wraps `XRSocketInteractor`), `PortValidationService`, `BeamVfx`.

Responsibilities:

- Core presents GDD states (Dormant / Active / Charging / Heating / AlmostStable / Stabilized / Overloaded) driven by gameplay events — the view does not compute progress/heat.
- Each port detects a shard **released inside it** and routes the result through `PortValidationService`, which checks color: a **match** → consume the shard, fire the beam into the core, raise `CorrectInsert`; a **mismatch** → reject with a soft push-back, raise `WrongInsert`, and the shard returns to its feeder pad.
- A release counts as **wrong** only when the shard is clearly inside a wrong port. Dropping outside any port has no penalty.

> **Critical design/implementation note — do NOT hard-filter color at the interactor.** The GDD requires a *wrong* insert to register (+1 heat, combo reset, spark, push-back). If you gate the socket by color (per-color Interaction Layer Mask, or an `IXRSelectFilter` that returns `false` for the wrong color), a wrong-color shard is silently never selected — so **no wrong-insert event fires and the heat mechanic dies quietly**. Instead: let the port accept *any shard* (a single shared "Shard" interaction layer separates shards from UI/other interactors), then validate **color in `PortValidationService` on select**, and force-eject + penalize on mismatch. Interaction Layer Masks are the right tool for *interactor/interactable separation*, the wrong tool for *gameplay color matching here*.

Must not: own scoring/heat/round rules; recompute progress; search the scene.

### UI

Simple MVP-style split, **world-space** (diegetic) UI.

Recommended classes: `MainMenuView`, `CalibrationView`, `HUDView`, `PauseMenuView`, `ResultsView`, `SettingsView`, `HowToView`, `CreditsView`; presenters `HUDPresenter`, `ResultsPresenter`, `SettingsPresenter`.

Rules: views expose serialized references and UI events; presenters subscribe to gameplay events and call view methods; views contain no gameplay decisions; presenters know nothing about XRI internals. HUD is diegetic (timer on console, score on side console, combo near the core, reactor charge as ring segments, heat as a bar). World-space UI must be large enough to read without leaning; never lock big panels to the player's face.

### Audio, VFX, and Haptics

Recommended classes: `AudioService`, `AudioCueConfig`, `HapticService`, `HapticConfig`, `VfxPool`, `BeamVfx`, `SparkVfx`, `FizzleVfx`, `ComboPulseVfx`, `StabilizeVfx`.

Rules: gameplay emits semantic events (`CorrectInsert`, `WrongInsert`, `ShardExpired`, `ComboMilestone`, `RoundWon`, `RoundOverloaded`); audio/VFX/haptics react. Gameplay never references a specific `AudioClip`, `ParticleSystem`, or controller haptic call. Respect the Sound on/off and Haptics on/off settings centrally.

### Persistence

Recommended classes: `SettingsStore`, `BestScoreStore`.

Rules: wrap `PlayerPrefs` (or the chosen mechanism) behind a small interface; persist only MVP data (sound, optional haptics, optional best score); no arbitrary gameplay class reads/writes `PlayerPrefs`.

---

## 7. Data Model and ScriptableObjects

Use ScriptableObjects for configuration the implementation chat may tune in the Inspector.

Recommended assets:

- `RoundConfig`
  - `roundDurationSeconds` (90)
  - `stabilizationRequirement` (20)
  - `heatCap` (8)
  - `activeShardsDefault` (4), `activeShardsMax` (6)
  - `activeShards5AfterAccepts` (8), `activeShards6AfterAccepts` (15) — escalation applies only if the extra feeder pads exist (GDD §12); not in the as-built `RoundConfig` (MVP has 4 pads)
  - `shardLifetimeStart` (14), `shardLifetimeLate` (12), `lateLifetimeAfterAccepts` (10)
  - `respawnDelayMin` (0.3), `respawnDelayMax` (0.8)
  - `comboBonusInterval` (5), `comboBonusScore` (50), `comboHeatRelief` (1)
  - `correctScore` (10), `heatPenaltyPerHeat` (10), `victoryTimeBonusPerSecond` (2)
- `ShardDefinition`
  - `id`, `displayName`, `color` (Solar / Ion / Pulse), optional `shapeMarker`, `visualPrefab`
- `PortDefinition`
  - `id`, `color`, `shapeMarker`, `socketPrefab`
- `ReactorConfig`
  - `coreForwardDistance` (0.75–0.95), `coreHeight` (1.1–1.3)
  - feeder pad slot positions, forward arc (−50°…+50°), spawn height band, reach bounds
  - `shardWorldSize` (0.10–0.14 m)
- `AudioCueConfig` — semantic cue names → clips + volume
- `HapticConfig` — semantic cue → amplitude + duration
- `EventChannel` assets if using ScriptableObject event channels

ScriptableObject rules:

- Treat config assets as **read-only during gameplay** unless intentionally runtime variables.
- Runtime mutable values (score, combo, heat, timer, stabilization progress, current active-shard count) belong in **runtime state classes**, not permanent config assets.
- If using SO runtime variables, keep `InitialValue` and `RuntimeValue` separate so Play Mode changes do not persist to disk.

---

## 8. Dependency Rules

Dependency injection is required as a design practice. A DI container is optional.

Preferred dependency methods:

1. Serialized field references assigned in prefab/scene.
2. Constructor injection for plain C# classes.
3. A scene-level composition root that creates plain C# services and wires MonoBehaviour adapters.
4. Explicit `Initialize(...)` methods for MonoBehaviours created at runtime.
5. ScriptableObject event channels for feature-scoped notifications.

Avoid: scene-wide searches; hidden static dependencies; global service locators; static mutable state; public fields used as uncontrolled globals.

Allowed limited exceptions:

- Unity/XR-required components and static APIs (`Time`, `Application`, `PlayerPrefs`) wrapped at boundaries.
- The XRI **XR Interaction Manager** is a framework-provided scene component (one per scene). It is **not** a custom singleton and is allowed — but do not pile gameplay logic onto it.
- A single scene-level composition root may wire dependencies.
- A custom update manager only if profiling shows many idle `Update` callbacks. MVP should not need one.

Composition root policy: all cross-system wiring in one obvious place; feature classes declare dependencies via constructors or explicit `Initialize`; classes do not reach into a global container during gameplay; runtime-created shards receive dependencies from the factory/pool when spawned, not by searching the scene.

Singleton policy:

- Do not create `GameManager.Instance`, `AudioManager.Instance`, `UIManager.Instance`, or `ServiceLocator.Instance`.
- If a singleton is proposed, code-review notes must justify why a serialized reference and scene composition are insufficient, how lifecycle/reset is handled, and how tests can replace it.
- For this MVP the expected number of custom singletons is **zero**.

---

## 9. DI Container Policy

The preferred default for this MVP is **manual DI**: a scene-level composition root, constructor injection for plain C# services, serialized fields for Unity/XR scene references, explicit factories for pooled/runtime-created objects, and dependencies visible at the class boundary. This gives most DI benefits without adding a package, reflection cost, or unfamiliar lifecycle rules during a short prototype.

A DI container is allowed only if the implementation chat explicitly chooses it and explains why **before** coding, and only when at least two of these hold: the package is already installed and working; many plain C# services with non-trivial graphs; the developer is comfortable with that container; tests need easy service replacement; the app is expected to grow well beyond this prototype; container setup will not threaten the vertical slice.

If a container is added from scratch, **VContainer is the preferred candidate** (Unity-specific, GC-free, actively maintained, constructor injection, LifetimeScope scoping, diagnostics, source-gen performance); **Reflex** is a newer minimal high-performance alternative. **Zenject/Extenject are legacy** (heavier, GC overhead, low activity) and not recommended for new work; keep one only if already a dependency. Do not install more than one container, do not inject the container into gameplay classes, do not call `Resolve` from arbitrary gameplay code, and add a startup smoke test that proves all bindings resolve. DI should make dependencies more visible, not more magical.

---

## 10. Forbidden and Restricted APIs

### Forbidden in gameplay/runtime hot paths

- `GameObject.Find`, `GameObject.FindWithTag`, `Object.FindObjectOfType`, `Object.FindObjectsOfType`, `Object.FindAnyObjectByType`
- `GetComponent` inside `Update` or frequent loops
- `Camera.main` per frame (cache the XR camera reference)
- LINQ inside per-frame gameplay loops
- String-based dispatch (`SendMessage`)
- Reflection-based resolving during gameplay hot paths (container build-time reflection at startup is fine if a container is explicitly chosen and not used as a service locator)
- `Resources.Load` for normal project assets
- Repeated `Instantiate`/`Destroy` for shards/beams during active rounds
- Legacy input in gameplay: `Input.GetButton/GetAxis`, direct `UnityEngine.XR` device polling — use Input System / XRI actions
- Enabling locomotion/teleport providers (out of MVP scope)
- `new WaitForSeconds(...)` allocated repeatedly inside loops

### Allowed only at setup time with justification

- `GetComponent` in `Awake`, `Start`, or one-time initialization.
- `FindAnyObjectByType` in editor-only tooling or temporary migration code.
- Tags/layers/interaction layers for filtering, as long as code does not repeatedly search by tag.
- `Instantiate` during initial pool warmup or one-time scene setup.

### Preferred alternatives

Serialized fields; prefab references; cached component references; explicit DI; object pools; typed events; interaction-layer masks for interactor/interactable separation; cached XR camera/origin references.

Before final handoff, run a restricted-API search (PowerShell + ripgrep on Windows):

```powershell
rg -n "GameObject\.Find|FindWithTag|FindObjectOfType|FindObjectsOfType|FindAnyObjectByType|SendMessage|Resources\.Load|Camera\.main|Input\.Get" Assets
```

Any match in runtime code must be removed or justified.

---

## 11. Update, Tick, and Coroutine Rules

VR on Quest is sensitive to frame time; missed frames cause discomfort. Keep ticking explicit.

Rules:

- `RoundController` (or the active app state) owns the main gameplay tick.
- Shards may have a lightweight motion script only if necessary.
- Do not create dozens of MonoBehaviours with empty/rarely-used `Update`.
- Do not poll state in UI every frame — update UI when state changes.
- Do not check "are we paused / tracking lost" inside every object — pause/resume centrally.
- Use coroutines for short sequences; do not hide game-state transitions inside unrelated coroutines.
- If a coroutine can outlive its state, cancel it on `Exit`, `OnDisable`, or round reset.

Acceptable: `ShardMotion.Update` for the ~4–6 active shards; `RoundController.Tick(deltaTime)` from the Playing state; short animation coroutines owned by views. XRI interactors run their own update loop — do not duplicate per-frame interaction logic on top of them.

Not acceptable: every UI panel running `Update` to query score; every shard searching for the core each frame; timers scattered across unrelated MonoBehaviours.

---

## 12. Object Pooling Rules

Pool repeated short-lived gameplay objects:

- Energy shards
- Insert beams (port → core)
- Spark / fizzle / combo-pulse VFX

Do not over-pool: the single core, rings, the three ports, static station-bay props, menus.

Pool requirements:

- Pool size covers the designed max active count plus a small buffer.
- Shard pool initial capacity: **6–8** (max active 6 + buffer); max size **10–12**.
- Beam pool initial capacity: **4–6**.
- VFX pool initial capacity: **6–10** per frequently used effect.
- Returned shards must reset visual state, color, collider/grab-interactable enabled state, lifetime, event subscriptions, socket/grab state, and parent transform.
- Guard double-release in editor/dev builds.

Do not `Destroy` collected shards during gameplay. Return them to the pool.

---

## 13. XR / OpenXR + XRI Rules

These replace the AR Foundation rules. They assume OpenXR + XR Interaction Toolkit on URP, Android/Quest target (per the setup plan).

XR setup:

- Exactly **one** XR Origin (VR) and **one** XR Interaction Manager in the scene.
- OpenXR provider enabled for **Android** with **Meta Quest Support**; add the **Oculus Touch Controller** interaction profile.
- **Render Mode = Single Pass Instanced** (delivered as **Multiview** on Quest via OpenXR — the default and most performant stereo mode); **Vulkan** graphics (Meta/Unity recommended with URP); **IL2CPP + ARM64**.
- Input via the **Input System** (XRI Default Input Actions / Starter Assets). No legacy input, no direct device polling in gameplay.
- **Not in MVP:** hand tracking, passthrough, locomotion/teleport, multiple simultaneous XR providers for Android.

Interaction model:

- **Grabbing shards:** a Direct Interactor (near grab) per controller bound to **Grip** — or the XRI 3.x Near-Far Interactor. Shards are `XRGrabInteractable`.
- **Inserting shards:** ports are `XRSocketInteractor`s that accept **any** shard (one shared "Shard" interaction layer). On select, `PortValidationService` checks color: match → correct insert → beam into core → shard returns to pool; mismatch → force-eject, raise `WrongInsert` (heat/combo), shard returns to its pad. **Do not gate the socket by color** (see §6 critical note), or wrong inserts never fire.
- **World-space UI:** an XR Ray Interactor + XR UI Input Module, select on **Trigger**.
- **No ballistic throwing:** set `throwOnDetach = false` on grab interactables and use kinematic/instantaneous movement. Released-in-empty-space behavior is **scripted** (lerp back to pad after 0.5–1.0 s), never physics velocity. Do not tune throwing — it is explicitly out of scope and does not help the demo.

Tracking, comfort, and recenter:

- Optionally monitor headset/controller tracking; if implemented, `TrackingLost` pauses the timer. Brief tracking loss must **not** create heat or wrong-insert penalties by itself even if the overlay is not implemented.
- No artificial camera movement, no forced turning, no screen-space full-view flashes, no fast objects flying at the face.
- Provide recenter via the XR Origin / OpenXR recenter on the Calibration screen if easy; otherwise rely on the Quest system recenter and keep the play area forgiving.

Performance-specific:

- Avoid stacked transparent planes filling the HMD view (overdraw is expensive in stereo).
- **Fixed Foveated Rendering (FFR)** and **dynamic resolution** are the Quest-specific levers when GPU-fill-bound; enable/raise them after profiling, not blindly.
- Cache the XR camera/origin; never `Camera.main` per frame.

---

## 14. UI Architecture Rules

UI must be simple but not tangled.

Rules:

- One view class per screen/panel.
- Views expose events (`PlayClicked`, `PauseClicked`, `SettingsChanged`).
- Presenters subscribe to view events and call app/gameplay services; presenters update text/sliders/icons/buttons through view methods.
- UI text is formatted in presenters, not in gameplay services.
- UI never calls `GameObject.Find` to locate gameplay.
- UI never owns score, timer, heat, combo, or stabilization as the source of truth.
- World-space canvases sized for VR reading distance; interact via ray + Trigger.

Recommended flow:

```text
RoundController raises HeatChanged / StabilizationChanged / ScoreChanged
HUDPresenter receives events
HUDPresenter formats timer/score/combo/charge/heat
HUDView displays values (diegetic console + ring segments)
```

---

## 15. Event Policy

Use events to reduce coupling, but keep them disciplined.

Good event examples: `RoundStarted`, `ShardGrabbed`, `ShardReleased`, `CorrectInsert`, `WrongInsert`, `ShardExpired`, `ScoreChanged`, `ComboChanged`, `ComboMilestone`, `HeatChanged`, `StabilizationChanged`, `RoundWon`, `RoundOverloaded`, `RoundTimedOut`, `TrackingLost`, `TrackingRecovered`.

Rules: events are typed (avoid string names); payloads are small immutable structs; subscribe on enable/start and unsubscribe on disable/dispose; no single giant global `EventBus`; feature-scoped channels are fine; for one-to-one relationships prefer explicit method calls.

Bad:

```text
GlobalEventBus.Publish("thing_happened", object[] data)
```

Good:

```csharp
public readonly struct CorrectInsertEvent
{
    public ShardColor Color { get; }
    public int Combo { get; }
    public int Stabilization { get; }
}
```

---

## 16. Gameplay Rule Ownership

Each rule has one owner.

| Rule | Owner |
|---|---|
| Round duration | `RoundConfig` read by `RoundTimer` |
| Timer ticking and pause | `RoundTimer` / `RoundController` |
| Correct vs wrong insert (color match) | `PortValidationService` / `RoundController` |
| Score formula | `ScoreService` |
| Combo formula and reset | `ComboTracker` (or `ScoreService`) |
| Heat (gain, cap, overload, combo relief) | `HeatService` |
| Stabilization progress (victory at 20) | `StabilizationProgress` |
| Shard count, color distribution, respawn cadence | `ShardSpawnPlanner` |
| Shard visuals/motion | `ShardView` / `ShardVisual` / `ShardMotion` |
| Core + ring visuals and states | `ReactorCoreView` / `CoreStatePresenter` |
| HUD display | `HUDPresenter` / `HUDView` |
| Audio/haptic choice | `AudioService` / `HapticService` + configs |

**Concurrent end-state priority (owned by `RoundController`):** if stabilization reaches 20, heat reaches 8, and/or the timer reaches 0 on the same frame, resolve in order **victory > overload > time-out**. (This makes the GDD's edge cases deterministic.)

Do not duplicate formulas across UI, gameplay, and the results screen. Results **reads final round state**, it does not recompute score/heat independently.

---

## 17. Testing Requirements

At minimum, write Edit Mode tests for pure gameplay logic. Test coverage is part of the Definition of Done, not an afterthought.

### Per-mechanic test gate (Definition of Done)

Every gameplay mechanic is built test-alongside and is **not "done" until all three hold**:

1. **Unit tests in the same change.** New/changed **pure rules** (score, combo, heat, stabilization, timer, target/spawn planning, validation) ship with Edit Mode tests in the same commit. A mechanic that is *only* a thin XR/UI adapter with no pure-rule logic has no unit tests by design (see Testability rules) and is covered by the smoke/device pass instead — but any testable rule it introduces must be tested.
2. **Smoke test before completion.** Before calling a mechanic done, run a smoke pass as close to real human use as practical: a Play Mode smoke test and/or an **MCP-driven** editor check (enter Play, drive the flow, read the Console). Use the **XR Device Simulator** (XRI Samples — keyboard/mouse drives the HMD + controllers, no extra runtime) for quick in-editor interaction checks, and the **Meta XR Simulator** (an OpenXR runtime that emulates a Quest at the API level; needs no extra Unity packages when the project is on OpenXR) for Quest-API-level checks and automation. Do a **real Quest** pass when the mechanic depends on real tracking, controller 6DoF, or grab/socket feel. Use MCP where it can realistically drive the flow; otherwise a manual pass.
3. **No regression.** Re-run the **full existing** Edit Mode suite plus the relevant Play Mode / smoke checks and confirm earlier mechanics still pass and behave correctly. A change that breaks an earlier test is not done — fix it or revert before moving on.

### Required Edit Mode tests (pure rules)

- `ScoreService`: +10 per correct; +50 at every 5-combo milestone; victory time bonus = remaining seconds × 2; results heat penalty = heat × 10, never below 0.
- `ComboTracker`: +1 on correct; resets to 0 on wrong insert; resets on expire **only if the shard was held**; milestone every 5.
- `HeatService`: +1 on wrong insert; +1 on expired shard; −1 at each combo milestone if heat > 0; overload exactly at `heatCap` (8).
- `StabilizationProgress`: +1 per correct; victory triggers exactly at `stabilizationRequirement` (20).
- `RoundTimer`: ticks down; pauses/resumes without losing time incorrectly; time-out at 0.
- `ShardSpawnPlanner`: maintains the active target count (4 by default); never > 3 shards of one color at once; always ≥ 2 colors active; prefers the under-represented color; respawn delay within 0.3–0.8 s after an accept/expire; never returns spawn positions behind the player, outside the −50…+50° arc, or outside the height band; one shard per pad (a reserved pad does not double-spawn).
- `PortValidationService`: a color match yields a correct insert; a mismatch yields a wrong insert (counted only when the shard is clearly inside a wrong port); an empty-space drop has no penalty.
- Concurrent end-state: `RoundController` resolves victory > overload > time-out.
- Star rating: 0 (0–5 or overload before 6), 1 (6–11), 2 (12–19), 3 (stabilized = 20).

### Play Mode / smoke tests (XR Simulation)

- App transitions MainMenu → Calibration → Playing with a mocked rig.
- Grabbing a shard and socketing it into the matching port raises a correct-insert event and a beam plays.
- A wrong insert is rejected and the shard returns to its pad.
- `TrackingLost` pauses the timer (if implemented).
- Restart / Play Again clears shards and resets score, timer, heat, combo, and progress on the same scene.
- Results receives victory / overload / time-out data.

### Testability rules

- Pure rule classes do not inherit `MonoBehaviour`.
- Pure rule classes do not depend on `Time.time`, `Random.Range`, or Unity/XR scene objects directly.
- Wrap randomness behind an interface or pass a seeded random provider (especially `ShardSpawnPlanner`).
- If a class cannot be tested without a headset, it must be a **thin adapter**, not a rule owner.
- Grab/socket reliability and comfort cannot be unit-tested — they require the XR Simulator smoke pass and a Quest device pass.

---

## 18. Performance Requirements

Target: stable VR frame rate over visual extravagance.

Runtime rules:

- **72 FPS floor on Quest 2** (the baseline). Quest 3 may target 90 FPS only if profiling shows headroom; mechanics stay identical.
- No per-frame scene-wide searches.
- No avoidable GC allocations during the active round (aim near zero per frame).
- No repeated instantiate/destroy cycle for shards/beams.
- No expensive physics for shards (kinematic/scripted movement).
- Use simple colliders / socket volumes for ports.
- Keep active shards at 4 by default, never above 6.
- Keep particle bursts short (about 0.5–1.0 s) and limited.
- Single Pass Instanced; avoid transparent overdraw across the HMD view.
- Budget draw calls aggressively (Quest is mobile-class): share meshes/materials, GPU-instance repeats, keep unique materials low.
- **Fixed Foveated Rendering** (and optionally dynamic resolution) are the levers when GPU-fill-bound — likely with fake-glow transparency.
- Fake glow (emissive + halo billboards) instead of URP bloom for the MVP look.
- Avoid real-time shadows on Quest 2 unless profiling says they are safe.
- Modest material/texture sizes; downscale large textures.

Profiling expectations: use the Unity Profiler and on-device metrics (OVR Metrics Tool / Meta performance tooling) before declaring performance "done"; check GC during a round; check frame time during grab/insert bursts (the busy moments); if performance is poor, reduce particles, shadows, transparent overdraw, and active shards **before changing game rules**. Profile on Quest 2 if available; if only Quest 3 is available, keep extra headroom and assume Quest 2 is less forgiving.

---

## 19. Error Handling and Edge Cases

The app must not get stuck in a broken state.

Handle: headset/controller tracking lost during a round; headset removed / app paused mid-round; player grabs two shards (one per hand); shard released in empty space; shard expires while held; a socket receives an already-accepted shard; pool object returned twice; player pauses mid-grab; recenter mid-round; player presses Pause/Main Menu during a round.

Rules: invalid player actions are ignored safely or produce soft feedback; do not throw exceptions for normal user behavior; do not swallow exceptions silently during development; use clear debug logs for unexpected states but remove noisy logs from demo builds. Brief tracking loss must never produce heat or wrong-insert penalties on its own.

---

## 20. Architecture Decision Gate

Before writing production code, the implementation chat must make a short architecture decision note (a section in its first response, a small `ARCHITECTURE.md`, or a tracked task note).

It must answer:

- Which XR stack and Unity version (expected: OpenXR + XRI on URP, Unity 6.3) and why.
- How ports validate inserts: socket/trigger **accepts the shard, then validates color in code** (not a color-gated socket filter — see §6), and how correct/wrong outcomes are produced.
- Scene structure: one scene, additive, or another approach.
- App/round state machine approach.
- Dependency approach: manual DI/composition root (default), VContainer, or another — and why a container is or is not used.
- How runtime-created shards/VFX are created, initialized, pooled, and reset.
- Which gameplay rules will be pure C# covered by Edit Mode tests, and which parts stay thin XR/UI adapters with little/no unit coverage.
- Which guardrail deviations, if any, are intentional.

It must explicitly reject at least: one `GameManager` owning XR, UI, score, heat, audio, spawning, persistence; global singletons for normal gameplay services; service-locator access from gameplay; per-frame `GameObject.Find` / tag / scene-wide searches; UI as the source of truth for score/timer/heat/progress; XR/interaction code owning gameplay rules; **ballistic throwing to insert shards**.

This gate forces a deliberate choice before generating code; it is not a long essay.

---

## 21. LLM Implementation Protocol

Before coding: read `docs/product/game-design.md` (the GDD) and this document and the setup plan; confirm the chosen XR stack and Unity version; choose manual DI or a specific container and explain why; list the planned systems and files; state intentional deviations.

During coding: build **one vertical slice first** — menu → calibration → start round → grab a primitive shard → socket it into a matching primitive port → correct/wrong/expired feedback → results — before importing art. Keep classes small; move formulas and state transitions out of MonoBehaviours when practical; write unit tests **alongside each mechanic** (same change), not afterward; do not postpone all architecture until after the prototype works.

Before handoff: run the **full** Edit Mode suite (confirm no regressions); do a **smoke pass in human-realistic conditions** (MCP-driven Play Mode and/or XR Simulator, plus a Quest device pass where tracking/controllers/grab matter); run the restricted-API search; review every singleton/static mutable field; verify no one-file GodObject; verify the game can complete one victory, one overload, and one time-out path; verify tracking-loss pauses gameplay if implemented; verify no ballistic throwing is enabled.

---

## 22. Code Review Checklist

A change is not acceptable if any answer is "no" without written justification.

Architecture:

- Does each class have a clear single responsibility?
- Can gameplay rules be understood without reading UI and XR code?
- Can UI be changed without rewriting scoring/heat?
- Can the XR rig / interaction setup change without rewriting round rules?
- Is runtime state reset cleanly on replay/restart?

Dependencies:

- Are dependencies assigned explicitly?
- Is there a clear composition root or DI scope?
- Are gameplay dependencies visible through constructors or explicit initialization?
- If a container is used, is it absent from gameplay classes except the composition layer?
- Are there no new global singletons?
- Are events typed and unsubscribed?
- Are ScriptableObjects used as config/event channels rather than hidden mutable globals?

Performance:

- Are repeated objects (shards, beams, VFX) pooled?
- Are scene searches absent from gameplay hot paths?
- Are per-frame allocations avoided in the active round?
- Is the XR camera cached (no `Camera.main` per frame)?

Testing:

- Are score/combo/heat/stabilization/timer/spawn/validation rules covered by Edit Mode tests?
- Does each new/changed mechanic ship with unit tests in the **same change**?
- Was the **full existing suite re-run with no regressions**?
- Was the mechanic **smoke-tested in human-realistic conditions** (MCP / XR Simulator / Quest)?
- Can a non-XR mock path drive the round logic?
- Did the implementer test at least one victory, one overload, and one time-out path?

Readability:

- Are names specific?
- Are methods short enough to understand?
- Are comments used only where they clarify non-obvious intent?
- Is there no script mixing UI, XR, score, heat, spawn, audio, and persistence?

---

## 23. Red Flags That Require Refactoring

Refactor immediately if you see:

- A class over roughly 300 lines in MVP code, unless generated or data-only.
- A method over roughly 50 lines with mixed responsibilities.
- `GameManager.Instance` used from many scripts.
- UI text updated from gameplay objects directly.
- Shard scripts changing score/heat by themselves.
- Port/socket code owning round rules, or XR rig code spawning gameplay shards directly.
- `AudioClip` / haptic references scattered across unrelated scripts.
- Copy-pasted scoring/heat formulas.
- `Update` methods that only poll rare conditions.
- Scene object names used as identifiers.
- String event names.
- Persistent state hidden in static fields.
- Testable logic trapped inside MonoBehaviours.
- Coroutines that continue after the state that started them has ended.
- Ballistic throwing enabled on shards, or physics used for shard insertion.
- `Camera.main` or scene searches in per-frame code.

---

## 24. Acceptable Simplicity

Good architecture here is not maximal architecture.

Acceptable: a simple `AppStateMachine` with small state classes; a few ScriptableObject configs; plain C# services for score, combo, heat, timer, validation, and spawn planning; manual DI through a composition root plus serialized Unity/XR references; one scene if that is easier; primitive placeholder visuals during early vertical-slice work; the XRI Starter Assets rig.

Overkill for MVP: full ECS/DOTS; networking; Addressables unless asset loading genuinely needs it; a heavy/unfamiliar DI framework added for fashion; a generic global event bus; complex save architecture; a plugin system; procedural level generation; hand tracking; passthrough; locomotion.

The ideal result is boring in the best way: clear, explicit, easy to debug, and hard to accidentally turn into spaghetti.

---

## 25. Final Implementation Contract

The Starforge Relay implementation should be accepted only when:

- The game design in `docs/product/game-design.md` is playable.
- The architecture is split into XR rig/interaction, gameplay, UI, audio, persistence, and infrastructure boundaries.
- No MVP gameplay depends on global singletons.
- Dependencies are wired through a clear composition root, manual DI, or one approved DI container.
- No gameplay class resolves dependencies from a global container/service locator.
- No runtime gameplay hot path uses scene-wide object searches or `Camera.main` per frame.
- Repeated shards/beams/VFX are pooled or otherwise proven harmless.
- Score, combo, heat, stabilization, timer, validation, and spawn planning are testable outside a headset.
- App and round states are explicit.
- XR/interaction code is isolated from gameplay rules.
- UI is presentation code, not gameplay authority.
- No shard insertion relies on ballistic throwing.
- The code review checklist is satisfied.

If a future chat wants to break these rules, it must explain why the rule is wrong for this project, what risk is being accepted, and how the risk will be tested.
