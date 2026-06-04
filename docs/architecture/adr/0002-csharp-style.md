# ADR 0002: C# code style and enforcement

Status: Accepted
Date: 2026-06-04
Decision owner: Project owner

## Context

First-party C# (the M1 pure-rule classes) was generating non-public fields as bare `camelCase` (`heatCap`,
`correctScore`, …). The only guidance — `.claude/rules/unity-csharp.md` — said merely "camelCase fields":
no private/public distinction, no `_` prefix, no machine enforcement. So both an LLM and a human had nothing
precise to follow and the style drifted. We need one precise, machine-checked C# convention that human- and
LLM-generated code both follow, on Unity 6.3 / C# 9, across two dev machines. The convention and its
enforcement file are derived from the reusable bootstrap package (`csharp-style.md` + `editorconfig.template`).

## Options Considered

- **Bare `camelCase` fields (status quo)** — ambiguous; the cause of the drift. Rejected.
- **Unity-internal `m_` prefix** — legacy Unity-engine style, not for new code. Rejected.
- **Google C# Style** — `_camelCase` for non-public fields *and properties*. Extends `_` to properties
  (uncommon in app code, more churn). Rejected in favor of the runtime convention.
- **.NET runtime / Roslyn convention** — `_camelCase` instance fields, `s_camelCase` static, `t_`
  thread-static; `PascalCase` for types / methods / properties / constants; the `_`/`s_`/`t_` prefixes are
  for *fields only*. **Chosen** — the de-facto large-C#-codebase standard, minimal property churn.
- **Doc-only vs enforced** — a prose guide alone drifts. Chose to **also enforce** via `.editorconfig`.

## Decision

- Adopt the **.NET-runtime naming convention** plus Allman braces, 4-space indent, `using` outside the
  namespace, `var` only when the type is apparent, one public type per file.
- **Home:** [`docs/architecture/csharp-style.md`](../csharp-style.md) — an engine-agnostic spine + a Unity
  overlay that pins the **C# 9 ceiling** (block-scoped namespaces, no global usings, no `record`/`init` in
  serialized types).
- **Enforcement:** a repo-root `.editorconfig` (rule `IDE1006`, severity `warning`), honored by Visual
  Studio / VS Code / Rider / `dotnet format` / CI.
- Conformed the existing M1 pure-rule classes + `RoundConfig` (`_camelCase` fields, no `this.`); the
  EditMode tests already conformed and stayed green (45/45).

## Consequences

- Easier: consistent human + LLM codegen; machine-checked in every IDE and in CI; diff-clean across the two
  dev machines. New gameplay code (from T05 on) is written to this style from the first line.
- Constrained: new first-party C# must conform; the `.editorconfig` namespace knob is pinned `block_scoped`
  for Unity's C# 9 — raising the language version later means updating both the overlay ceiling and the
  `.editorconfig` knob together (the language baseline is [ADR 0001](0001-tech-baseline.md)).
- One home per fact: the full rule list + cited rationale (Microsoft / Google / Unity) live in
  `csharp-style.md`; this ADR records *why* we chose it, not the rules themselves.

## Follow-Up

- Raise the naming severity from `warning` to `error` once a CI step runs `dotnet format --verify-no-changes`.
- Future projects inherit this convention from the bootstrap package; this ADR is the per-project record.
