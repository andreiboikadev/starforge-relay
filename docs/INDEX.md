# Documentation Index

Last verified: 2026-06-03

This is the source-of-truth map for Starforge Relay. **No new doc should exist unless it is linked here.**

| Document | Purpose | Update when |
|---|---|---|
| [product/game-design.md](product/game-design.md) | Game rules, UX, content, MVP cut line | Gameplay or UX changes |
| [architecture/implementation-guardrails.md](architecture/implementation-guardrails.md) | Engineering rules + code-review checklist | Architecture / code-quality rules change |
| [architecture/adr/](architecture/adr/) | Dated architecture decisions (ADRs) | A significant decision is made or superseded |
| [development/build-and-test.md](development/build-and-test.md) | Exact build / test / verify commands | Commands or verification process change |
| [development/agent-verification.md](development/agent-verification.md) | Verify state before claiming it (anti-false-claim discipline) | The verification discipline changes |
| [tasks/README.md](tasks/README.md) | Task plan to demo-ready — matrix + per-task briefs (`tasks/Tnn-*.md`) | A task is added, started, or closed |
| [handoff/current-status.md](handoff/current-status.md) | Current active state, blockers, next actions | End of every session |
| [assets/asset-ledger.md](assets/asset-ledger.md) | Third-party assets + licenses | An asset is added, removed, or modified |

## Start here

1. [../README.md](../README.md)
2. [handoff/current-status.md](handoff/current-status.md)
3. [product/game-design.md](product/game-design.md)
4. [architecture/implementation-guardrails.md](architecture/implementation-guardrails.md)
5. [development/build-and-test.md](development/build-and-test.md)
6. [tasks/README.md](tasks/README.md) — the task plan; what to work on next

## Reference (background — not the live config)

Consolidated in-repo so the project is self-contained; these are explanatory sources, not the applied
setup. The applied setup is the actual `CLAUDE.md`, `.claude/`, and the docs above.

| Document | Purpose |
|---|---|
| [reference/claude-code-rules.md](reference/claude-code-rules.md) | How Claude Code rules / memory / permissions / hooks / skills work |
| [reference/documentation-system.md](reference/documentation-system.md) | Blueprint & rationale for this repo's documentation system (already implemented) |

## Deliberately not created yet (add only when the need is real)

This project is a short VR MVP; the doc set is kept lean on purpose. Create these **when friction
appears**, and link them here when you do:

- `product/scope.md` — folded into `game-design.md` for now.
- `architecture/overview.md` — folded into `implementation-guardrails.md`.
- `development/setup.md` — the README quick start is enough so far.
- `development/unity-workflow.md`, `development/ai-workflow.md` — covered by `CLAUDE.md` + `.claude/rules/`.
- `quality/test-strategy.md`, `quality/performance-budget.md` — the FPS / draw-call budget is summarized
  in guardrails §18 and game-design §26; promote to its own file once on-device profiling starts.
- `CHANGELOG.md`, `.github/pull_request_template.md`, `.claude/skills/`, hooks, subagents.
