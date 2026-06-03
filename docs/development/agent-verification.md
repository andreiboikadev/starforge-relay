# Agent Verification & Self-Review

Last verified: 2026-06-03

**Why this exists.** An AI assistant's most damaging failure mode is the **confident false claim** — "that's
done", "that test passed", "that's not the cause" — stated without checking. This is the standing
discipline that prevents it. It is project-agnostic; reuse it across projects. (Distilled from a larger
project's hard-won `agent_verification_protocol.md`; the cross-team-communication parts of that original
are intentionally omitted here.)

## 0. Assume your first attempt is probably wrong — self-review before you present

A chat's first pass at anything non-trivial almost always carries an error, an omission, or over-reach.
**Treat your own output as a draft to be audited, not a finished answer.** Before presenting a result,
run the loop:

> **produce → adversarially self-review → cross-check against reality → fix → re-verify → only then present**

- **Audit your own work as a skeptic would** — assume a bug / inconsistency / over-reach is in there and
  go find it, instead of confirming it looks fine (the contradiction-seeking step, §3, is the mechanic).
- **Check it both ways (вдоль и поперёк):** does it do what was intended **and** not break anything else?
  Edge cases covered? Do cross-references, numbers, and names still line up? Did the change create a *new*
  inconsistency somewhere you didn't look?
- **Cross-check against the real thing, not your memory or the docs.** Run the tools (§4): for Unity, read
  the **Console** (compile/errors), **run the tests**, read the actual **editor / scene / git** state via
  MCP. Reality outranks every doc and every assumption (§1).
- **Be adequate:** is the result right-sized (not over-engineered, not gold-plated), free of invented
  facts, and does it solve the user's *real* need rather than a misread of it?
- **Iterate to the desired result.** If self-review finds anything, fix it and re-verify — loop until it's
  clean. **Stopping at "first attempt done" is the single most common failure; don't.**

Sections 1–6 below are the *mechanics* of this loop.

## 1. Sources of truth — precedence

When sources disagree, the higher one wins:

1. **Reality** — actual test output, the compiler / Console, `git` / the filesystem, a real run. **Beats
   every doc and every memory.** A doc claim is a *hypothesis about reality*, not reality.
2. **Task brief Status** (`docs/tasks/Tnn-*.md`) — canonical per-task state.
3. **Task matrix** (`docs/tasks/README.md`) — should mirror the briefs; if it disagrees, the brief wins
   (the matrix is stale and gets fixed).
4. **`docs/handoff/current-status.md`** — what's in flight right now.
5. **Memory / chat history** — lowest. Never the basis for a claim on its own.

## 2. Before claiming "done / passed / blocked / not the cause"

- Read the actual file / Status row — not your memory of it.
- For **done / passed**: cite evidence from **this session** (the test run, the Console read, the diff).
  Project rule stands: *never claim a test passed unless it ran this session.*
- For **"X is the bug" / "X isn't done"**: read the real artifact — the actual diff content (not just a
  file list), the actual log line (not a guess).

## 3. Contradiction-seeking step (before every non-trivial claim)

Confirmation bias is the trap: once you suspect "T05 isn't done", you only notice supporting evidence.
Counter it explicitly:

1. State your hypothesis in one line.
2. Ask: **"what would have to be true for the opposite to hold?"**
3. Verify against that opposite *before* you assert.

Red flags you're biased: the claim rests on an **absence** (a missing file / function — easy to misjudge
without an exhaustive search); you spent < 1 minute; the claim contradicts what the user just told you.

## 4. When to drop to git / build / tests

Trust the brief Status by default. Verify against reality when:

- Status is `✅ done` but another section mentions deferred / partial / follow-up work.
- Acceptance criteria name files (scenes, prefabs, scripts) you can't find.
- Two docs disagree.
- You're about to contradict the user or a recent commit.

Read the **content** — the real diff, the real test output — not just a file count or a summary. A tiny
diff can fully satisfy a brief that *looked* like it demanded new assets, if the work hooked into existing
ones; only the diff tells you.

## 5. Keep the few docs consistent (status transitions)

A status change rides with the work it describes — no separate "status-only" commits. On **close**, in the
same pass, update: the **brief Status** (+ fill *What was actually done*), the **matrix row**, and
**`current-status.md`**. Don't delete matrix rows; mark cancellations with a date + reason. If two
locations disagree, the most recent change wins and the drift is fixed the same session.

## 6. When in doubt — ask, don't invent

If after the above you still can't tell, **say so and ask the user**, quoting what you found and where it
conflicts ("matrix says ✅, brief says partial, and I see no trace in code — which is right?"). Honest
*"I'm not sure, let's verify X"* beats a confident wrong answer. Confident-wrong is the single biggest
source of lost trust.
