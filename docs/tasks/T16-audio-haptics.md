# T16 — AudioService + HapticService + configs

| | |
|---|---|
| Branch | `feature/audio-haptics` |
| Milestone | M4 — feedback |
| Design ref | GDD §18 (audio direction + the required-sound list + volume guidance), §12 (correct insert: "short controller haptic pulse"; wrong insert: "no harsh sound"), §16 (Settings: Sound/Haptics on/off), §22 (cut line: basic sound feedback = must-keep; ambient loop = cut-first), §27 (accessibility: sound/haptics off must work); guardrails §6 (Audio/VFX/Haptics — semantic events, gameplay never references clips/haptics, respect settings centrally), §7 (`AudioCueConfig` / `HapticConfig` SOs), §8 (Unity statics wrapped at boundaries), §15 (typed events), §16 (audio/haptic choice owner = services + configs), §17 (per-mechanic gate); [ADR 0001](../architecture/adr/0001-tech-baseline.md) (manual DI) |
| Depends on | T11 (round-loop events), T13 (`RoundStarted` / gated lifecycle), T15 (`SettingsService.Current` + `Changed` — the consumer seam) |
| Touches scenes/prefabs | **minor** — one `Audio Source` object + one `Feedback` object + new serialized refs on the composition root; 2 new SO assets; imported audio clips. **No canvas/prefab/rig structural change** (the rig's existing haptic components are only toggled at runtime). *(Matrix said "no" — corrected at authoring.)* |
| Status | 🟡 in progress |

## Goal
The first **feedback layer**: every GDD-§18-required sound and the §12 correct-insert haptic pulse, driven by
the **existing semantic events** (no gameplay-rule change), with clips/strengths in **config SOs** and both
channels **actually gated** by the T15 settings — the first real consumer of `SettingsService.Current` +
`Changed`. After T16 the Settings toggles audibly/tactilely do something, closing the T15 carry-forward.

## Decisions (resolved at authoring — validate before coding)
1. **One shared cue vocabulary.** A `FeedbackCue` enum carries the 10 GDD-§18-required cues — `UiSelect`,
   `RoundStart`, `GrabShard`, `ReleaseShard`, `CorrectInsert`, `WrongInsert`, `ShardExpired`,
   `ComboMilestone`, `Victory`, `Overload` — plus `TimedOut` (the GDD does **not require** a time-out sound —
   §18's required list omits it — so the config entry may stay empty = silent no-op). Both configs key off the
   same enum; a missing entry/clip is a **silent skip**, so optional cues need no special casing.
2. **Sound on/off gate = `AudioListener.volume` (0/1), owned by `AudioService`.** One central gate
   (guardrails §6 "respect settings centrally") that also covers any future audio (ambient loop, XRI
   feedback components, T17 VFX stingers) without per-source checks. `AudioListener` is a Unity static
   wrapped at exactly one boundary — the audio service (guardrails §8 allowed exception). *Rejected:*
   checking `SoundEnabled` inside `Play()` only — leaves future sources ungated. **Forward note (single
   writer):** keep `AudioService` the *only* writer of `AudioListener.volume`. T21 fades/ducking or a
   Quest-3 audio profile must route through it (or a future `AudioMixer` master param), never write the
   listener directly — otherwise the on/off gate and a fade fight for the same value.
3. **Haptics via the rig's existing plumbing — and the setting must gate the built-ins too.** The scene rig
   already carries one **`HapticImpulsePlayer`** per controller (`XR Origin (XR Rig)/Camera Offset/Left|Right
   Controller`) and **4 `SimpleHapticFeedback`** components (XRI select/hover feel on the Near-Far + Poke
   interactors — the source of the known benign sim haptic-capability errors). `HapticService` sends
   gameplay-cue pulses through the two players — **to both controllers** (GDD §12 says "controller haptic
   pulse"; by insert time the inserting hand has already released, so hand attribution isn't tracked —
   MVP-simple) — and **enables/disables the 4 `SimpleHapticFeedback` components** from
   `HapticsEnabled` at init + on `Changed`, otherwise "Haptics Off" would still buzz on grab on a real
   device (GDD §16/§27). Verified against installed XRI 3.3.0:
   `HapticImpulsePlayer.SendHapticImpulse(float amplitude, float duration)` (+ `float frequency` overload),
   both non-obsolete.
4. **Grab/release cues come from `ShardSpawner` held-state transitions — no new XRI surface.** The spawner
   already reads each active shard's held state every frame (the T11b lifetime tick). Add
   `ShardMotion.IsHeldByHand` (selected **and** the selector is not a socket — one `XRSocketInteractor`
   type check; XRI stays confined to its designated adapters) and track the previous value per
   `ActiveShard`: **`GrabShard` on `IsHeldByHand` false→true; `ReleaseShard` on true→false only when
   `!motion.IsHeld`** — i.e. the hand let go into empty space, **not** a hand→socket handoff. A socket snap
   therefore fires **no grab cue** and **an insert fires no release cue** (the correct/wrong cues own that
   moment; this is the smoke criterion). Known 1-frame race: if the spawner samples in the gap between hand
   release and socket select, a release cue can slip through — accept it in the smoke if inaudible against
   the insert cue, else debounce the release by one tick. Spurious cues from consume/clear paths can't
   happen (`Despawn`/`ClearActive` remove the shard from `_shardPads` synchronously). *Rejected:* XRI
   `SimpleAudioFeedback` on the shard prefab (clips would live outside `AudioCueConfig` — two homes;
   per-shard `AudioSource`s for nothing); per-shard C# event subscriptions (the pooling-lifecycle churn
   `ShardMotion`'s poll-based design deliberately avoids).
5. **UI-select cue:** `AppFlowController` raises a semantic `UiSelected` event from each button handler (it
   already routes every screen button — one place); the Settings **toggles** map via
   `SettingsService.Changed` → `UiSelect` (each real change is exactly one click; a no-op re-set raises
   nothing — acceptable). *Rejected:* subscribing the feedback layer to every view's events (duplicates the
   flow wiring).
6. **Wiring home = a `FeedbackController` adapter** (mirrors `HUDPresenter`: `Initialize(...)` from the
   root, symmetric unsubscribe). It maps events → cues and calls the two services; gameplay code never
   references a clip, an `AudioSource`, or a haptic call (guardrails §6). **T17 seam:** VFX reacts to the
   **same** surfaced round/spawner events through its **own sibling controller** — `FeedbackController` stays
   audio+haptics-only; do **not** route VFX through `AudioService` or widen `FeedbackCue` into a VFX bus
   (guardrails §6 lists Audio/VFX/Haptics as peers reacting to the shared semantic events, not a chain).
7. **Clips = a hand-picked CC0 subset** of **Kenney Sci-fi Sounds** (gameplay) + **Kenney Interface Sounds**
   (UI) — the GDD §19 recommended MVP audio set — imported under `Assets/ThirdParty/`, ~10 files, not the
   full packs. The **asset ledger** rows flip PLANNED → IMPORTED in the same change (download date, local
   path, "subset (N of M files)", license re-verified at download — the ledger rule).
8. **Git LFS — first binary assets land here; recommend *defer* to T18 (human decides).** T16 is the first
   task to commit binary media, and the repo's `.gitattributes` explicitly parks the LFS migration for
   "when we import Kenney art (textures/audio)" because converting now is a **history rewrite + force-push +
   re-clone on the second machine**. Recommendation: **stay off LFS for T16** — ~10 short Kenney clips are a
   few hundred KB, far below the cost of a history rewrite — and do the deliberate `git lfs migrate` at
   **T18** when the bulk texture/mesh packs arrive (it sweeps the T16 clips in at the same time). The clips
   commit as plain `binary` (already configured), no EOL risk. **This is a git/commit decision (human-owned)
   — surface it at validation; do not silently assume either way.**
9. **No ADR.** No new package, no container, no hard-to-reverse choice (the listener gate and component
   toggling are reversible implementation details inside the sanctioned baseline). *(The LFS choice in
   decision 8, if it ever flips to "migrate now", would warrant its own note — but deferring needs none.)*

## Acceptance criteria

### Cues + configs (`StarforgeRelay.Audio` — new namespace, existing `StarforgeRelay.Runtime` asmdef)
- **`FeedbackCue`** enum — the 11 values from decision 1.
- **`AudioCueConfig`** SO (`ScriptableObjects/Config/AudioCueConfig.cs` + one asset, `[CreateAssetMenu]`,
  read-only at runtime): entries cue → `AudioClip` + volume `[0..1]`. Missing entry/clip = silent skip.
  Tooltips/defaults carry the design guidance: grab/release **short** (GDD §18 — they are frequent; "quiet"
  is our local default, not GDD text); correct satisfying, not loud (§18); wrong soft — "no harsh sound"
  (GDD **§12**).
- **`HapticConfig`** SO + asset: entries cue → amplitude `[0..1]` + duration s + frequency (0 = device
  default). Entries only where haptics make sense: `CorrectInsert` (GDD-required), optionally `WrongInsert`
  (soft), `ComboMilestone`, `Victory`, `Overload`. No entry = no haptic.

### `AudioService` (plain C#, `Scripts/Audio/` — an infra adapter like `ShardPool`: may touch `UnityEngine`, must not be a MonoBehaviour)
- Ctor `(AudioCueConfig, AudioSource, SettingsService)`. `Play(FeedbackCue)` → `PlayOneShot(clip, volume)`.
- Applies `AudioListener.volume = SoundEnabled ? 1 : 0` at construction **and** on `SettingsService.Changed`
  (decision 2). `IDisposable` — unsubscribes from `Changed`; the root disposes it (the `ShardPool` pattern).
- No singleton; no static state.

### `HapticService` (plain C#, `Scripts/Audio/`)
- Ctor `(HapticConfig, HapticImpulsePlayer left, HapticImpulsePlayer right, SimpleHapticFeedback[]
  rigSelectHaptics, SettingsService)`.
- `Play(FeedbackCue)` → if `HapticsEnabled` and an entry exists, `SendHapticImpulse(amplitude, duration[,
  frequency])` on **both** players (decision 3). No entry / disabled = no-op.
- Sets `enabled` on the 4 rig `SimpleHapticFeedback` components from `HapticsEnabled` at construction +
  on `Changed`. `IDisposable` (unsubscribe), disposed by the root. *§5 tension, accepted:* a `*Service`
  normally has "no scene ownership"; here it mutates injected scene components because the settings **must**
  gate the rig's built-in select-haptics centrally (guardrails §6 / GDD §16/§27) — the components arrive as
  serialized refs from the root (§8), never discovered, and the service stays the single haptics boundary.
- **XRI confinement grows by two files (both justified):** `HapticService` (haptics *is* an XRI API; the
  §6-designated boundary) and `StarforgeRelayCompositionRoot` (its new `HapticImpulsePlayer` /
  `SimpleHapticFeedback` **serialized fields** live in XRI namespaces — §8's sanctioned wiring method; the
  root makes no XRI calls and holds no logic). New expected grep set: {`PortSocket`, `ShardMotion`,
  **`HapticService`**, **`StarforgeRelayCompositionRoot`** (field declarations only)}; the root's "no XRI"
  doc comment is updated in the same change.

### Event sources (small edits, no rule changes)
- **`AppFlowController`** += `public event Action UiSelected;` raised in every button handler
  (`OnPlay`/`OnStartRound`/`OnResume`/`OnMainMenu`/`OnPause`/`OnOpenSettings`/`OnCloseSettings`).
- **`ShardMotion`** += `public bool IsHeldByHand` — `isSelected` and the current selector is **not** an
  `XRSocketInteractor` (verify the exact selector member — `interactorsSelecting` /
  `firstInteractorSelecting` — against installed XRI 3.3.0 via `unity_reflect` at impl). `IsHeld`
  (selected-by-anything) stays as-is for the lifetime slow (T11b semantics unchanged).
- **`ShardSpawner`** += a `WasHeldByHand` bool on the private `ActiveShard` record, updated in the existing
  `Update` loop; raise `event Action ShardGrabbed` / `event Action ShardReleased` per the decision-4
  transition rules (grab: false→true; release: true→false **and** `!motion.IsHeld`). Raising inline during
  the enumeration is safe — handlers only play cues, nothing mutates `_shardPads` (unlike the expiry path,
  which keeps its collect-then-raise buffer). No payload, no per-frame allocation, spawner stays XRI-free.
- **`RoundLoopController`** += `public bool IsPaused => _paused;` (one line). Everything else is already
  there: `RoundStarted`, `CorrectInserted`, `WrongInserted`, `ShardExpired`, `RoundEnded` are surfaced
  (T13/T14) and gated while paused. `IsPaused` exists because the **new grab/release transitions bypass
  those gates** — XRI selection still processes at `timeScale = 0`, and the spawner's held-state sampling
  runs every `Update` — so the feedback layer needs the pause flag to stay silent behind the pause menu.

### `FeedbackController` (MonoBehaviour adapter, `Scripts/Audio/`)
- `Initialize(AudioService, HapticService, RoundLoopController, ShardSpawner, AppFlowController,
  SettingsService)`; subscribes in `Initialize`, unsubscribes in `OnDestroy` (the `HUDPresenter` pattern).
- Cue map: `RoundStarted`→`RoundStart`; `CorrectInserted`→`CorrectInsert` **plus** `ComboMilestone` layered
  when `e.IsMilestone`; `WrongInserted`→`WrongInsert`; `ShardExpired`→`ShardExpired`;
  `RoundEnded`→`Victory`/`Overload`/`TimedOut` by `e.Result`; `ShardGrabbed`/`ShardReleased`→
  `GrabShard`/`ReleaseShard`; `UiSelected` + `SettingsService.Changed`→`UiSelect`. Audio for every cue;
  haptics wherever `HapticConfig` has an entry (at minimum `CorrectInsert`). The result-phase→cue mapping is
  a trivial switch (no gameplay logic — T14's "trivial display mapping" precedent, no separate unit test).
- **Pause gate:** skip `GrabShard`/`ReleaseShard` while `RoundLoopController.IsPaused` (the one event pair
  that bypasses the round loop's own gates); UI cues stay audible in pause by design.

### Composition root + scene (minor)
- Root: keep the `SettingsService` in a field (today a local); new serialized refs — `AudioCueConfig`,
  `HapticConfig`, the `AudioSource`, left/right `HapticImpulsePlayer`, the 4 `SimpleHapticFeedback`, the
  `FeedbackController`. Construct both services in `Awake`, inject via `FeedbackController.Initialize`,
  dispose both in `OnDestroy` (alongside the pool). Construction-only stays intact (no tick, no rules).
- Scene: an `Audio Source` object (one 2D `AudioSource`: `playOnAwake` off, `spatialBlend` 0 — spatial
  polish is T17/T18) + a `Feedback` object with `FeedbackController`; wire every new ref and **re-read to
  verify** (MCP discipline). Saved/committed.

### Assets + ledger (decision 7)
- ~10 CC0 clips under `Assets/ThirdParty/Kenney Sci-fi Sounds/` + `…/Kenney Interface Sounds/` (subset
  only); `AudioCueConfig` asset filled; `docs/assets/asset-ledger.md` rows updated (PLANNED → IMPORTED,
  date, path, subset note). Licenses re-verified on the source pages at download time.

### Behaviour (smoke)
- Menu/pause/settings buttons click audibly; round start plays a wake cue; hand grab / empty-space release
  play their cues; **a socket snap plays no grab cue and an insert plays no release cue** (the insert cue
  owns that moment); **no grab/release cues while paused** (UI clicks still audible); correct / wrong /
  expired each sound distinct; combo-5 layers the milestone cue; Won / Overloaded end with their cues
  (TimedOut stays silent unless a clip is assigned).
- **Settings really gate:** Sound OFF mid-round silences everything immediately (listener gate) and ON
  restores it; Haptics OFF sends no impulses **and** disables the rig select-haptics; both persist across
  Play sessions (T15).

## Implementation notes
- **New:** `Scripts/Audio/{FeedbackCue,AudioService,HapticService,FeedbackController}.cs`,
  `ScriptableObjects/Config/{AudioCueConfig,HapticConfig}.cs` + 2 assets, imported clips. **Edit:**
  `AppFlowController.cs` (UiSelected), `ShardMotion.cs` (IsHeldByHand), `ShardSpawner.cs` (transition
  events), `StarforgeRelayCompositionRoot.cs` (fields + construction + dispose), the scene.
- **Verified this session (2026-06-10):** the two `HapticImpulsePlayer`s and 4 `SimpleHapticFeedback`s exist
  in the scene at the paths above; `SendHapticImpulse` overloads as in decision 3; the round-loop events and
  the spawner's per-frame `IsHeld` read as described (code read, not memory). First-party audio assets:
  **none** (only the XRI sample `Button Pop.wav`) — hence decision 7.
- **Verify at impl via `unity_reflect`:** the `XRGrabInteractable` selector member for `IsHeldByHand`;
  `SimpleHapticFeedback`'s exact serialized/enable surface before toggling it. **Scene note:** 2 of the 4
  `SimpleHapticFeedback` components have `m_HapticImpulsePlayer: {fileID: 0}` (they resolve their player at
  runtime) — toggling `enabled` works either way; assign the refs while wiring only if reflection shows it
  matters.
- **Clip download:** assistant attempts `Invoke-WebRequest` + `Expand-Archive` from kenney.nl; if the
  download is unreliable, the human downloads and drops the files (division of labor) — flag it, don't stall.
- **MCP discipline (T08–T15):** `refresh scope=all` for new files; `manage_components` set + **re-read** for
  serialized refs (component arrays need `[{"instanceID":…}]`); never edit scripts while in Play; smoke from
  a settled editor (first Play after a recompile shows the benign domain-reload transient). No canvas work
  in T16, so the T15 scaled-canvas gotcha shouldn't bite — it applies only if a stray UI tweak sneaks in.
- **Style** (`csharp-style.md` + `.editorconfig`): C# 9 (block namespace, no `record`/`init`), no
  `#nullable enable`, `_camelCase` fields, `sealed`, XML docs, `var` only when RHS-apparent;
  `dotnet format` 0 IDE1006 on touched files (IDE0044 on `[SerializeField]` is the known project-wide false
  positive).
- **Audio behaviour notes:** `AudioSource`/`PlayOneShot` ignores `Time.timeScale`, so UI clicks stay audible
  while paused (desired); round cues can't fire while paused (the T13 insert/expiry gates). Keep clips short
  per GDD §18; one 2D source is enough for MVP overlap (`PlayOneShot` mixes internally).

## Out of scope
- **VFX** (beam/spark/fizzle/combo-pulse), core state visuals, and **per-result RoundComplete timing**
  (GDD §12: 2 / 1.5 / 1 s) → **T17**.
- GDD §18 **optional** audio: reactor hum loop, station ambience, heat-warning beep (the ambient loop is
  additionally §22 cut-first; the cue enum + config make them cheap to add later if T18+ wants them).
- Final mix levels, haptic strength/duration tuning, and spatialization → **T21** (on device); art/glow →
  T18–19; best score → its own follow-up (T15 carry-forward, not audio).
- **No gameplay-rule changes** — the tested T01–T06 core and all adapters' behavior stay identical; T16 only
  listens.

## Verification
- **Tests:** adapter/config task — **no new pure rule** (services are Unity-audio/XRI-haptics boundaries;
  the cue map is a trivial switch), so per the §17 gate there are no new EditMode tests by design; any
  genuinely pure helper that emerges ships tests in-change. **Re-run the full EditMode suite this session
  (103/103) — no regression.**
- **Restricted-API:** clean over `Assets/_Project`; the `Interaction.Toolkit` grep now expects exactly
  {`PortSocket`, `ShardMotion`, `HapticService`, `StarforgeRelayCompositionRoot`} — the first three are the
  §6-designated boundaries, the root carries only XRI-**typed serialized fields** (§8 wiring, no calls/logic);
  spawner/flow/feedback stay XRI-free; no `Resources.Load` (clips come via the config SO); `dotnet format`
  clean on touched files.
- **Smoke (XR Device Simulator, human):** the Behaviour list above — all cues audible at the right moments,
  no grab cue on socket snap, Sound OFF silences instantly (incl. mid-round), persistence across Play
  sessions. Console clean bar the known benign sim haptic-capability errors (they originate from the very
  `SimpleHapticFeedback` components this task gates; **logged once per device at channel-group init, not per
  impulse** — verified in `XRInputDeviceHapticImpulseChannelGroup.Initialize`'s `m_Device == device` early
  return — so T16's gameplay pulses do **not** multiply them; still absent on device — re-check at T20).
- **Haptics feel (the one thing the simulator cannot produce):** a **Quest Link pass** — correct insert
  pulses both controllers; Haptics OFF stops both the pulse and the grab select-buzz. If no headset is
  available this session, verify `SendHapticImpulse` is called without exceptions + the rig components
  toggle, and **re-check feel on device at T20** (fenced, like T08's deferred grab feel).
- **Done =** full EditMode suite green this session + restricted-API (with the extended XRI set) clean +
  `dotnet format` clean + the audio smoke + the haptics check above + no Console errors; then close docs —
  brief Status ✅ + **What was actually done**, matrix row, `current-status.md`, **asset ledger**.

## What was actually done
—
