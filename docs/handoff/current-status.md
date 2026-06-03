# Current Status

Last updated: 2026-06-03
Updated by: Claude Code (task-system + agent-verification docs session)
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
- **Done — docs/rules bootstrap (2026-06-02 session):** `CLAUDE.md`, `README.md`, `.claude/settings.json`
  (git-write deny for Bash *and* PowerShell + secret/keystore deny), `.claude/rules/unity-csharp.md` and
  `.claude/rules/docs.md`, `docs/INDEX.md`, ADR 0000/0001, `build-and-test.md`, `asset-ledger.md`, this
  file. The 4 source docs were consolidated in-repo: GDD → `docs/product/game-design.md`, guardrails →
  `docs/architecture/implementation-guardrails.md`, the two reference guides → `docs/reference/`.
- **Done — task system + verification (this session, 2026-06-03):** added `docs/tasks/` (the plan to
  demo-ready — matrix M0–M6 + per-task briefs; full briefs written for M1 `T01`–`T06`) and
  `docs/development/agent-verification.md` (anti-false-claim discipline, distilled from a larger project's
  protocol). Both linked from `docs/INDEX.md`. No gameplay code yet — docs only.
- **Done — reference-doc consistency sweep (2026-06-03):** corrected the consolidated in-repo docs
  (`docs/architecture/implementation-guardrails.md`, `docs/reference/{claude-code-rules,documentation-system}.md`)
  — fixed stale "foundation docs at repo root" wording and broken `VR-Game-Concept-GDD.md` references →
  `docs/product/game-design.md`, and tightened the git-deny example (added `switch`/`merge`; prefix
  `checkout`). Added GDD **§34 "Open design questions"** (deferred; MVP behaviour stays authoritative).
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

The full plan now lives in [`../tasks/README.md`](../tasks/README.md) (matrix **M0–M6** to demo-ready).
Work top-down from there; immediate queue:

1. **M1 — pure rules (T01–T06), each with EditMode tests** (guardrails §17): `T01` RoundConfig +
   ScoreService + ComboTracker → `T02` Heat + Stabilization → `T03` RoundTimer → `T04` PortValidation →
   `T05` ShardSpawnPlanner → `T06` RoundController. **Start with T01.**
2. **M2 — VR vertical slice (T07–T11)** in `SampleScene`: strip Locomotion, primitive shards (grab on
   Grip) + accept-any sockets (validate color **in code**) + reactor / feeder spawn + round-loop adapter.
3. **M3+ (T12–T21)** — composition root, app flow, world-space UI, then feedback / art / device tuning.

Per task: write/confirm the brief, set Status 🟡, do the work + tests, then close it (brief + matrix +
this file) per [`../development/agent-verification.md`](../development/agent-verification.md).

## Notes for next session

Read `CLAUDE.md` + this file first. All docs are in-repo. Every mechanic follows the test gate
(guardrails §17): unit tests in-change, smoke before done, no regression. Hard rules block all git
writes (Bash and PowerShell) — propose a commit message, don't commit. Strip the rig's Locomotion branch
at the first interaction slice. Verify any XR API against the installed XRI 3.3.0 / OpenXR 1.16 packages
before relying on it.

**In-Editor testing enabled:** OpenXR is now enabled for **both Android and Standalone**, so Quest Link
Play and the XR Device Simulator work in the Editor for fast iteration. A device **Build And Run**
remains the final validation (real tracking / controllers / grab feel).
