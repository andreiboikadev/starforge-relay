# Current Status

Last updated: 2026-06-03
Updated by: Claude Code (T02 — heat + stabilization rules)
Branch/context: `feature/heat-stabilization` (off `dev`; human reviews and commits)

## Current objective

The VR engine setup (Part B) is complete and verified on a Quest 2, and all project documentation now
lives in-repo under `docs/` (single source of truth). Next: begin the gameplay vertical slice from
primitives, following the GDD development order and the per-mechanic test gate.

## Status

- **Done — VR setup (Part B, committed):** OpenXR provider enabled for Android + Meta Quest Support;
  Oculus Touch interaction profile; Render Mode = Single Pass Instanced; platform Android + ASTC; Player
  settings Linear / Vulkan-only / IL2CPP / ARM64 / min API 32 / target API 34 / package
  `com.innowise.starforgerelay`; Project Validation (Android) clean (0 errors). Minimal VR scene
  `Assets/Scenes/SampleScene.unity` (relocated to `_Project/Scenes/` by `1cfbb91`, see below): **XR Origin
  (XR Rig)** with tracking origin **Floor**, an
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
  `checkout`). Added, then **resolved**, GDD §34 — the 5 design ambiguities decided (recommendation each) and fixed in §12/§14/§16/§28 (§34 is now a decisions log); MVP behaviour unchanged (4 shards, 20-req, standing, 14→12 s).
- **Done — project structure tidy (committed `1cfbb91`):** created `Assets/_Project/` (first-party
  root, by-type per guardrails §4) and moved the VR scene there → `Assets/_Project/Scenes/SampleScene.unity`
  (GUID preserved; build list + scene-path docs updated). Deleted URP-template cruft (`TutorialInfo/`,
  `Readme.asset`). Left package/template config (`XR/ XRI/ Settings/ Samples/`) and the referenced
  `InputSystem_Actions.inputactions` in place. `ProjectSettings.asset templateDefaultScene` still points at
  the old path (vestigial template field — no build/gameplay impact).
- **Done — M1 pure rules T01–T02:** `ShardColor`, `RoundConfig` (+ asset), `ScoreService`, `ComboTracker`
  (T01 — committed via PR #1); `HeatService` (overload at cap, combo relief) + `StabilizationProgress`
  (victory at 20) (T02 — on branch `feature/heat-stabilization`). All pure C# under `Assets/_Project/` with
  `StarforgeRelay.Runtime` + `StarforgeRelay.Tests.EditMode` asmdefs. **Full EditMode suite 24/24 green**
  (MCP Test Runner, 2026-06-03); no Console errors. Briefs: [T01](../tasks/T01-round-config-scoring.md),
  [T02](../tasks/T02-heat-stabilization.md).
- **Not started:** M1 rules `T03`–`T06` (Timer, PortValidation, ShardSpawnPlanner, RoundController); all
  MonoBehaviour adapters / scene wiring (M2+); no prefabs; no DI composition root (manual DI by ADR 0001).
  The rig still includes the Starter Assets **Locomotion** branch — to be stripped at the first interaction
  slice (T07; no locomotion in MVP).

## Files changed recently

- Engine config (committed earlier): `ProjectSettings/*`, `Assets/XR/*`, `Assets/Settings/*`, the VR scene
  (committed under `Assets/Scenes/`, **relocated to `Assets/_Project/Scenes/SampleScene.unity` by `1cfbb91`**),
  `Assets/XRI/Settings/Resources/InteractionLayerSettings.asset`.
- Docs bootstrap (committed earlier): `CLAUDE.md`, `README.md`, `.claude/settings.json`, `.claude/rules/*`,
  `docs/**`.
- Project structure tidy (committed `1cfbb91`): `Assets/_Project/` first-party root + moved scene
  (above); deleted URP-template `Assets/TutorialInfo/` + `Assets/Readme.asset`; updated
  `ProjectSettings/EditorBuildSettings.asset` and scene-path refs in `README.md` +
  `docs/{development/build-and-test,architecture/adr/0001-tech-baseline,handoff/current-status}.md`.
- T01 pure rules (committed via PR #1): `Assets/_Project/Scripts/Gameplay/{ShardColor,ScoreService,ComboTracker}.cs`,
  `Assets/_Project/ScriptableObjects/Config/RoundConfig.{cs,asset}`, `Assets/_Project/StarforgeRelay.Runtime.asmdef`,
  `Assets/_Project/Tests/EditMode/{StarforgeRelay.Tests.EditMode.asmdef,ScoreServiceTests.cs,ComboTrackerTests.cs}`;
  closed `docs/tasks/{README,T01-round-config-scoring}.md`.
- T02 pure rules (this branch `feature/heat-stabilization`, uncommitted):
  `Assets/_Project/Scripts/Gameplay/{HeatService,StabilizationProgress}.cs`,
  `Assets/_Project/Tests/EditMode/{HeatServiceTests,StabilizationProgressTests}.cs`; closed
  `docs/tasks/{README,T02-heat-stabilization}.md`.

## Checks run

- Part B verified on Quest 2 via `adb` + logcat (OpenXR FOCUSED, Vulkan, Touch tracked, no crash).
- Project Validation (Android): 0 errors (the optional SSAO warning was resolved by removing the
  renderer feature from `PC_Renderer`).
- Docs session: no compile/tests required; Unity Editor not modified.
- T01 (2026-06-03): full EditMode suite green — **12/12** via MCP Test Runner; no Console errors after compile.
- T02 (2026-06-03): full EditMode suite green — **24/24** (12 prior + 12 new); no Console errors; restricted-API scan clean.

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

1. **M1 — pure rules (each with EditMode tests, guardrails §17):** `T01` ✅ · `T02` ✅ → **next `T03`
   RoundTimer** → `T04` PortValidation → `T05` ShardSpawnPlanner → `T06` RoundController.
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
