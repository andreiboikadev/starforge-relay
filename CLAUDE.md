# Starforge Relay — Claude Code instructions

## Project snapshot
Unity **6.3 LTS** (`6000.3.16f1`), **URP** (Universal 3D). Building the GDD's **Starforge Relay**: a
stationary **VR arcade** for **Meta Quest 2/3** — stand at a reactor, grab glowing energy **shards** with
the Touch controllers, and insert each into the matching colored **port** to stabilize a mini-star before
a 90 s timer or the heat meter ends the round. XR stack: **OpenXR + XR Interaction Toolkit (XRI 3.x)**,
target **Android / Quest**. New **Input System**. Editor automation via the **MCP for Unity** server.
Aim: a playable MVP from primitives first, art later.

- Git repo root = this folder; it holds `Assets/ Packages/ ProjectSettings/`. Run Claude Code here.
- **All project docs live in-repo under `docs/` — the single source of truth** (see below). Design and
  reference docs were consolidated here; do not rely on copies outside the repo.

## Start every session
1. Read `docs/INDEX.md` and `docs/handoff/current-status.md`.
2. Read the docs relevant to the task (game design, guardrails, build & test).
3. Run `git status --short` before editing; state what you're about to change before broad edits.
4. If the working tree looks unexpectedly dirty or another agent may be active, ask before editing.

## Must-read docs (source of truth — don't re-derive design/architecture from chat history)
- `docs/product/game-design.md` — what to build (loop, rules, scoring, screens, MVP cut line).
- `docs/architecture/implementation-guardrails.md` — how to build it; decisions in `docs/architecture/adr/`.
- `docs/development/build-and-test.md` — exact build / test / verify commands (the only command source).
- `docs/handoff/current-status.md` — where we are right now.

## Architecture must-knows (full detail auto-loads from `.claude/rules/unity-csharp.md` when editing C#)
- Thin MonoBehaviour **adapters** over testable **plain-C# gameplay rules** (score, combo, heat,
  stabilization, timer, port validation, shard-spawn planning). Manual DI via **one composition root**;
  **no new singletons** (expected custom singletons = 0).
- Forbidden in runtime: `GameObject.Find`/`FindObjectOfType`, per-frame `GetComponent`/LINQ,
  `Camera.main` per frame, `SendMessage`, `Resources.Load`, `Instantiate`/`Destroy` of shards/beams
  mid-round (pool via `ObjectPool<T>`), legacy `Input`/direct `UnityEngine.XR` polling (use XRI actions).
- **OpenXR + XRI 3.x:** one XR Origin + one XR Interaction Manager; tracking origin **Floor**. Ports are
  **XR Socket Interactors** that accept **any** shard, then **validate color in code** on select (NOT a
  color-gated socket filter, or wrong-insert events never fire). Grab on **Grip**, world-space UI on
  **Trigger**. `throwOnDetach = false`, scripted/kinematic movement — **no ballistic throwing**. **No
  locomotion** in MVP. Single Pass Instanced + Vulkan. Verify any XR API against the installed package.

## Division of labor
- **Assistant:** edit C#/text files directly; Unity editor ops via **MCP** (GameObjects, components,
  scenes, materials, Test Runner, reading the Console); add package deps by editing
  `Packages/manifest.json`. **Read-only git only** (`git status`, `git diff`, `git log`, `git show`).
- **Human:** GUI toggles unreliable via MCP (platform switch, parts of Player/OpenXR settings, URP
  renderer feature, Graphics API selection), device build/run & Quest Link, and **all git commits**.

## Hard rule — git (also enforced in `.claude/settings.json` for Bash *and* PowerShell)
**Never** run `git add`, `git commit`, `git push` (or `reset`/`restore`/`checkout`/`switch`/`clean`/
`rebase`/`merge`). After each logical unit of work: stop, summarize, and **propose a commit message**.
The human reviews the diff and commits.

## Commit messages — Conventional Commits (always propose one, ready to paste)
```
<type>(<scope>): <imperative summary, ~50 chars, hard max 72>

<optional body — what & why, as bullets; omit if obvious>
```
- **Types:** `feat` `fix` `chore` `build` `docs` `refactor` `test` `perf` `style` `ci`
- **Scopes (typical here):** `xr` `gameplay` `ui` `audio` `vfx` `core` `scene` `deps` `build` `docs`
- **Examples:**
  - `feat(xr): minimal VR scene with XR Origin rig (Floor tracking)`
  - `feat(gameplay): validate port color on socket select`
  - `chore: enable OpenXR/Meta Quest and configure Android player settings`
  - `test(gameplay): cover heat overload at cap`

## Branching — GitFlow-lite
- `dev` — default integration branch. `feature/<slug>`, `fix/<slug>` for focused work, merged into `dev`.
- No `main`/`release/*` until we actually ship. When a change warrants its own branch, the assistant
  proposes the branch name (the human creates and switches it).

## Conventions (apply as needed, not "just in case")
- New **Input System** (XRI actions, not legacy `Input`); **ScriptableObjects** for config; **Prefabs**
  for instantiated objects.
- Keep our code/content under `Assets/_Project/...`, separate from imported packages/Samples.
- Introduce a new abstraction only on its **second** real use. (C# style: `.claude/rules/unity-csharp.md`.)

## "Ready for review" =
Compiles with no Console errors (checked via MCP) and **Project Validation** passes. **Per-mechanic test
gate (guardrails §17):** new/changed pure rules have EditMode tests in the same change; the **full**
suite re-runs with **no regressions**; and the mechanic had a **smoke pass in human-realistic
conditions** (MCP-driven Play Mode / XR Device Simulator / Meta XR Simulator, Quest device where
tracking/controllers/grab feel matter).

## End every session
Update `docs/handoff/current-status.md`: outcome, files changed, checks run + results, decisions
(link an ADR if one was made), blockers, next 3 actions. Never claim a test passed unless it ran this
session.
