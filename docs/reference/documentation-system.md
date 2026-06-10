# Starforge Relay VR — Documentation System

> **Purpose.** A blueprint for the repository-local documentation system of the **Starforge Relay** VR project (Unity 6.3 + OpenXR + XR Interaction Toolkit, Meta Quest 2/3, automated via MCP for Unity). It lets human developers and Claude Code sessions work across many sessions without losing context or turning the repo into a pile of stale notes.
>
> **Status.** This is the *blueprint* (the "why" and the templates). The **live** system is the repo's actual `CLAUDE.md`, `.claude/`, and `docs/` once they are created from this file. Where a template here differs from the real files, **the real files win**.
>
> **Foundation docs** (these seed the live docs and are referenced throughout): the GDD (design source of truth), the engineering guardrails, and the Claude Code rules reference. They are **consolidated in-repo** under `docs/` — `docs/product/game-design.md`, `docs/architecture/implementation-guardrails.md`, `docs/reference/` — linked from `docs/INDEX.md` (one source of truth per fact, not duplicated).
>
> **Verification & language.** Claude Code facts here were checked against the official docs (June 2026). All project Markdown is written in **English**.
>
> **Right-size this — the bootstrap minimum is the default.** For a 1–2 day VR prototype the **§5 bootstrap set is usually all you ever need.** Everything else in the tree (§4) and every template below is the *eventual* shape — add a file only when a real need appears, never as an empty stub, and prefer linking over duplicating. The documentation must never outweigh the game: if maintaining the docs costs more than the prototype, you over-built.

---

## 1. How to use this file

The chat that sets up the docs must:

1. Read this file and the other foundation docs first (the GDD, the engineering guardrails, and the Claude Code rules reference).
2. Confirm the actual Unity repository root before creating files (the Unity project may not exist yet — see the setup plan).
3. Stop and ask if another agent is actively working in the repo.
4. Create the **bootstrap minimum** (§5) only; do not pre-create empty stubs.
5. Keep generated docs small, linked, and maintainable.

The expected result: a new human or AI session can answer — *what is the game, what are the engineering guardrails, how do I build/test, what is the current state, what must I not do* — from the docs alone.

---

## 2. Design goals

Optimize for:

- Fast onboarding for a new human or AI session.
- Clear handoff between coding sessions.
- Pull-request-style review even when one person works alone.
- Low duplication; one clear source of truth per fact.
- A concise `CLAUDE.md` that loads reliably every session.
- **Hard** safety controls for actions that must be blocked (git writes), not just soft prompts.
- Practical Unity version-control rules.
- A small first implementation, expanded only when needed.

Do **not** optimize for:

- A documentation website before the prototype exists.
- A huge `CLAUDE.md` that burns context every session.
- Duplicating the same rules into `CLAUDE.md`, `AGENTS.md`, README, and random docs.
- Keeping important decisions only in chat history.
- Many empty folders and placeholder documents.
- Treating AI auto memory as a substitute for repository documentation.

---

## 3. Source-backed principles

1. **Docs as code.** Plain-text docs in the repo, reviewed and updated in the same workflow as code.
2. **README as front door.** What the project is, how to start, where to go next — not the whole manual.
3. **`CLAUDE.md` is soft guidance.** Persistent context, "not enforced configuration." For hard boundaries use permissions/hooks. (See `claude-code-rules.md`.)
4. **Keep `CLAUDE.md` concise** — target under ~200 lines; longer reduces adherence.
5. **`.claude/rules/` for modular guidance** — path-scoped rules load only when Claude touches matching files.
6. **Skills for procedures** — repeatable multi-step processes load on demand (`/skill-name`). Custom commands are now part of skills.
7. **Hooks/permissions for hard rules** — block git writes with `permissions.deny` (covering **Bash and PowerShell** on Windows) and, if needed, a `PreToolUse` hook.
8. **Separate documentation types** (Diátaxis): tutorials, how-to, reference, explanation are different needs.
9. **Record decisions as ADRs** — short, dated, versioned Markdown (MADR style).
10. **Human-readable changelog** — not a raw git-log dump.
11. **Conventional Commits** — scannable history for humans and tools.
12. **Respect Unity version control** — track `Assets`, `Packages`, `ProjectSettings`, `.meta`; ignore generated folders (`Library`, `Temp`, …).

---

## 4. Target repository documentation structure

Target layout inside the repo root. **Not** a command to create every file immediately.

```text
README.md
CLAUDE.md
AGENTS.md                          optional, only for non-Claude agents
CHANGELOG.md                       optional until there are notable changes
.editorconfig                      machine-enforced C# style (from editorconfig.template)
.github/
  pull_request_template.md         recommended for PR-style review
.claude/
  settings.json                    git-write deny rules (Bash + PowerShell)
  rules/
    unity-code.md                  path-scoped C# rules → points to implementation-guardrails.md
    documentation.md               Markdown/docs rules (optional)
  hooks/                           only if an ironclad block is needed
  skills/                          later, when a procedure repeats
docs/
  INDEX.md
  product/
    game-design.md                 the GDD — consolidated in-repo as the design source of truth
    scope.md                       optional if scope outgrows the GDD
  architecture/
    implementation-guardrails.md   the engineering contract — consolidated in-repo
    csharp-style.md                C# code style (spine + engine overlay) — enforced by .editorconfig
    overview.md                    optional if guardrails already explain the shape
    adr/
      0000-template.md             when the first ADR is added
  reference/
    claude-code-rules.md           how Claude Code rules work (background, not live config)
    documentation-system.md        this blueprint (background)
  development/
    setup.md                       as-built setup record from the setup stage (template: setup-playbook.md)
    build-and-test.md
    unity-workflow.md              XR + MCP for Unity habits
    ai-workflow.md                 optional if CLAUDE.md is enough
    agent-verification.md          verify state before claiming it (add with tasks/; template: agent-verification.md)
  quality/
    test-strategy.md
    performance-budget.md          VR FPS / draw-call budget
  assets/
    asset-ledger.md
  handoff/
    current-status.md
  release/                         created at the first distributable build (template: release-playbook.md)
    store-checklist-<store>.md     dated per-store requirement snapshot — regenerated each release
  tasks/                           graduate-to when the backlog outgrows current-status's "next actions"
    README.md                      matrix (plan to ship) + brief template (template: task-system.md)
    Tnn-….md                       one self-contained brief per task (+ a "what was actually done" field)
```

The GDD and the engineering guardrails are **consolidated in-repo** — GDD → `docs/product/game-design.md`, guardrails → `docs/architecture/implementation-guardrails.md`, the two reference docs → `docs/reference/` — and `docs/INDEX.md` links them (one source of truth per fact, no duplication). Do not create optional files as empty stubs. Template annotations in the tree ("template: …") point at the reusable **docs base** kept outside the repo (machine-local) — vendor a template into the repo only when it is first used.

---

## 5. Bootstrap minimum

For the first setup pass, create only what makes the next coding session safe and understandable:

- `README.md`
- `CLAUDE.md`
- `.claude/settings.json` (git-write deny: Bash **and** PowerShell)
- `docs/INDEX.md`
- `docs/development/build-and-test.md`
- `docs/handoff/current-status.md`
- `docs/assets/asset-ledger.md`
- `.editorconfig` at the repo root (copy from `editorconfig.template`) — once C# code is added, so the first generated code already conforms

Consolidated in-repo and linked from `INDEX.md`: the GDD → `docs/product/game-design.md`, guardrails → `docs/architecture/implementation-guardrails.md`, the reference docs → `docs/reference/` (link, don't duplicate). The C# style guide → `docs/architecture/csharp-style.md` (a portable asset — swap only its engine overlay), machine-enforced by the repo-root `.editorconfig`.

Also consolidated (not optional): `docs/development/setup.md` — the **as-built setup record** from the setup stage, i.e. the filled `setup-playbook.md` overlay with what actually happened (see the playbook's Phase B½ / Gate B½). Create if immediately useful: `.github/pull_request_template.md`, `.claude/rules/unity-code.md`. Create skills/subagents/hooks only after the basic docs exist and a real need appears — not in the first pass.

**Graduate-when-needed (not bootstrap):** a **task system** (`docs/tasks/` — a matrix planning the arc to ship + self-contained per-task briefs, each with a *"what was actually done"* field) and a **verification protocol** (`docs/development/agent-verification.md` — how to confirm work is really done before claiming it). Add them the moment the backlog outgrows `current-status.md`'s "next actions" bullets, or a second contributor appears. They are **portable assets** — copy the `task-system.md` and `agent-verification.md` templates and grow them, don't re-derive each project. The **release stage** is the same kind of graduate-when-needed asset: at the first distributable build, generate `docs/release/store-checklist-<store>.md` from the `release-playbook.md` template (stable categories + the method; requirement *values* are fetched from the store's official source and dated at release time, never baked in).

A **code-navigation graph layer** (Graphify-class tools: a queryable knowledge graph built over the codebase, so agents traverse a map instead of re-reading raw files) graduates the same way — it sits *between* the docs hierarchy and the source, automating only code navigation, never replacing the docs (which carry intent, rules, and as-built state a graph cannot). Adopt it only when a **real trigger** fires:

- the codebase has outgrown navigation by `INDEX.md` + guardrails + naming conventions — roughly beyond ~50k LOC, or a multi-project / monorepo workspace where no single map fits the docs;
- session transcripts show agents spending most of their tool calls **re-Reading/Grepping code they have already read** (the observable symptom, not a guess);
- the project has entered **long-lived post-release maintenance** *and* the codebase is near the size threshold above — lifetime amortizes only the **one-time indexing** cost, while the per-session tax below keeps recurring, so the first two triggers remain the primary signals.

Below those triggers it is a standing tax — tool schemas in every session's context, graph-freshness hooks to maintain, one more moving part — paid for savings the docs hierarchy already delivers on a small codebase. On adoption: **re-verify the current tooling at use time** (this tool class moves fast — process over values); record the choice plus its freshness/update discipline in an **ADR**; keep the hard safety rules (git denies, hooks) unaffected by the tool's own hook setup; and confirm the indexer's **ingestion scope excludes secrets/keystores** — the `settings.json` read-denies bind the assistant's tools, **not** an external indexing process — recording the ingestion scope and the index storage location in the same ADR.

---

## 6. Source-of-truth rules

Every important fact has one home:

| Fact type | Source of truth |
|---|---|
| Project summary and quick start | `README.md` |
| Claude Code session rules | `CLAUDE.md` |
| How Claude Code rules work (reference) | `docs/reference/claude-code-rules.md` |
| Documentation map | `docs/INDEX.md` |
| Game rules, UX, content, MVP | `docs/product/game-design.md` |
| MVP cuts and non-goals | `docs/product/game-design.md` (or `docs/product/scope.md` if it grows) |
| Engineering guardrails | `docs/architecture/implementation-guardrails.md` |
| C# code style (naming, formatting, conventions) | `docs/architecture/csharp-style.md` (enforced by repo-root `.editorconfig`) |
| Architecture decisions | `docs/architecture/adr/` |
| Setup steps | `docs/development/setup.md` |
| Build and test commands | `docs/development/build-and-test.md` |
| Unity / XR / MCP workflow | `docs/development/unity-workflow.md` |
| Test strategy | `docs/quality/test-strategy.md` |
| Performance targets (VR FPS budget) | `docs/quality/performance-budget.md` |
| Asset sources and licenses | `docs/assets/asset-ledger.md` |
| Current active state | `docs/handoff/current-status.md` |
| Store/release requirement snapshots | `docs/release/store-checklist-<store>.md` (dated; regenerated per release) |
| Task plan + per-task briefs (when the backlog grows) | `docs/tasks/` — matrix + `Tnn-*.md` |
| State-claim verification discipline | `docs/development/agent-verification.md` |
| Release notes | `CHANGELOG.md` |

If the same command or rule appears in more than one place, pick one home and replace the duplicates with links.

---

## 7. `CLAUDE.md` policy

The primary Claude Code entry point — read at the start of every session, so keep it concise and useful every time. Under ~200 lines; direct commands; details in linked docs or `.claude/rules/`; never paste the full GDD or guardrails; back hard-safety rules with `.claude/settings.json` or hooks.

Recommended `CLAUDE.md`:

````markdown
# Claude Code Instructions — Starforge Relay (VR)

## Project Snapshot
Stationary VR arcade for Meta Quest 2/3. Unity 6.3 + OpenXR + XR Interaction Toolkit (URP),
automated via MCP for Unity. Design: `docs/product/game-design.md`. Engineering: `docs/architecture/implementation-guardrails.md`.

## Start Every Session
1. Read `docs/INDEX.md` and `docs/handoff/current-status.md`.
2. Read the task-relevant docs (GDD section, guardrails).
3. Run read-only `git status --short` before editing.
4. State what you are about to change before broad edits.

## Must-Read Docs
- `docs/product/game-design.md`
- `docs/architecture/implementation-guardrails.md`
- `docs/development/build-and-test.md`
- `docs/handoff/current-status.md`

## Repository Rules
- First-party code/content under `Assets/_Project/`; commit `.meta` files with assets.
- Do not edit generated `Library/`, `Temp/`, `Obj/`, `Logs/`, `UserSettings/`, or build output.
- Add package deps by editing `Packages/manifest.json`; document why.
- Update docs in the same change when behavior, setup, architecture, tests, assets, or workflow change.

## Architecture Rules
- Follow `implementation-guardrails.md`. Manual DI / composition root; no `GameManager.Instance`.
- No scene-wide searches or `Camera.main` in runtime hot paths. Keep gameplay rules testable outside the headset.
- Ports validate color in code (not a color-gated socket filter). No ballistic throwing.

## Verification
- `docs/development/build-and-test.md` is the source of truth for commands.
- Never claim a test passed unless it was run this session or exact evidence is cited.

## Git Policy
- Read-only git only: `git status`, `git diff`, `git log`.
- Never `git add`, `git commit`, `git push`, or destructive git. Propose a Conventional Commit message; the human commits.

## End Every Session
Update `docs/handoff/current-status.md`: outcome, files changed, checks run, blockers, next actions.
````

---

## 8. `AGENTS.md` policy

Claude Code reads `CLAUDE.md`, not `AGENTS.md`. Add an `AGENTS.md` only if the repo will also be opened by Codex or another agent that auto-discovers it; then keep it short and import it from `CLAUDE.md` with `@AGENTS.md` so there is one source. Do not maintain two independent agent manuals.

---

## 9. `.claude/rules/` policy

Use when instructions are too detailed for `CLAUDE.md` but still need to load regularly. Do not mirror whole docs — a rule file adds a short operational rule or points to the real source of truth.

Path-scoped Unity C# rule:

````markdown
---
paths:
  - "Assets/**/*.cs"
---
# Unity C# Rules
- Follow `implementation-guardrails.md`.
- Naming / formatting per `csharp-style.md`, enforced by `.editorconfig` (private fields `_camelCase`, **not** bare `camelCase`).
- No `GameObject.Find` / `FindObjectOfType` / `Camera.main` in runtime gameplay code.
- Use serialized references, composition-root wiring, factories, or pools instead of hidden globals.
- Keep gameplay rules testable outside MonoBehaviours; add/Update EditMode tests when changing pure logic.
````

Markdown documentation rule:

````markdown
---
paths:
  - "**/*.md"
---
# Documentation Rules
- Write docs in English; use relative links for repo files.
- Add `Last verified: YYYY-MM-DD` to setup, build, platform, and workflow docs.
- Do not duplicate commands — link to `docs/development/build-and-test.md`.
- Do not add a new doc unless it is linked from `docs/INDEX.md`.
````

`~/.claude/rules/` holds personal, all-project rules (loaded before project rules, so project rules win).

---

## 10. `.claude/settings.json` policy

Hard safety boundaries and shared config. Permission rules are enforced by Claude Code, not the model; they **merge across scopes** and a **deny at any scope wins**; evaluation is **deny → ask → allow**.

**Critical (Windows):** the agent's shell tool here is **PowerShell**, a separate permission tool from Bash. Git-write denies must cover **both**, or the rule is a no-op.

The full worked deny list — git writes for **Bash and PowerShell** (including the bare argless verbs) + secret/keystore read-denies — lives in **`claude-code-rules.md` §5** (one home; this blueprint links rather than duplicates it). The repo's live `.claude/settings.json` is the enforced instance.

Notes: this policy assumes the human reviews, stages, commits, and pushes. Keep personal settings in `.claude/settings.local.json` (auto-gitignored). Do not enable bypass modes in shared settings. Confirm the live PowerShell tool name in `/permissions`. Argument-level Bash/PowerShell filtering is fragile — use a hook for argument logic.

---

## 11. Hooks policy

Hooks are for actions that must be deterministic. **Do not add hooks in the first setup pass** unless there is a real enforcement need — `permissions.deny` + clear `CLAUDE.md` rules are enough to start.

If you want an ironclad git-write block that survives unusual phrasings, add a `PreToolUse` hook with `matcher: "Bash|PowerShell"` that blocks git write subcommands via **exit code 2** (blocking error; stderr is fed to Claude before permission rules are evaluated) or an exit-0 JSON `permissionDecision: "deny"`. See `claude-code-rules.md` §5 for the exact script. Good hook uses: block dangerous commands, run a Markdown/format check after edits, scan the working tree at `Stop` for docs that must be updated. Avoid hooks for vague reminders, hidden automation, per-turn network calls, or anything the developer cannot inspect. Document any hook in `docs/development/ai-workflow.md`.

---

## 12. Skills policy

Skills are repeatable procedures that load on demand (`/skill-name`) and cost almost no context until used. **Custom commands are merged into skills** — `.claude/commands/x.md` and `.claude/skills/x/SKILL.md` both create `/x`. Create a skill when the same multi-step instruction is pasted more than twice or a `CLAUDE.md` section becomes a procedure. Do not create skills in the first pass.

Likely project skills (later): `session-handoff` (update `current-status.md`), `pre-pr-review` (diff + tests + restricted-API scan + Conventional Commit proposal), `unity-device-test` (Quest build + on-device smoke checklist).

`SKILL.md` shape:

````markdown
---
name: session-handoff
description: Update project handoff after a coding/documentation session. Use before stopping work.
---
# Session Handoff
Update `docs/handoff/current-status.md`: date, context, objective, outcome, files changed,
commands/checks run, exact test status, decisions, blockers, next three actions.
Do not paste full logs. Do not claim tests passed without evidence.
````

---

## 13. Subagents policy

Optional; add only after the base workflow is stable. Each subagent (`.claude/agents/*.md`, YAML frontmatter `name`/`description`/`tools`/`model`) runs in its own context window with limited tools — good for code review, docs audit, architecture-risk review, or XR test-plan review. Keep one focused purpose per subagent and limit tool access. Do not use subagents to bypass the handoff process.

---

## 14. Core documentation files (templates)

### 14.1 `README.md` (front door)
Include: project name; one-paragraph description; current status; Unity version (6.3 LTS / `6000.3.x`); target platform (Quest 2/3 standalone); required tools; quick-start pointer; build/test pointer; docs-map pointer; AI-workflow pointer. Exclude: full game design, long architecture rules, chat history.

### 14.2 `docs/INDEX.md` (map + freshness)

````markdown
# Documentation Index
Last verified: YYYY-MM-DD

| Document | Purpose | Update when |
|---|---|---|
| `product/game-design.md` | Game rules, UX, content, MVP | Gameplay/UX changes |
| `architecture/implementation-guardrails.md` | Engineering rules | Architecture/code-quality rules change |
| `architecture/csharp-style.md` | C# style (naming/format) — enforced by `.editorconfig` | The style or its enforcement changes |
| `architecture/adr/` | Dated architecture decisions (ADRs) | A decision is made or superseded |
| `reference/*.md` | Background (Claude Code rules ref; this blueprint) | The reference material changes |
| `development/setup.md` | First setup | Unity/packages/platform setup change |
| `development/build-and-test.md` | Exact build/test commands | Commands/verification change |
| `quality/performance-budget.md` | VR FPS / draw-call budget | Targets change |
| `assets/asset-ledger.md` | Third-party assets + licenses | An asset is added/removed/modified |
| `handoff/current-status.md` | Current active state | End of every session |

## Start Here
1. `../README.md`
2. `handoff/current-status.md`
3. `product/game-design.md`
4. `architecture/implementation-guardrails.md`
5. `development/build-and-test.md`
````

Rule: no new doc may be added unless it is linked from `docs/INDEX.md`.

### 14.3 `docs/development/build-and-test.md` (command source of truth)
Every command copy-pasteable from the repo root. Include: how to run EditMode tests (Unity Test Runner / batch mode), PlayMode tests, how to build to Quest (Build Profiles, `adb`), how to drive the editor via MCP for Unity, what counts as success, and what cannot be automated yet (real-device tracking/controller feel). Mark headset-specific specifics as "verify on first build". Add `Last verified: YYYY-MM-DD`.

### 14.4 `docs/quality/performance-budget.md` (VR)
Include: target devices (Quest 2 baseline, Quest 3 optional); **72 FPS floor on Quest 2** (90 optional on Quest 3 after profiling); max active shards (4 default, ≤6); particle/VFX limits; draw-call budget; GC-allocation goal (near zero in the active round); Single Pass Instanced; Fixed Foveated Rendering / dynamic resolution levers; profiling steps (Unity Profiler + on-device metrics); downgrade order when FPS drops (particles → shadows → overdraw → active shards, before changing rules).

### 14.5 `docs/assets/asset-ledger.md`
Seed from the GDD's CC0 list (Kenney Space Station Kit / Modular Space Kit / Space Kit / Particle Pack / Sci-fi Sounds / Interface Sounds; Quaternius Ultimate Space Kit; Poly Haven). Record every imported asset:

````markdown
# Asset Ledger
Last verified: YYYY-MM-DD

| Asset | Source URL | Author | License | Download date | Local path | Modifications | Notes |
|---|---|---|---|---|---|---|---|
````

Rules: no unclear-license asset in a showable build; record AI-generated assets (tool, prompt summary, date, rights); keep local paths accurate.

### 14.6 `docs/handoff/current-status.md` (memory bridge between sessions)

````markdown
# Current Status
Last updated: YYYY-MM-DD
Updated by: human or AI session
Branch/context:

## Current Objective
One or two sentences.

## Status
Done / partially done / not started.

## Files Changed Recently  *(this session only — replace each update; not a running per-task log)*
- `path/to/file`

## Checks Run
- `command`: result
- MCP/editor check: result
- Quest device check: result or not run (reason)

## Decisions Made
- Summary, with ADR link if applicable.

## Blockers
- Blocker, owner, next action.

## Next Actions
1. …  2. …  3. …
````

Rules: **`current-status.md` is a state snapshot, not a changelog** — it records only the *current* objective / status / blockers / next actions. **Do not accumulate a per-task history or track commit/PR state in it:** git owns commit/merge history, the task matrix (`docs/tasks/`) owns per-task status, and each brief's *"What was actually done"* owns the as-built record. **Refresh** it (don't append) at the end of every session; keep current, not historical; no full logs; absolute dates; no unverified test claims.

### 14.7 `docs/architecture/adr/0000-template.md`

````markdown
# ADR 0000: Title
Status: Proposed | Accepted | Deprecated | Superseded
Date: YYYY-MM-DD
Decision owner:

## Context
What problem requires a decision?

## Options Considered
- Option A / B / C

## Decision
What was chosen?

## Consequences
Easier / harder / riskier / more constrained.
````

Create an ADR for meaningful, hard-to-reverse choices: XR stack (OpenXR + XRI), DI approach (manual DI vs VContainer), scene structure, package additions, persistence, build pipeline, major guardrail deviations. One decision per ADR; supersede rather than rewrite accepted ADRs.

---

## 15. Pull request workflow

Use PR discipline even solo. Every logical change answers: what changed, why, how verified, what docs changed, what risks remain, proposed Conventional Commit message.

`.github/pull_request_template.md`:

````markdown
## Summary
What changed and why?

## Scope
- [ ] Gameplay  - [ ] XR/Interaction  - [ ] UI  - [ ] Audio/VFX/Haptics
- [ ] Persistence  - [ ] Architecture  - [ ] Assets  - [ ] Build/CI  - [ ] Documentation

## Verification
- [ ] EditMode tests run
- [ ] PlayMode / XR Device Simulator (or Meta XR Simulator) smoke run
- [ ] Unity compile checked (no Console errors)
- [ ] Quest device test run
- [ ] Screenshot/video attached for UI/VR changes

Commands/results:
```text
paste concise results here
```

## Documentation
- [ ] README / CLAUDE.md updated or confirmed unchanged
- [ ] GDD / guardrails updated or confirmed unchanged
- [ ] Build/test, asset-ledger, changelog updated or confirmed unchanged
- [ ] Handoff status updated

## Risks
Known risks, limitations, follow-ups.
````

---

## 16. Commit and changelog policy

Conventional Commits:

```text
feat(xr): add socket validation for reactor ports
fix(gameplay): stop combo reset on ignored expired shard
docs(architecture): record manual-DI decision
test(gameplay): cover heat overload at cap
perf(render): enable fixed foveated rendering
```

Types: `feat`, `fix`, `docs`, `test`, `refactor`, `perf`, `build`, `chore`, `ci`, `style`. Use `CHANGELOG.md` (Keep a Changelog style: Unreleased → Added/Changed/Fixed/Removed) for notable, demo-visible, build, asset, or performance changes — not raw commits.

---

## 17. Unity version-control rules

**Track:** `Assets/`, `Packages/` (incl. `manifest.json` + `packages-lock.json`), `ProjectSettings/`, `.meta` files, project docs, `.github/`, `.editorconfig`, reviewed `.claude/` shared config.

**Ignore:** `Library/`, `Temp/`, `Obj/`, `Logs/`, `UserSettings/`, build outputs (`*.apk`, `Builds/`) unless intentionally versioned, IDE-generated files. (Unity Hub's template `.gitignore`/`.gitattributes` cover most of this.)

**Asset rules:** Visible Meta Files mode; move/rename assets in the Editor (or move the paired `.meta` too); never commit orphan `.meta`; no empty folders for structure; keep first-party assets separate from third-party imports. **Git LFS** for large binary art/audio (`*.png`, `*.fbx`, `*.wav`, `*.mp3`, HDRIs) — run `git lfs install` once per machine.

---

## 18. AI session protocol

**Start:** confirm repo root; read `CLAUDE.md`, `docs/INDEX.md`, `docs/handoff/current-status.md`, and task-relevant docs; run read-only `git status --short`; state intended changes; ask before editing if another agent is active or the tree is unexpectedly dirty.

**During:** keep changes scoped; avoid unrelated refactors; update docs alongside behavior/setup/architecture/test/asset changes; create ADRs for significant decisions; run the documented checks; communicate blockers early.

**End:** update `docs/handoff/current-status.md`; state checks run and not-run (with reasons); summarize changed files; list blockers; propose next actions; propose a Conventional Commit message (the human commits).

---

## 19. Documentation maintenance cadence

| Event | Required action |
|---|---|
| End of every AI session | Update `docs/handoff/current-status.md` |
| Gameplay rule change | Update `docs/product/game-design.md` |
| Architecture decision | Add or supersede an ADR |
| Code-quality rule change | Update `docs/architecture/implementation-guardrails.md` or `.claude/rules/` |
| C# style or its enforcement change | Update `docs/architecture/csharp-style.md` / repo-root `.editorconfig` |
| Build/test command change | Update `docs/development/build-and-test.md` |
| Unity/package/platform setup change | Update `docs/development/setup.md` |
| New external asset | Update `docs/assets/asset-ledger.md` |
| Performance target change | Update `docs/quality/performance-budget.md` |
| Demo/release-visible change | Update `CHANGELOG.md` |
| New doc file | Link it from `docs/INDEX.md` |

---

## 20. Bootstrap order for the next chat

1. Confirm no other agent is editing the repo; confirm the repo root.
2. Create the bootstrap docs only (§5).
3. Create `CLAUDE.md` (§7).
4. Create `.claude/settings.json` with the Bash + PowerShell git-write deny rules (worked list: `claude-code-rules.md` §5; policy notes: §10).
5. Create `docs/INDEX.md` linking the consolidated GDD (`docs/product/game-design.md`), guardrails, and reference docs.
6. Create `docs/development/build-and-test.md`, `docs/handoff/current-status.md`, `docs/assets/asset-ledger.md`; and (once C# code is added) a repo-root `.editorconfig` from `editorconfig.template` + `docs/architecture/csharp-style.md` (swap its engine overlay).
7. Consolidate the filled setup plan as `docs/development/setup.md` (the as-built record; template: `setup-playbook.md`).
8. Add `.github/pull_request_template.md` and `.claude/rules/unity-code.md` if useful.
9. Add ADR 0001 only if an architecture choice is already locked.
10. Run a docs audit before feature coding.

Feature work may begin once these exist and the next session can answer: what is the game, what are the guardrails, how do I build/test, what is the current state, what must I not do.

---

## 21. Red flags

Stop and fix the docs system if: `CLAUDE.md` becomes the whole manual; `CLAUDE.md`/`AGENTS.md`/docs disagree; a chat relies on chat history instead of `current-status.md`; build commands are duplicated and differ; an architecture decision exists only in chat; new assets have no license record; PRs change behavior without updating docs; the changelog becomes a raw commit dump; session notes become a diary instead of current state; tests are claimed without evidence; a new doc isn't linked from `INDEX.md`; a `TODO` has no owner/date/next-action; **a hard safety rule (git-write block) is written only as soft instruction, or covers Bash but not PowerShell.**

---

## 22. Acceptance criteria

The documentation system is acceptable when:

- A new developer can start from `README.md` and `docs/INDEX.md`.
- Claude Code has a concise `CLAUDE.md` and (if used) modular `.claude/rules/`.
- Git writes are blocked by `permissions.deny` covering **Bash and PowerShell**.
- The current state is recoverable from `docs/handoff/current-status.md`.
- Game design and engineering guardrails have clear, single source files (consolidated in-repo under `docs/`).
- Build/test steps are exact enough to run.
- Important decisions are captured as ADRs.
- Assets have source and license records.
- PRs cannot silently skip documentation impact.
- The docs are small enough to maintain during a short VR prototype.

---

## 23. Sources

- Claude Code memory / `CLAUDE.md` / `.claude/rules/` — https://code.claude.com/docs/en/memory
- Claude Code settings & precedence — https://code.claude.com/docs/en/settings
- Claude Code permissions (Bash + PowerShell rules, deny precedence/merge) — https://code.claude.com/docs/en/permissions
- Claude Code hooks — https://code.claude.com/docs/en/hooks-guide , https://code.claude.com/docs/en/hooks
- Claude Code skills (commands merged into skills) — https://code.claude.com/docs/en/skills
- Claude Code subagents — https://code.claude.com/docs/en/sub-agents
- Diátaxis — https://diataxis.fr/
- Markdown ADRs (MADR) — https://adr.github.io/madr/
- Keep a Changelog — https://keepachangelog.com/en/1.1.0/
- Conventional Commits — https://www.conventionalcommits.org/en/v1.0.0/
- Unity external version control (Visible Meta Files / asset serialization) — https://docs.unity3d.com/Manual/ExternalVersionControlSystemSupport.html ; Unity `.gitignore` template — https://github.com/github/gitignore/blob/main/Unity.gitignore
