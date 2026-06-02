# Current Status

Last updated: 2026-06-02
Updated by: Claude Code (Part B VR setup + docs bootstrap session)
Branch/context: `dev` (local workspace — human reviews and commits)

## Current objective

The VR engine setup (Part B) is complete and verified on a Quest 2, and all project documentation now
lives in-repo under `docs/` (single source of truth). Next: begin the gameplay vertical slice from
primitives, following the GDD development order and the per-mechanic test gate.

## Status

- **Done — VR setup (Part B, committed):** OpenXR provider enabled for Android + Meta Quest Support;
  Oculus Touch interaction profile; Render Mode = Single Pass Instanced; platform Android + ASTC; Player
  settings Linear / Vulkan-only / IL2CPP / ARM64 / min API 32 / target API 34 / package
  `com.innowise.starforgerelay`; Project Validation (Android) clean (0 errors). Minimal VR scene
  `Assets/Scenes/SampleScene.unity`: **XR Origin (XR Rig)** with tracking origin **Floor**, an
  **XR Interaction Manager**, and the rig's Input Action Manager; default Main Camera removed; scene in
  the Build list. **Verified on a real Quest 2** — built, installed, launched, OpenXR session reached
  FOCUSED, Vulkan render, Touch controllers tracked, no crash.
- **Done — docs/rules bootstrap (this session):** `CLAUDE.md`, `README.md`, `.claude/settings.json`
  (git-write deny for Bash *and* PowerShell + secret/keystore deny), `.claude/rules/unity-csharp.md` and
  `.claude/rules/docs.md`, `docs/INDEX.md`, ADR 0000/0001, `build-and-test.md`, `asset-ledger.md`, this
  file. The 4 source docs were consolidated in-repo: GDD → `docs/product/game-design.md`, guardrails →
  `docs/architecture/implementation-guardrails.md`, the two reference guides → `docs/reference/`.
- **Not started:** all gameplay code. No scripts under `Assets/_Project/` yet. No ScriptableObject
  configs, prefabs, or tests yet. No DI container (manual DI by ADR 0001). The rig still includes the
  Starter Assets **Locomotion** branch — to be stripped during implementation (no locomotion in MVP).

## Files changed recently

- Engine config (committed earlier): `ProjectSettings/*`, `Assets/XR/*`, `Assets/Settings/*`,
  `Assets/Scenes/SampleScene.unity`, `Assets/XRI/Settings/Resources/InteractionLayerSettings.asset`.
- Docs bootstrap (this session, uncommitted): `CLAUDE.md`, `README.md`, `.claude/settings.json`,
  `.claude/rules/*`, `docs/**`.

## Checks run

- Part B verified on Quest 2 via `adb` + logcat (OpenXR FOCUSED, Vulkan, Touch tracked, no crash).
- Project Validation (Android): 0 errors (the optional SSAO warning was resolved by removing the
  renderer feature from `PC_Renderer`).
- Docs session: no compile/tests required; Unity Editor not modified.

## Decisions made

- Technical baseline recorded in [ADR 0001](../architecture/adr/0001-tech-baseline.md): Unity 6.3 LTS,
  URP, OpenXR 1.16 + XRI 3.3.0 + Meta Quest Support + Oculus Touch, Single Pass Instanced, Vulkan,
  IL2CPP, ARM64, manual DI, single scene, tracking origin Floor, pooling via `ObjectPool<T>`.
- **In-repo docs are the single source of truth.** The parent-folder source `.md` files were copied in;
  treat the in-repo copies as authoritative (delete the parent copies at your discretion).

## Blockers

- None.

## Next actions

1. **Pure rules + tests first.** Add a `RoundConfig` ScriptableObject (90 s, 20 stabilization, heat cap
   8, 4–6 shards, 14→12 s lifetime, combo +50/5) and plain-C# `ScoreService`, `ComboTracker`,
   `HeatService`, `StabilizationProgress`, `RoundTimer`, `PortValidationService`, `ShardSpawnPlanner`
   under `Assets/_Project/Scripts/Gameplay`, each with EditMode tests (guardrails §17).
2. **VR vertical slice** in `SampleScene`: primitive reactor + 3 ports (XR Socket Interactors) +
   grabbable primitive shards; grab on Grip; validate color **in code** on socket select;
   correct/wrong/expired feedback. Verify under the XR Device Simulator / Quest Link.
3. **Composition root** wiring menu → calibration → start round → grab shard → socket → results, using
   serialized refs + manual DI; pool shards / beams / VFX.

## Notes for next session

Read `CLAUDE.md` + this file first. All docs are in-repo. Every mechanic follows the test gate
(guardrails §17): unit tests in-change, smoke before done, no regression. Hard rules block all git
writes (Bash and PowerShell) — propose a commit message, don't commit. Strip the rig's Locomotion branch
at the first interaction slice. Verify any XR API against the installed XRI 3.3.0 / OpenXR 1.16 packages
before relying on it.

**Known setup gap:** OpenXR is enabled for the **Android** target only — not Standalone. So in-Editor
headset testing (Quest Link Play, XR Device Simulator) is **not wired yet**; the validated test path is
a device **Build And Run**. Enabling OpenXR for Standalone is a quick follow-up that unlocks fast
in-Editor iteration.
