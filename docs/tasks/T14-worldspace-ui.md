# T14 — World-space UI views + presenters (ray + Trigger)

| | |
|---|---|
| Branch | `feature/worldspace-ui` |
| Milestone | M3 — wiring |
| Design ref | GDD §15 (user flow), §16 (screens / HUD / pause / results), §11 (Trigger = UI select), §27 (large readable world-space UI); guardrails §6 (UI), §14 (UI architecture), §13 (XR UI: Ray Interactor + XR UI Input Module, Trigger); [ADR 0001](../architecture/adr/0001-tech-baseline.md) |
| Depends on | T13 (AppStateMachine: triggers + `PhaseChanged` + `LastResult`) |
| Touches scenes/prefabs | **yes** — new world-space canvases + `EventSystem`/XR UI Input Module; rig interaction-layer tweak ("Shard"); new UI prefabs; edits the scene |
| Status | 🟡 in progress |

## Goal
Turn the debug-key-driven flow (T13) into an **in-VR playable demo**: world-space UI for
MainMenu → Calibration → Playing (HUD) ⇄ Pause → Results, driven by the controller **ray + Trigger** and
wired to the `AppStateMachine`; the HUD shows the live round meters. Remove the temporary debug keys and the
`[Round]` logs. After T14 the player goes from launch to gameplay and back **without developer help** (the
GDD §25 demo bar, minus final art/audio).

## Decisions (resolved at authoring)
1. **Scope = the core flow UI + HUD** (MainMenu / Calibration / Pause / Results + HUD), ray+Trigger driven,
   wired to the machine. **Deferred:** Settings view + persistence → **T15**; HowTo + Credits → later (stub
   or T18); Splash → cut (GDD §22); Recenter (Calibration button) → later if easy; TrackingLost → still
   deferred; UI **audio** → T16; final **glow/art** on UI → T18–19 (T14 = functional primitive UI, readable
   per GDD §27). *(Open fork: if the build balloons, split HUD → `T14b`. Recommend keeping it together — the
   demo needs the whole flow and shares one XR-UI pipeline.)*
2. **MVP-style split (guardrails §14):** dumb **Views** (serialized refs + UI events + setter methods) +
   **Presenters** (subscribe to the machine / round, format, call view methods). Views own no gameplay/XRI
   logic; presenters know no XRI internals.
3. **UI tech = uGUI (world-space `Canvas`), NOT UI Toolkit.** XRI's UI interaction (XR UI Input Module +
   `TrackedDeviceGraphicRaycaster`) — the controller far-ray + Trigger "laser-pointer" click — is built for
   uGUI Canvas; it's the documented, sample-backed path (guardrails §13, ADR 0001). UI Toolkit world-space
   runtime panels are experimental in Unity 6.3 and have **no mature XR-ray interaction** — wrong tool for a
   world-space VR menu. (UITK would win for 2D/screen-space or editor tooling — not this.)
4. **One world-space Canvas per screen** (MainMenu / Calibration / Pause / Results) on the reactor-console
   area + a **HUD** canvas; a presenter shows the matching canvas on `PhaseChanged` and hides the others.
   *(Alternative: a single canvas with sub-panels — per-screen canvases are cleaner for show/hide; flag if
   you prefer one.)*
5. **Pause entry:** bind the controller **Menu button** (XRI action) to `RequestPause` (GDD §11 "Pause if
   easy to bind"); fall back to an in-world Pause button on the console if the action binding is fiddly.
6. **Extract a thin `AppFlowController` (MonoBehaviour) — resolves the T13 altitude carry-forward.** The
   `AppStateMachine` tick + the screen show/hide + button→trigger wiring move **out of the composition root**
   into an `AppFlowController` (a scene object the root injects the machine into via `Initialize`, like the
   other adapters). The root returns to **construction-only** (guardrails §6 — wiring, not coordination / no
   `Update`); `AppFlowController` owns the machine, ticks it on `unscaledDeltaTime`, and connects the views to
   the triggers + `PhaseChanged` + `LastResult`. T13 flagged this as the T14 step. (`HUDPresenter` /
   `ResultsPresenter` may be plain classes it drives, or sibling components.)

## Acceptance criteria

### XR UI input pipeline (foundational — nothing exists yet: scene has 0 Canvas / 0 EventSystem)
- An `EventSystem` with **`XRUIInputModule`** (`UnityEngine.XR.Interaction.Toolkit.UI`) + a
  **`TrackedDeviceGraphicRaycaster`** on each world-space Canvas, so the controller's **far ray** selects
  uGUI on **Trigger** (guardrails §13). **Confirmed against installed XRI 3.3.0 (reflection):** the rig's
  **`NearFarInteractor`** already implements **`IUIInteractor`** — enable its **`enableUIInteraction`** and
  bind **`uiPressInput`** to the Trigger action; **no separate Ray Interactor is needed**. (Re-verify the
  exact serialized field/action wiring in-editor at implementation.)
- **Grab-vs-UI separation:** the Near-Far drives UI on its **far** region and grabs shards on its **near**
  region, and exposes **`blockUIOnInteractableSelection`** (suppresses UI while holding a shard). Rely on the
  region / UI-block config first. Add a dedicated **"Shard" interaction layer** (+ matching rig near-grab
  mask; update `Shard.prefab` + the 3 `PortSocket`s **together**, or grab breaks — the T08 note) **only if
  the smoke shows the far ray grabbing shards or a UI↔grab conflict** — the T09/T10 deferral, pulled in only
  if actually needed (don't re-layer speculatively).

### Views (`StarforgeRelay.UI`, world-space, primitive but readable — GDD §27)
- `MainMenuView` — **Play** (How To / Settings / Credits placeholders hidden or inert in T14).
- `CalibrationView` — **Start**, **Back** ("Stand comfortably and face forward" copy, GDD §16/§28; Recenter deferred).
- `HUDView` (diegetic) — timer, score, combo, reactor charge (stabilization `N/20`), heat.
- `PauseMenuView` — **Resume**, **Restart**, **Main Menu**.
- `ResultsView` — result title (**Core Stable!** / **Partial Relay** / **Overloaded**), stars, correct count,
  final heat, score, **Play Again**, **Main Menu**.
- Each view exposes UI events (`PlayClicked`, …) + setter methods (`SetTimer`, `SetScore`, …); **no** gameplay
  decisions, **no** XRI references.

### Presenters + wiring to the machine (T13)
- The **`AppFlowController`** (owns + ticks the machine, decision 6) subscribes `AppStateMachine.PhaseChanged`
  → shows the matching view, hides the rest; routes view button events → machine triggers:
  - MainMenu **Play** → `RequestCalibration`; Calibration **Start** → `RequestStartRound`; Calibration
    **Back** → `RequestMainMenu`; Pause **Resume** → `RequestResume`; Pause **Restart** → `RequestStartRound`;
    Pause/Results **Main Menu** → `RequestMainMenu`; Results **Play Again** → `RequestStartRound`; **Menu
    button** → `RequestPause`.
- **`HUDPresenter`** formats + updates the HUD from the round state: timer as `mm:ss` (update only when the
  shown second changes — no per-frame text churn, guardrails §11/§14), score, combo, charge `N/20` (ring
  segments), heat bar.
- **`ResultsPresenter`** — on entering Results, reads `AppStateMachine.LastResult` (the snapshot) → fills
  `ResultsView` (title from `Result`, stars, score, stabilization, heat). Reads, never recomputes (guardrails §16).

### How the HUD reaches round state (design point — keep XRI-free)
- `RoundLoopController` already re-raises `RoundEnded`; **extend it to surface the live round to the HUD** —
  re-raise `CorrectInserted` / `WrongInserted` / `ShardExpired` and expose the meters (`Score` / `Combo` /
  `Heat` / `Stabilization` / `TimeRemaining`), or hand the `HUDPresenter` the `RoundController` on
  `StartRound` via a small event. The `[Round]` `Debug.Log`s are **replaced** by the HUD consuming these.

### Remove the T13 scaffolding
- Remove the temporary Input-System **debug keys** (Space/P/R/M) from `StarforgeRelayCompositionRoot`.
- Remove the dev **`[Round]` `Debug.Log`s** from `RoundLoopController` (the HUD now shows this).

### Behaviour (smoke)
- Launch → **MainMenu** visible; ray+Trigger clicks **Play** → Calibration → **Start** → Playing; **HUD**
  shows the timer counting down + score/combo/charge/heat.
- Correct/wrong insert → HUD updates live; **Menu button** → Pause menu, round frozen (timeScale, T13);
  **Resume** → continues; **Restart** → fresh round.
- Reach Won / Overloaded / TimedOut → **Results** shows the snapshot; **Play Again** → fresh round; **Main
  Menu** → menu. No debug keys; no `[Round]` console logs.

## Implementation notes
- **New:** `Scripts/UI/{MainMenuView,CalibrationView,HUDView,PauseMenuView,ResultsView}.cs` +
  `Scripts/App/AppFlowController.cs` (owns + ticks the machine + flow presentation, decision 6) +
  `Scripts/UI/{HUDPresenter,ResultsPresenter}.cs`; `Prefabs/UI/*`; scene canvases + an `EventSystem`
  (`XRUIInputModule`) + a per-canvas `TrackedDeviceGraphicRaycaster`. **Edit:** `StarforgeRelayCompositionRoot`
  (drop the debug keys **and** the machine tick → into `AppFlowController`; construct + inject it; the root
  becomes construction-only), `RoundLoopController` (drop `[Round]` logs; surface round events/meters to the
  HUD), the **scene** (UI objects; + the optional "Shard" layer on `Shard.prefab`/`PortSocket`s/rig only if
  the smoke needs it; saved).
- **Namespace** `StarforgeRelay.UI` (guardrails §5). The Runtime asmdef may need **TextMeshPro** /
  **Unity.ugui** references for `Canvas`/`TMP_Text` — verify and add as needed (as `Unity.InputSystem` in T13).
- **Verify against installed XRI 3.3.0 / OpenXR 1.16** before relying on it: the XR UI Input Module type, how
  the Near-Far ray feeds UI, and interaction-layer masks. (The rig kept Near-Far + Poke in T07.)
- **MCP discipline (T08–T13):** `refresh scope=all` for new files; `manage_components` set + re-read for
  serialized refs (component arrays need `[{"instanceID":…}]`); never edit scripts in Play; smoke from a
  settled editor; the first Play after a recompile shows the benign domain-reload transient.
- **Style** (`csharp-style.md` + `.editorconfig`): C# 9 (block namespace, no `record`/`init`), no `#nullable
  enable`, `_camelCase` fields, `sealed`, XML docs; `dotnet format --verify-no-changes` on the touched files
  (IDE1006 clean — Unity does not surface it). Pre-existing `IDE0044` on `[SerializeField]` is a project-wide
  false positive (not introduced here).

## Out of scope
- **Settings + persistence** → T15. **HowTo / Credits** content → later (stub/inert in T14). **Recenter**,
  **TrackingLost** → later. **UI audio / haptics** → T16. **Final glow / art / shape markers** on UI/shards →
  T18–19. No gameplay-rule changes (HUD/Results **read** state; the rules are the tested T01–T06 core).

## Verification
- **Tests:** T14 is UI/adapter-heavy; any **pure formatting helper** (timer `mm:ss`, results title from
  `RoundPhase`, charge `N/20`) ships EditMode tests in-change. **Re-run the full suite** (96/96 → +N), no regression.
- **Restricted-API:** clean over `Assets/_Project`; XRI in C# **stays** confined to `PortSocket` +
  `ShardMotion` (T14's UI is uGUI + a scene-configured `XRUIInputModule` / `TrackedDeviceGraphicRaycaster`,
  not XRI C#); views / presenters / `AppFlowController` stay free of gameplay rules.
- **Smoke (XR Device Simulator / Quest):** the full in-VR flow above via ray+Trigger; all three end states →
  Results; **no debug keys, no `[Round]` logs**; console clean (sim-haptic excepted; re-check on device T20).
- **Done =** full EditMode suite green this session + restricted-API clean + `dotnet format` clean on touched
  files + the in-VR flow smoke + no Console errors; then close docs (brief + matrix + current-status).

## What was actually done
—
