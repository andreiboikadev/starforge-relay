---
paths:
  - "**/*.md"
  - "docs/**/*.md"
---

# Documentation Rules

- Write all project docs in **English**.
- Use **relative links** for repository files (e.g. `docs/architecture/implementation-guardrails.md`).
- Add `Last verified: YYYY-MM-DD` to setup, build, platform, and workflow docs. Use **absolute dates**,
  never "today" / "last week".
- **One source of truth per fact.** Do not duplicate commands — link to
  `docs/development/build-and-test.md` instead of repeating them.
- Do not add a new doc file unless it is linked from `docs/INDEX.md`.
- Keep docs small and current. Prune stale content instead of appending; this is not a changelog.
- A `TODO` must carry an owner, a date, and a next action — otherwise don't write it.
