# Claude Code Rules — A Practical Guide for the Starforge Relay VR Project

> **Who this is for.** A chat/agent (or developer) who needs to understand what "rules" are in Claude Code, which types exist, and how to compose them so the model works correctly and consistently on this project. **All facts here were verified against the official Claude Code documentation (checked June 2026; links at the end).**
>
> **What this is (and isn't).** This is *reference knowledge* — the "how Claude Code rules work" layer. The live, project-specific rules live in the repo's actual `CLAUDE.md`, `.claude/rules/`, and `.claude/settings.json` once they are created. Where this guide differs from those real files, **the real files win**.
>
> **Project context.** Stack: Unity 6.3 LTS + OpenXR + XR Interaction Toolkit (URP, Quest 2/3), automated via **MCP for Unity**. Dev host is **Windows** (the agent runs shell commands through the **PowerShell** tool, not Bash). **The human owns every git commit.** These three facts change the worked example in §5 versus a generic guide — read it.

---

## 1. The one mental model: SOFT vs HARD

Everything below is either a **soft rule** or a **hard rule**. Getting this distinction right is most of using rules well.

| Type | What it is | Enforced by | Guarantee |
|---|---|---|---|
| **SOFT** (guidance) | Instructions Claude reads and tries to follow | The model's judgment | High adherence if written well, but **no guarantee** |
| **HARD** (enforcement) | Constraints applied by the Claude Code client itself | The harness (not the model) | **Guaranteed** — applies regardless of what the model decides |

Official wording (verified):
- "Claude treats them [CLAUDE.md / auto memory] as **context, not enforced configuration**."
- "**Permission rules are enforced by Claude Code, not by the model.** Instructions in your prompt or `CLAUDE.md` shape what Claude tries to do, but they don't change what Claude Code allows."
- "To block an action regardless of what Claude decides, use a **PreToolUse hook** instead."

**Rule of thumb:** behavior, conventions, "how we work" → SOFT. "This must never happen" / "this must always run" → HARD.

---

## 2. The mechanisms (the actual "rules")

| # | Mechanism | Soft/Hard | Who writes | Committed to repo? | Use for |
|---|---|---|---|---|---|
| A | `CLAUDE.md` | Soft | You | Yes (project) | Always-on project facts, conventions, workflow |
| B | `.claude/rules/*.md` | Soft | You | Yes | Modular / path-scoped instructions |
| C | Auto memory | Soft | Claude | No (machine-local) | Things Claude learns from your corrections |
| D | `settings.json` `permissions` | **Hard** | You | Yes (`.claude/settings.json`) | Allow/ask/**deny** specific tools & commands |
| E | Hooks (`PreToolUse`, …) | **Hard** | You | Yes | Deterministic block/automation at lifecycle events |

### A. `CLAUDE.md` — the primary instruction file (SOFT)

Plain markdown that Claude loads **at the start of every session**. Locations, in load order (broadest → most specific; all are **concatenated**, not overridden):

| Scope | Location | Shared with |
|---|---|---|
| Managed policy | OS-specific managed path (e.g. `C:\Program Files\ClaudeCode\CLAUDE.md` on Windows) | Whole org (cannot be excluded) |
| User | `~/.claude/CLAUDE.md` | Just you, all projects |
| **Project** | `./CLAUDE.md` or `./.claude/CLAUDE.md` | **Team, via source control** |
| Local | `./CLAUDE.local.md` | Just you, this project (gitignore it) |

- Files up the directory tree load in full at launch; **subdirectory** `CLAUDE.md` files load **on demand** when Claude reads files in that subtree.
- **Size:** "target under 200 lines per CLAUDE.md file. Longer files consume more context and reduce adherence." Split with `.claude/rules/` or imports.
- **Imports:** `@path/to/file` (relative or absolute), recursive, **maximum depth four hops**. Imported files still load at launch and consume context.
- `CLAUDE.local.md` is the standard place for private, gitignored, per-project notes.
- **AGENTS.md:** Claude reads `CLAUDE.md`, *not* `AGENTS.md`. If you keep an `AGENTS.md`, put `@AGENTS.md` at the top of `CLAUDE.md` so both tools share one source.
- Block-level HTML comments (`<!-- … -->`) are stripped before injection — use them for maintainer notes that shouldn't cost context.
- Create with `/init`; view/verify loaded files with `/memory`.

### B. `.claude/rules/*.md` — modular, optionally path-scoped (SOFT)

For larger projects, split instructions into topic files in `.claude/rules/` (discovered recursively):

```
.claude/
├── CLAUDE.md            # main, always-on
└── rules/
    ├── unity-code.md    # path-scoped C# rules
    └── documentation.md # path-scoped Markdown rules
```

- Files **without** a `paths` field load at launch (same priority as `.claude/CLAUDE.md`).
- Files **with** `paths` frontmatter load only when Claude touches matching files — saves context:
  ```markdown
  ---
  paths:
    - "Assets/**/*.cs"
  ---
  # Rules that apply only to those files
  ```
- `~/.claude/rules/` holds personal, all-project rules; **user-level rules load before project rules, so project rules win**.

### C. Auto memory — Claude's own notes (SOFT, machine-local)

Claude writes learnings to `~/.claude/projects/<project>/memory/MEMORY.md`. The first **200 lines or 25 KB** of `MEMORY.md` load each session (topic files load on demand). It is **machine-local, not shared via git**, and shared across worktrees of the same repo. On by default (requires Claude Code v2.1.59+); inspect/toggle with `/memory`. Good for discovered build commands and preferences — **not a substitute for committed `CLAUDE.md` rules** that the whole team needs.

### D. `permissions` in `settings.json` — HARD, enforced

Three lists, evaluated **deny → ask → allow** (first match wins). Verified behaviors:
- "**Deny rules always take precedence.**" A deny at *any* settings scope blocks the call — "If a tool is denied at any level, no other level can allow it."
- **Permission rules MERGE across scopes** rather than override (unlike most settings, which follow Managed > CLI > Local > Project > User precedence).
- A **bare tool** deny (`Bash`) removes the tool from Claude's context entirely; a **scoped** deny (`Bash(rm *)`) leaves the tool available and blocks matching calls.

```json
{
  "permissions": {
    "allow": ["Bash(npm run *)"],
    "ask":   [],
    "deny":  ["Bash(git push *)"]
  }
}
```

Pattern syntax (verified):
- `Tool` or `Tool(specifier)`. `Bash(*)` ≡ `Bash`.
- Wildcards `*` can appear anywhere. A **space before `*`** enforces a word boundary: `Bash(ls *)` matches `ls -la` but not `lsof`; `Bash(ls*)` matches both.
- The `:*` suffix is **equivalent** to a trailing ` *` and is **only recognized at the end** of a pattern.
- **Shell-operator aware:** a `Bash(safe *)` rule does **not** authorize `safe && other`. Separators `&&  ||  ;  |  |&  &  newline` each split into subcommands that must each match.
- A fixed set of wrappers is stripped before matching (`timeout`, `time`, `nice`, `nohup`, `stdbuf`, bare `xargs`).
- **Caveat (from the docs):** "Bash permission patterns that try to constrain command **arguments** are fragile." Prefix blocks ("no `git commit`") are reliable; argument-level filtering is not — use a hook for those.

**⚠️ Windows / PowerShell (critical for this project):** on Windows the agent's shell tool is **PowerShell**, a *separate* permission tool with the **same rule shape** as Bash: `PowerShell(Get-ChildItem *)`, `PowerShell(git commit *)`, etc. Aliases are canonicalized (a rule for `Get-ChildItem` also matches `gci`/`ls`/`dir`) and matching is case-insensitive. **A `Bash(git commit *)` deny does nothing if the agent commits via the PowerShell tool** — so our deny list covers *both* (see §5). Confirm the live tool name in `/permissions`.

Settings file locations:
| Scope | File | Committed? |
|---|---|---|
| Project (shared) | `.claude/settings.json` | **Yes** |
| Local (personal) | `.claude/settings.local.json` | No (auto-gitignored) |
| User | `~/.claude/settings.json` | No |
| Managed | OS managed path | Admin-deployed, wins over all |

### E. Hooks — HARD, deterministic

Shell commands that run at lifecycle events "ensuring certain actions always happen rather than relying on the LLM." Events include `SessionStart`, `UserPromptSubmit`, `PreToolUse`, `PostToolUse`, `Notification`, `SubagentStop`, `Stop`, `PreCompact`, `SessionEnd` (and more).

A **`PreToolUse`** hook blocks a tool call in one of two verified ways:
- **Exit code 2** — blocking error. `stderr` is fed back to Claude; the call is prevented *before* permission rules are evaluated (so a hook can **block** a call an `allow` rule would otherwise have permitted — it adds restriction; it cannot grant past a `deny`, see below). Any stdout/JSON is ignored.
- **Exit code 0 + JSON** with a deny decision:
  ```json
  {
    "hookSpecificOutput": {
      "hookEventName": "PreToolUse",
      "permissionDecision": "deny",
      "permissionDecisionReason": "Reserved for the human; blocked by project policy."
    }
  }
  ```
  (`permissionDecision` ∈ `allow` / `deny` / `ask` / `defer`. Exit 0 with no output = no decision; the call continues through the normal permission flow — "staying silent doesn't approve it.")

Configured in a `hooks` block in `settings.json`; the `matcher` filters the tool name (e.g. `"Bash"`, `"Edit|Write"`, or a regex). Scripts typically live in `.claude/hooks/` and are committed. **Hook decisions do not bypass permission rules** — a matching `deny`/`ask` rule still applies regardless of what the hook returns.

> **Adjacent (not "rules"):** **Skills** (`.claude/skills/<name>/SKILL.md`) for on-demand procedures that load only when relevant or when you type `/skill-name` — note that **custom commands are now merged into skills** (`.claude/commands/deploy.md` ≡ `.claude/skills/deploy/SKILL.md`). **Subagents** (`.claude/agents/*.md`, YAML frontmatter `name`/`description`/`tools`/`model`) run isolated tasks in a separate context window with limited tools. Use skills for multi-step procedures that shouldn't sit in context all the time.

---

## 3. How to compose rules well

1. **Keep `CLAUDE.md` under ~200 lines.** Longer files consume context and *reduce* adherence. Split with `.claude/rules/` or imports.
2. **Be specific and verifiable.** "Use 2-space indentation" > "format code properly". "Run EditMode tests before marking done" > "test your changes".
3. **Structure it.** Markdown headers + bullets — Claude scans structure like a reader.
4. **No contradictions.** If two rules conflict, the model picks one arbitrarily. Prune periodically (`/memory` lists what's loaded).
5. **Put each thing in the right mechanism:**
   - Always-true project fact / convention → `CLAUDE.md`
   - Applies only to certain files → path-scoped `.claude/rules/`
   - A repeatable multi-step procedure → a **skill** (loads on demand)
   - "Must never happen" → `permissions.deny` (and a hook if it must be ironclad)
   - "Must always run at point X" → a **hook**
6. **Add a rule when you correct the same thing twice** — that's the signal it belongs in `CLAUDE.md`, not in chat.
7. **Don't bloat.** Every line is context spent every session. Delete stale rules.

---

## 4. How to use them for effective work

- **Commit the shared ones** so every machine and every new chat inherits them automatically: `./CLAUDE.md`, `./.claude/rules/*`, `./.claude/settings.json`, `./.claude/hooks/`.
- **Keep personal/machine-specific ones out of git:** `CLAUDE.local.md`, `.claude/settings.local.json`.
- **Verify loading:** run `/memory` — if a file isn't listed, Claude can't see it.
- **Tighten over time:** start minimal (a concise `CLAUDE.md` + the git-deny rules), grow as real friction appears. Same "no over-engineering" principle as good code.
- **When an instruction isn't followed:** make it more specific, check for conflicts, and if it *must* hold, promote it from SOFT (`CLAUDE.md`) to HARD (`permissions.deny` / hook).

---

## 5. Worked example — this project: MCP for Unity + "assistant never commits" on Windows

This is the exact setup we want: the assistant does the work via direct file edits and MCP-for-Unity editor operations, but **you review the diff and commit yourself**.

**`./CLAUDE.md`** (committed) — soft rules:
```markdown
# Project: Starforge Relay (VR)

## What this is
Stationary VR arcade for Meta Quest 2/3. Unity 6.3 + OpenXR + XR Interaction Toolkit (URP).
Design: docs/product/game-design.md. Engineering contract: docs/architecture/implementation-guardrails.md.

## Division of labor
- You (assistant): edit C#/text/asset files directly (clean `git diff`); do Editor operations
  via the MCP for Unity server (GameObjects/components, scenes, materials, run tests, read the
  Console); add package deps by editing `Packages/manifest.json`. Read-only git only.
- Me (human): GUI toggles unreliable via MCP (platform switch, parts of Player/OpenXR settings),
  running/building on the Quest and Quest Link, and ALL git commits.

## Hard rule
- Never run `git commit`, `git push`, or `git add`. After a logical unit of work, stop,
  summarize the change, and propose a Conventional Commit message. I review the diff and commit.

## Conventions
- First-party code/content under `Assets/_Project/`; commit `.meta` files with their assets.
- Before "ready for review": compiles with no Console errors (check via MCP); EditMode tests pass.
```

**`./.claude/settings.json`** (committed) — the hard rule that backs up the soft one. **Note the PowerShell rules** — on Windows the agent commits through the PowerShell tool, so Bash-only denies would not hold:
```json
{
  "$schema": "https://json.schemastore.org/claude-code-settings.json",
  "permissions": {
    "deny": [
      "Bash(git add *)",       "PowerShell(git add *)",
      "Bash(git commit *)",    "PowerShell(git commit *)",
      "Bash(git push *)",      "PowerShell(git push *)",
      "Bash(git reset *)",     "PowerShell(git reset *)",
      "Bash(git rebase *)",    "PowerShell(git rebase *)",
      "Bash(git restore *)",   "PowerShell(git restore *)",
      "Bash(git checkout *)",  "PowerShell(git checkout *)",
      "Bash(git switch *)",    "PowerShell(git switch *)",
      "Bash(git merge *)",     "PowerShell(git merge *)",
      "Bash(git clean *)",     "PowerShell(git clean *)",
      "Read(./.env)", "Read(./.env.*)", "Read(./secrets/**)"
    ]
  }
}
```
*Deny rules merge across scopes and a deny at any scope wins, so these hold even if user/local settings allow git. They are enforced by the client — the agent literally cannot run them. Your own terminal git is unaffected; only the agent's shell tools are.*

**Optional — ironclad block via hook** (only if you want a guarantee that survives unusual phrasings). `./.claude/settings.json` + `./.claude/hooks/block-commit.sh`:
```json
{
  "hooks": {
    "PreToolUse": [
      { "matcher": "Bash|PowerShell",
        "hooks": [ { "type": "command", "command": "${CLAUDE_PROJECT_DIR}/.claude/hooks/block-commit.sh" } ] }
    ]
  }
}
```
```bash
#!/bin/bash
# PreToolUse hook. The tool call arrives as JSON on stdin; block git write commands.
cmd=$(jq -r '.tool_input.command // empty')
if echo "$cmd" | grep -Eq '\bgit\b.*\b(commit|push|add|reset|rebase|restore|clean)\b'; then
  echo "Commits/pushes are reserved for the human; blocked by project policy." >&2
  exit 2          # exit 2 = blocking error: stderr goes to Claude, call is prevented before rules
fi
exit 0            # exit 0 with no output = no decision; the call continues normally
```
For most projects the `deny` rules + `CLAUDE.md` are enough; add the hook only if you want the extra guarantee. (MCP-for-Unity tools are namespaced `mcp__…` and are unrelated to git — they don't need git denies.)

**Why this matches a multi-machine / multi-chat setup:** `CLAUDE.md`, `.claude/settings.json`, and `.claude/hooks/` are all committed, so anyone who clones the repo — any machine, any new chat — inherits the same rules with zero extra setup.

---

## 6. Pitfalls

- **Treating `CLAUDE.md` as enforcement.** It's guidance delivered as context. If it MUST hold, use a permission/hook.
- **Bash-only denies on Windows.** The agent uses the **PowerShell** tool here — cover `PowerShell(...)` too, or the rule is a no-op.
- **Over-long `CLAUDE.md`.** >200 lines hurts adherence. Move detail to `rules/`, skills, or imports.
- **Argument-level Bash/PowerShell deny patterns.** Fragile (docs say so). Use a hook for argument logic.
- **Vague instructions.** "Be careful with X" does little. State the concrete do/don't.
- **Conflicting rules across files.** The model picks arbitrarily — keep them consistent; review with `/memory`.
- **Putting personal config in committed files.** Use `CLAUDE.local.md` / `settings.local.json`.

---

## 7. Cheat sheet

| Goal | Use |
|---|---|
| Persistent project context & conventions | `CLAUDE.md` (committed) |
| Instructions only for certain files | `.claude/rules/*.md` with `paths:` |
| On-demand multi-step procedure | a **skill** (`/skill-name`) |
| Block a command (e.g. git writes) | `permissions.deny` — Bash **and** PowerShell (+ hook for ironclad) |
| Always run something at an event | a **hook** (`PreToolUse`, `Stop`, …) |
| Personal, not shared | `CLAUDE.local.md`, `settings.local.json` |
| See what's loaded | `/memory` and `/permissions` |
| Generate a starting `CLAUDE.md` | `/init` |

---

## 8. Sources (official Claude Code docs, verified June 2026)

- Memory / `CLAUDE.md` / `.claude/rules/` / auto memory — https://code.claude.com/docs/en/memory
- Settings & file locations / precedence — https://code.claude.com/docs/en/settings
- Permissions (deny→ask→allow, merge, enforcement, Bash & PowerShell patterns, arg caveat) — https://code.claude.com/docs/en/permissions
- Hooks guide & reference (events, PreToolUse deny JSON, exit codes, matcher) — https://code.claude.com/docs/en/hooks-guide , https://code.claude.com/docs/en/hooks
- Skills (on-demand procedures; commands merged into skills) — https://code.claude.com/docs/en/skills
- Subagents (isolated context, tool limits) — https://code.claude.com/docs/en/sub-agents
