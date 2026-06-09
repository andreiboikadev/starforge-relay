# T15 — Settings (sound/haptics) + persistence

| | |
|---|---|
| Branch | `feature/settings-persistence` |
| Milestone | M3 — wiring |
| Design ref | GDD §16 (Settings screen; Pause "Settings"), §15 (user flow), §27 (sound/haptics off — accessibility), §29 (persistence: sound/haptics, best score if trivial), §13/§16 (best score on Results); guardrails §6 (Persistence — wrap `PlayerPrefs` behind an interface; UI views+presenters), §8 (manual DI), §14 (UI MVP split), §16 (Results reads, never recomputes), §17 (per-mechanic test gate); [ADR 0001](../architecture/adr/0001-tech-baseline.md) (manual DI, single scene) |
| Depends on | T14 (XR-UI pipeline + `AppFlowController` + the 5 screens) |
| Touches scenes/prefabs | **yes** — new world-space **Settings** canvas (2 toggles + Back, own `TrackedDeviceGraphicRaycaster`) + a **Settings** button on the MainMenu & Pause canvases; *(best score)* a Best readout on the Results canvas; new serialized refs on the composition root. |
| Status | 🟡 in progress |

## Goal
Give the player the GDD's required **Settings** (Sound on/off, Haptics on/off) on a world-space screen
reachable from **MainMenu** and **Pause**, backed by a small **`PlayerPrefs`** persistence layer that
**loads at boot** and survives relaunch. This completes the M3 wiring screen set and stands up the
persistence seam that **T16** (AudioService/HapticService) and the Results **best score** read from.

## Decisions (resolved at authoring — validate before coding)
1. **Scope = the GDD-*required* settings + the persistence foundation.** Sound on/off + Haptics on/off
   (GDD §16 "Required"), persisted via `PlayerPrefs`, loaded at boot, exposed as a runtime contract.
   **Deferred (GDD §16 "Optional if trivial"):** Brightness, Color-assist (shape markers = stretch §30);
   **Player height** stays out — standing-only MVP (GDD §34.4). HowTo/Credits, Recenter, TrackingLost → later.
2. **Settings is an OVERLAY, not an `AppPhase` (recommended).** Keep the tested `AppStateMachine`
   (`AppStateMachineTests`, 11 cases) **untouched** — GDD §23's phase list has no Settings state.
   `AppFlowController` shows a Settings canvas **over** the current phase's view and restores on Back via
   `OnPhaseChanged(_machine.Phase)` — so it is reachable from **MainMenu** *and* **Paused**, and Back
   returns to the caller automatically because the phase never changed while Settings was open.
   *(Alternative, rejected for MVP: a `Settings` `AppPhase` + `SettingsState` + a "return-to" memory — adds
   transitions/tests to the tested machine and contradicts GDD §23's flat list. Flag if you prefer it.)*
3. **No consumer in T15 — this is a contract, not an effect.** There is **no AudioService/HapticService
   yet** (that is **T16**). Toggling Sound/Haptics here **persists** and updates the runtime
   `SettingsService`, but **nothing becomes audible/silent or buzzes/stops** in T15 — there is no audio or
   haptics to gate. T16's services read `SettingsService.Current` + subscribe to `Changed`. *State this in
   the smoke notes so "the toggle changes nothing audible" is not mistaken for a bug.*
4. **Persistence = `ISettingsStore` wrapping `PlayerPrefs` (guardrails §6/§8/§10).** A single
   `PlayerPrefsSettingsStore` is the **only** type that touches `PlayerPrefs`; a plain-C# `SettingsService`
   holds `Current` + a `Changed` event and is **headless-unit-testable** through a fake store.
5. **Best score — recommended INCLUDE, fenced/cuttable.** The persistence layer is being built now and GDD
   §16 Results lists "Best score if available" (today `ResultsView` has no slot). Add `IBestScoreStore` on
   the same seam; `ResultsPresenter` persists `max(stored, LastResult.Score)` on entering Results and the
   view shows it. It is a small, self-contained block — **cut it at validation** if you'd rather keep T15
   to settings only (then best score becomes its own follow-up).
6. **Mirror T14's MVP split + manual DI.** Dumb `SettingsView` (toggles + Back: events + setters, no logic)
   + `SettingsPresenter` (binds view ↔ `SettingsService`). The composition root constructs the store +
   service, **loads at boot** (`Awake`, before `machine.Begin()` in `Start`), and injects via `Initialize`.

## Acceptance criteria

### Persistence (`StarforgeRelay.Persistence` — new namespace; plain C# + one thin `PlayerPrefs` adapter)
- **`GameSettings`** — immutable `readonly struct { bool SoundEnabled; bool HapticsEnabled; }` with a static
  **`Default`** = both **true** (GDD: sound/haptics on by default). C# 9 — `readonly struct` + ctor, **no**
  `record`/`init` (style overlay).
- **`ISettingsStore`** — `GameSettings Load();` `void Save(GameSettings settings);`
- **`PlayerPrefsSettingsStore : ISettingsStore`** — the **only** `PlayerPrefs` toucher; keys (e.g.
  `"settings.sound"`, `"settings.haptics"`, int `0/1`) **default to ON when absent**.
- **`SettingsService`** (plain C#) — built over an `ISettingsStore`; exposes `GameSettings Current`,
  `event Action<GameSettings> Changed`, `SetSoundEnabled(bool)` / `SetHapticsEnabled(bool)` which update
  `Current`, **persist via the store**, and raise `Changed` **only on an actual change** (idempotent set =
  no Save, no event).

### Settings screen (`StarforgeRelay.UI`, world-space, ray + Trigger)
- **`SettingsView`** (dumb) — uGUI **`Toggle` ×2** (Sound, Haptics) + a **Back** `Button`; exposes
  `SoundToggled(bool)`, `HapticsToggled(bool)`, `BackClicked`; setters `SetSound(bool)` / `SetHaptics(bool)`
  initialize the toggles **without** re-raising their change events — use uGUI **`Toggle.SetIsOnWithoutNotify`**,
  not a hand-rolled suppression flag (else the init value feeds straight back through the change handler).
  No gameplay/XRI references.
- **`SettingsPresenter`** — on init, set both toggles from `SettingsService.Current`; on a toggle, call the
  matching `SettingsService.Set…`. Presentation only (no recompute).
- **`MainMenuView`** gains `_settingsButton` + `SettingsClicked`; **`PauseMenuView`** gains `_settingsButton`
  + `SettingsClicked`.

### Flow wiring (overlay — `AppFlowController`, decision 2)
- MainMenu/Pause `SettingsClicked` → `OpenSettings()` (hide the phase views, show the Settings canvas);
  Settings `BackClicked` → `CloseSettings()` (hide Settings, then `OnPhaseChanged(_machine.Phase)` to restore
  the caller's view). Subscribed/unsubscribed symmetric with the existing handlers; `AppStateMachine` unchanged.
- The Settings canvas is driven **only** by `OpenSettings`/`CloseSettings` — **do not** add `_settings` to
  `OnPhaseChanged`'s `SetActive` list (it would fight the overlay and hide it instantly — a confusing bug).
  `OnPhaseChanged` keeps managing only the 5 phase views.
- **Restore invariant** `CloseSettings` relies on: Settings opens **only** from MainMenu/Paused, and no
  `PhaseChanged` can fire while it is open (no round-end in a menu or while the round is frozen; `Tick`
  advances only Boot/RoundComplete). So `_machine.Phase` is still the caller when Back restores it — keep
  `OpenSettings` reachable from those two screens only.

### Boot load (GDD §23 "Boot: load settings")
- `StarforgeRelayCompositionRoot` constructs `PlayerPrefsSettingsStore` + `SettingsService` (load happens in
  `Awake`, before `AppFlowController.Start` calls `machine.Begin()`), and injects the service into
  `SettingsPresenter` via `Initialize`.

### Best score *(recommended; cut as a block at validation — decision 5)*
- **`PlayerPrefsBestScoreStore`** — `int Load()` (default 0) + `void Save(int score)`. **Concrete, no
  interface:** the only logic is a `Math.Max` and the store is a thin `PlayerPrefs` adapter (smoke-covered),
  so an `IBestScoreStore` is **not earned** under the "abstraction only on its second use" rule. *(Contrast
  `ISettingsStore`, which is earned — the `FakeSettingsStore` is a real second impl the `SettingsService`
  unit tests need to stay headless.)*
- `ResultsPresenter`: on entering Results, `best = Math.Max(store.Load(), LastResult.Score)`; `Save(best)`
  only if it rose; pass `best` to the view. Reads `LastResult`, never recomputes the round (guardrails §16).
- *Note (separation):* this puts a small **persistence write into a "presentation only" class.** Accepted
  for MVP at the one place that knows "a round just finished"; **widen `ResultsPresenter`'s doc comment** to
  own it, or (cleaner, optional) a 1-class `BestScoreTracker` subscribing to round-end. **Do not** push the
  write into the construction-only composition root.
- `ResultsView.SetResults(…)` gains a `bestScore` parameter + a `_bestScoreText` ("Best {n}") readout (GDD §16).

### EditMode tests (pure rules, **same change** — the gate, guardrails §17)
- `GameSettings.Default` → both `true`.
- `SettingsService` over a **`FakeSettingsStore`**: `Current` equals the loaded value; `SetSoundEnabled(false)`
  → `Current.SoundEnabled` false **and** the store saved **and** `Changed` raised once; setting the same value
  again → **no** Save, **no** event; Haptics symmetric.
- *(best score)* the update keeps the **max** — a higher score persists, an equal/lower one does not change
  the stored value.
- **Full EditMode suite re-runs green (96 → 96 + N), no regression.**

## Implementation notes
- **New:** `Scripts/Persistence/{GameSettings,ISettingsStore,PlayerPrefsSettingsStore,SettingsService}.cs`
  *(+ best score: `PlayerPrefsBestScoreStore.cs`)*; `Scripts/UI/{SettingsView,SettingsPresenter}.cs`;
  `Tests/EditMode/{SettingsServiceTests,FakeSettingsStore}.cs`.
- **Edit:** `MainMenuView` + `PauseMenuView` (add Settings button + event), `AppFlowController` (add
  `_settings` ref + Open/Close + the 3 new subscriptions), `StarforgeRelayCompositionRoot` (construct +
  load + inject store/service/presenter) *(+ best score: `ResultsView` + `ResultsPresenter` + the store ref)*.
- **Scene:** a new world-space **Settings** canvas (2 `Toggle`s + Back) with its **own**
  `TrackedDeviceGraphicRaycaster` (the per-canvas T14 pattern); a **Settings** button on the MainMenu &
  Pause canvases; *(best score)* a Best readout on Results; wire every new serialized ref + the
  composition root's new fields; save the scene.
- **Namespaces** `StarforgeRelay.Persistence` (new) + `StarforgeRelay.UI`; all in the **existing**
  `StarforgeRelay.Runtime` asmdef (no new asmdef — earn it on a second use). Tests in the existing
  `StarforgeRelay.Tests.EditMode` asmdef (model the test double on `Tests/EditMode/FakeRandom.cs`).
- **XR-UI:** a uGUI `Toggle` is ray+Trigger-selectable exactly like a `Button` on the T14 pipeline
  (`XRUIInputModule` + per-canvas `TrackedDeviceGraphicRaycaster`, Near-Far far-ray + Trigger). **Verify the
  `Toggle` ray-interaction in the XR Device Simulator** at implementation.
- **MCP discipline (T08–T14):** `refresh scope=all` for new files; `manage_components` set + **re-read** for
  serialized refs (component arrays need `[{"instanceID":…}]`); never edit scripts while in Play; smoke from a
  **settled** editor; the first Play after a recompile shows the benign domain-reload transient.
- **Style** (`csharp-style.md` + `.editorconfig`): C# 9 (block namespace, **no** `record`/`init`), no
  `#nullable enable`, `_camelCase` fields, `sealed`, XML docs; `dotnet format --verify-no-changes` clean on
  touched files (IDE1006 clean — Unity doesn't surface it; the pre-existing `IDE0044` on `[SerializeField]`
  is the project-wide false positive, not introduced here).
- **Restricted-API:** clean over `Assets/_Project`; `PlayerPrefs` is **allowed** wrapped at the store
  boundary (guardrails §8/§10). XRI in C# **stays** confined to `PortSocket` + `ShardMotion` (Settings UI is
  uGUI + the scene `XRUIInputModule`, not XRI C#).

## Out of scope
- The **actual muting/disabling** of sound + haptics → **T16** (AudioService/HapticService read
  `SettingsService`). **No audio/VFX/haptic systems are created here.**
- GDD §16 **optional** settings: Brightness, Color-assist (shape markers = stretch §30); **Player height**
  (standing-only MVP, §34.4). **HowTo / Credits** screens, **Recenter**, **TrackingLost** → later.
- **No gameplay-rule changes** — settings never touch the tested T01–T06 core; Results still **reads**
  `LastResult` and never recomputes (guardrails §16). Per-result RoundComplete timing → T17.
- *(If best score is cut at validation)* best score → its own follow-up.

## Verification
- **Tests:** EditMode for `GameSettings` + `SettingsService` *(+ best-score max)* per the gate above
  (`FakeSettingsStore` keeps them headless). The `PlayerPrefs*Store`s are **thin adapters** — primarily
  smoke-covered; an optional EditMode round-trip test is fine **only with `PlayerPrefs.DeleteKey` cleanup in
  teardown**. Re-run the **full** suite (96 → +N), no regression.
- **Restricted-API** clean over `Assets/_Project`; `dotnet format` clean on touched files; XRI-in-C#
  confinement (`PortSocket` + `ShardMotion`) intact.
- **Smoke (XR Device Simulator / MCP, ray+Trigger):**
  - MainMenu → **Settings** → toggle Sound off + Haptics off → **Back** → MainMenu; re-open Settings → both
    toggles reflect the new state (in-session).
  - **Pause** → Settings → Back → **Pause** (the round stays frozen — `timeScale`/insert gate, T13).
  - **Persistence proof:** Sound off → **stop Play** → re-enter Play → Settings shows Sound **off** (read
    back from `PlayerPrefs`).
  - *(best score)* play a round → Results shows **Score + Best**; a lower replay leaves Best unchanged; a
    higher one updates it; relaunch persists it.
  - Console clean apart from the known benign sim-haptic errors (absent on device; re-check at T20). *(Note:
    no audible/tactile change yet — decision 3.)*
- **Done =** full EditMode suite green **this session** + restricted-API clean + `dotnet format` clean +
  the smoke above + no Console errors; then close docs — brief Status `✅` + **What was actually done**,
  matrix row (Status `▫→✅`, Brief `·→✓`, Scenes `minor→yes`), and `current-status.md`.

## What was actually done
— (filled on close: what shipped, any deviation, the commit/PR, the date.)
