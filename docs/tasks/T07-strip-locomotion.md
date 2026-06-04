# T07 — Strip Locomotion; rename scene off `SampleScene`; confirm rig

| | |
|---|---|
| Branch | `chore/strip-locomotion` |
| Milestone | M2 — VR slice |
| Design ref | GDD §7 / §9 / §27 (no locomotion, stationary, Floor); guardrails §13 (XR rules), §17 (smoke gate); [ADR 0001](../architecture/adr/0001-tech-baseline.md) |
| Depends on | — |
| Touches scenes/prefabs | yes — `XR Origin (XR Rig)` + scene rename (the only build scene) |
| Status | 🟡 in progress |

## Goal

First scene/rig task of the VR slice. Remove the XRI Starter-Assets **Locomotion** branch (no locomotion
in MVP — GDD §7/§27, guardrails §13), rename the scene off the URP default `SampleScene`, fix the stale
`templateDefaultScene` path, and confirm the rig is the clean stationary baseline that T08+ (shard grab /
port sockets / spawner) build on.

## Acceptance criteria

**Rig unpacked, then locomotion stripped** — the `XR Origin (XR Rig)` is **unpacked** from the Starter
Assets prefab first (no `PrefabInstance` link remains; the imported sample prefab stays untouched), then —
verified by `find_gameobjects by_component` returning **none** of the provider types below:
- Delete `XR Origin (XR Rig)/Locomotion` and everything under it: `LocomotionMediator`, `XRBodyTransformer`,
  and the 7 provider objects — `Turn` (`SnapTurnProvider`, `ContinuousTurnProvider`), `Move`
  (`DynamicMoveProvider`), `Grab Move` (`GrabMoveProvider` ×2, `TwoHandedGrabMoveProvider`), `Teleportation`
  (`TeleportationProvider`), `Climb` (`ClimbProvider`), `Gravity` (`GravityProvider`), `Jump` (`JumpProvider`).
- **On each controller** (`Left`/`Right Controller`) — locomotion lives here too, not only in the
  `Locomotion` subtree: remove the **`ControllerInputActionManager`** component (binds Move / Turn /
  SnapTurn / TeleportMode input actions and references the Teleport Interactor), delete the child
  **`Teleport Interactor`** (`XRRayInteractor`), and drop that interactor from the controller's
  **`XRInteractionGroup`** member list.
- **On the XR Origin / Camera Offset** — `CharacterController` (only moved by locomotion); `XRGazeAssistance`
  (its config is gaze + teleport-ray assist — both gone); the two `* Controller Teleport Stabilized Origin`
  objects; the inactive `Gaze Interactor` / `Gaze Stabilized` objects.

**Rig confirmed (kept, not removed):**
- Exactly **one** `XROrigin` and **one** `XRInteractionManager` in the scene.
- `XROrigin.RequestedTrackingOriginMode = Floor` (currently `2` ✔️).
- `Main Camera` (Camera + AudioListener + TrackedPoseDriver) and both controllers stay, each keeping its
  **`Near-Far Interactor`** (near grab **and** far UI ray) and **`Poke Interactor`**. These are the grab / UI
  interactors T08 builds on — the rig already ships **Near-Far**, which settles ADR 0001's
  Direct-vs-Near-Far follow-up. `InputActionManager` (XR Origin) and `XRInputModalityManager` stay intact.

**Scene renamed** (full reference set from a repo-wide `SampleScene` grep; keep the `.meta` GUID so refs re-link):
- Rename `Assets/_Project/Scenes/SampleScene.unity` → `Assets/_Project/Scenes/<NewName>.unity`.
- `ProjectSettings/EditorBuildSettings.asset` — the **single build scene** path (confirmed: it lists exactly
  this one scene); verify it points at the new path after the rename.
- `ProjectSettings.asset → templateDefaultScene` — currently the stale `Assets/Scenes/SampleScene.unity` (no
  build impact); fix to the new path.
- Docs: `README.md` (×2), [`build-and-test.md`](../development/build-and-test.md),
  [ADR 0001](../architecture/adr/0001-tech-baseline.md) (path-only); clear the "scene still SampleScene"
  watch line in `current-status.md` on close.
- **Decide in review:** `Assets/Settings/SampleSceneProfile.asset` — the URP Volume Profile referenced by the
  scene's `Global Volume` (`sharedProfile`), also carrying the template name. Rename to match `<NewName>` or
  leave it (GUID-tracked → a rename re-links automatically). Not functionally required.

> **Scene name — confirm in review.** Proposed **`StarforgeRelay`** (the single MVP scene = the whole game).
> Alt: `ReactorBay` / `StationBay`. One word, cheap to change before commit.

## Implementation notes

- **The `XR Origin (XR Rig)` is a prefab instance** of the Starter Assets prefab (`m_SourcePrefab` guid
  `f6336ac4ac8b4d34bc5072418cdc62a0`, under `Assets/Samples/…` — imported third-party; do **not** edit that
  shared prefab, guardrails §4). **Unpack the instance Completely first** (`PrefabUtility.UnpackPrefabInstance`,
  or GameObject ▸ Prefab ▸ Unpack Completely) so the rig becomes plain scene objects; then do the removals
  below. Editing via prefab overrides also works but leaves locomotion in the source prefab and a messy
  override list — unpack is cleaner for this one-off customized rig.
- Rig edits via MCP (`manage_gameobject` delete / `manage_components` remove). Deleting the `Locomotion`
  GameObject removes the mediator + body transformer + all 7 providers at once; then re-verify "no providers
  left" with `find_gameobjects by_component`.
- Scene rename via `manage_asset` (rename preserves the GUID); build scene list via
  `manage_build(action='scenes')`. The editor is open, so set `templateDefaultScene` through the editor API,
  **not** by hand-editing `ProjectSettings.asset` (Unity may rewrite it).
- Keep the controllers' **`Near-Far`** and **`Poke`** interactors (grab + UI + poke); only the **teleport**
  interactor and its locomotion glue come out. Tuning the grab interactor is **T08's** job, not this task.
- Pitfall — teleport is referenced in **four** places; removing the `Teleport Interactor` alone leaves
  dangling refs and broken interactor state-switching. Clean them together: the controller's
  `ControllerInputActionManager` (`m_TeleportInteractor`), the controller's `XRInteractionGroup` member list,
  and the XR Origin's `XRGazeAssistance` (`m_RayInteractors`).
- Removing `ControllerInputActionManager` means no more interaction/teleport mode-switching — `Near-Far` +
  `Poke` then stay always-on (what a grab + UI game wants). Trade-off: its stick UI-scroll goes too
  (`uiScrollingEnabled`); we select on Trigger, so that is acceptable.

## Out of scope

- Choosing / tuning the grab interactor; shard prefab; sockets; spawner; reactor; any gameplay wiring (T08–T11).
- Station-bay art / lighting; HUD; menus.
- Any change to the pure M1 rules.

## Verification

- **Smoke (XR Device Simulator — guardrails §17):** enter Play; head + both controllers track; **stick /
  button input no longer moves, turns, or teleports the rig** (locomotion truly gone); and **after removing
  `ControllerInputActionManager`, confirm `Near-Far` grab + `Poke` + the far UI ray still work**. Read the
  Console (clean — no dangling-reference errors).
- **Regression:** the full **EditMode** suite stays green — T07 changes no pure rules (see
  [build-and-test.md](../development/build-and-test.md)).
- **Project Validation** (Android tab) passes; no Console errors.
- Quest / Quest-Link pass not required here (no grab-feel change yet) — defer device feel to T08.

## What was actually done

— (filled on close)
