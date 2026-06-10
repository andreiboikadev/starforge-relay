# C# Style Guide — reusable template

> **Reusable, project-agnostic.** Drop into a new project as `docs/architecture/csharp-style.md` (or
> `docs/development/csharp-style.md`) and link it from `docs/INDEX.md`. Pairs with **`editorconfig.template`**
> (copy to repo root as `.editorconfig`) — **this doc teaches the conventions; the `.editorconfig` enforces
> them** in Visual Studio, VS Code (C# Dev Kit), Rider, and `dotnet format` / CI.
>
> **Spine + overlay (the only thing you swap per project).** **Part 1** is the engine-agnostic C# spine —
> identical for Unity, Godot-C#, MAUI, console, or backend; copy it **verbatim**. **Part 2** is a small
> engine/platform overlay (serialization idiom, lifecycle, hot-path perf, the C#-language-version ceiling)
> — **replace only that section** for a non-Unity app. This mirrors the playbook's "adapt the platform
> layer, keep the spine."
>
> **For code generators (LLMs):** generate code that already conforms to Part 1 + the active Part 2 overlay
> by default — do not emit a draft that the `.editorconfig` then flags. When unsure, prefer the rule here
> over training-data habits.
>
> All volatile facts were verified against the official sources listed at the end (checked June 2026).
> Where a project's live `.editorconfig` differs from this doc, **the `.editorconfig` wins** (it's the
> enforced contract); fix whichever is wrong so they agree.

---

## Part 0 — Why and how

- **Goal:** code that reads consistently for both humans and LLMs, is diff-clean across machines, and is
  cheap to maintain. Consistency beats personal preference — pick the convention, then enforce it.
- **Two layers, one source of truth per fact:** the *narrative + rationale* live here; the *machine-checked
  rules* live in `.editorconfig`. Don't duplicate the rule list into `CLAUDE.md`/guardrails — link here.
- **Severity ladder for adoption:** on a greenfield project set naming/style severities to `warning` from
  day one. On a project with legacy code, start at `suggestion`, run one `style:` pass to conform, then
  raise to `warning` (or `error` in CI). See `editorconfig.template` comments.

---

## Part 1 — Engine-agnostic C# spine (constant across platforms)

### 1. Naming

| Element | Casing | Example |
|---|---|---|
| Namespace, class, struct, enum, delegate, record | `PascalCase` | `ScoreService`, `ItemKind` |
| Interface | `I` + `PascalCase` | `IClock`, `IRandomSource` |
| Method, property, event (**any** accessibility) | `PascalCase` | `ResetScore()`, `Current` |
| Public field *(avoid — prefer a property)* | `PascalCase` | `Origin` |
| Private / protected / internal **instance** field | `_camelCase` | `_maxRetries`, `_scoreMultiplier` |
| Private / protected / internal **static** field (incl. `static readonly`) | `s_camelCase` | `s_sharedBuffer` |
| Thread-static field | `t_camelCase` | `t_scratch` |
| Constant (`const`) — any accessibility | `PascalCase` | `DefaultTimeout` (not `DEFAULT_TIMEOUT`) |
| **Public** `static readonly` value (constant-like) | `PascalCase` | `Identity`, `Zero` |
| Local variable, parameter | `camelCase` | `maxRetries`, `deltaTime` |
| Type parameter (generics) | `T` or `T` + `PascalCase` | `T`, `TKey`, `TResult` |
| Enum members | `PascalCase` | `Idle`, `Running`, `Done` |
| File name | = the public type it contains | `ScoreService.cs` |

Rules and rationale:

- **The `_`/`s_`/`t_` prefixes are for FIELDS only.** Methods and properties stay `PascalCase` regardless of
  accessibility (the .NET runtime / Roslyn convention). This is the single biggest difference from "just
  camelCase fields": a non-public field is `_maxRetries`, never `maxRetries`.
- **For static fields, accessibility (not `readonly`) picks the style:** a non-public `static` /
  `static readonly` field is `s_camelCase`; only a **public** static-readonly constant is `PascalCase`;
  a `const` is always `PascalCase` at any accessibility.
- **`t_` (thread-static) is doc-only:** `[ThreadStatic]` cannot be targeted by `.editorconfig` naming
  rules, so a `t_*` field will be flagged by the `s_` rule — suppress `IDE1006` for that line (thread-
  statics should be rare anyway).
- **Why the underscore:** it disambiguates a field from a parameter/local **without `this.`** — so write
  `_maxRetries = maxRetries;`, not `this.maxRetries = maxRetries;`. (MS conventions: qualify with `this.`
  is unnecessary once the prefix makes scope obvious.)
- **No legacy Hungarian / `m_` prefix** for new code (`m_` is a legacy engine-internal style). No
  type-encoding prefixes (`strName`, `iCount`).
- **Acronyms:** 3+ letters are cased as words (`HttpClient`, `XmlReader`, `MyRpc` — not `HTTPClient`,
  `MyRPC`); **2-letter acronyms stay upper** (`IO`, `UI`, `OS` — e.g. `IOException`,
  `Environment.OSVersion`). Be consistent with the surrounding API.
- **Booleans read as assertions:** `IsRunning`, `HasExpired`, `CanUndo`.
- **Constants are `PascalCase`, not `ALL_CAPS`** — `ALL_CAPS` is C/Java style, not idiomatic C#.
- **`async` methods that return `Task`/`Task<T>` end in `Async`** — `LoadAsync()` (skip the suffix only for
  event handlers and entry points where it adds nothing).
- **Make a field `const` when its value is compile-time constant; `static readonly` otherwise.** Prefer
  `const`/`readonly` to a mutable field whenever the value never changes after construction.

### 2. Layout & formatting

- **Braces: Allman style** — open and close brace each on their own line, aligned to the current indent.
- **Indentation: 4 spaces, no tabs.** One statement per line; one declaration per line.
- **Always use braces**, even for single-line `if`/`for`/`while` bodies (prevents the "goto fail" class of
  bug and keeps diffs clean).
- **Blank line between members** (methods, properties). No blank line right after an opening brace.
- **`using` directives go OUTSIDE the namespace, `System.*` first, then alphabetical.** Placing `using`
  outside the namespace keeps names fully qualified and immune to a dependency later adding a colliding
  nested namespace (MS conventions). Remove unused usings.
- **One public type per file**, and the file is named after that type. Small closely-related `private`/
  nested types may share the file.
- **Member order within a type** (top → bottom): nested types & delegates → **static fields** (`const`,
  `static readonly`, then other statics) → **instance fields** → properties → constructors (then finalizer)
  → methods. Within each group, **static members come before instance members**, then order by accessibility
  `public` → `internal` → `protected` → `private`. (Google C# guide.)
- **`var` only when the type is obvious from the right-hand side** — `var service = new ScoreService();` yes;
  `var x = Compute();` no (write the type). Don't use `var` for built-in types where it hides intent
  (`int count = 0;`, not `var count = 0;`).
- **Use the concise object-creation form when the type is on the left:** `ScoreService service = new();`
  (target-typed `new()` is C# 9 — allowed when the Part 2 overlay's language ceiling permits).
- **Strings:** interpolation for short concatenation (`$"{a}-{b}"`); `StringBuilder` for loops; verbatim
  (`@"..."`) for multi-line. (Raw string literals are **C# 11+** — gated by the Part 2 overlay's C#
  ceiling.) Don't build strings with `+` in hot loops.
- **Parentheses to make precedence obvious** in non-trivial boolean/arithmetic expressions.

### 3. Types & language usage

- **Access modifiers are always explicit** — never rely on the implicit `private`/`internal` default. Order
  modifiers conventionally: `public`/`private`/… then `static`, `readonly`, etc.
- **`sealed` by default** for classes not designed for inheritance (clarity + a small perf win). **`readonly`
  every field** that isn't reassigned after construction.
- **Prefer properties over public fields.** Public mutable fields leak invariants; expose `{ get; }` /
  `{ get; private set; }` instead.
- **Enable nullable reference types** (`#nullable enable` or project-wide) and honor it — annotate, then use
  guard clauses rather than scattered null checks.
- **Use language keywords, not framework types:** `string`/`int`/`float`, not `String`/`Int32`/`Single`.
  Prefer `int` over unsigned types unless the domain demands otherwise.
- **Pattern matching & switch expressions** over long `if/else` ladders and type-casts.
- **Exceptions:** throw specific types with a clear message; never `catch (Exception)` without a filter and a
  reason; never silently swallow in development code. Don't use exceptions for normal control flow.
- **Immutable data carriers:** small DTO/event payloads as `readonly struct` (and `record` only where the
  engine supports it — see Part 2).
- **`static` members are called via the type name** (`Math.Max(...)`), never via an instance or a derived
  type name.

### 4. Documentation & comments

- **XML doc comments (`///`) on all public types and members** — summary, `<param>`, `<returns>`,
  `<exception>` where it adds information. (This is what shows in IntelliSense for callers.)
- **`//` single-line comments** for in-body notes; avoid `/* */`. Start with a capital, end with a period,
  one space after `//`. Put the comment on its own line, above the code.
- **Comment the *why*, not the *what*.** The code says what; a comment earns its place only when intent or a
  non-obvious trade-off isn't visible from the code.

### 5. Enforcement — `.editorconfig` (the "embed once, enforced everywhere" lever)

- Copy `editorconfig.template` to the repo root as **`.editorconfig`**. It encodes Part 1's **naming rules
  and the machine-checkable formatting** (member ordering, blank-line habits, and XML-doc coverage stay
  doc-only — catch those in review) and is honored by **VS, VS Code (C# Dev Kit), Rider, and
  `dotnet format`** — any OS, any editor.
- Naming violations surface as **`IDE1006`**; set `dotnet_diagnostic.IDE1006.severity` to enforce on build.
- Run `dotnet format` (or the IDE's "Reformat/Cleanup") to auto-apply; wire `dotnet format --verify-no-changes`
  into CI once the codebase conforms.

---

## Part 2 — Engine / platform overlay  *(SWAP this whole section per project)*

> The block below is the **Unity** overlay (this package's default worked example). For a different engine,
> **replace this section** and keep Part 1 intact. A new overlay must declare: (a) the **C# language-version
> ceiling**, (b) the **serialization / inspector idiom**, (c) the **hot-path / perf** caveat, and (d) the
> **lifecycle** idioms.

### Unity (OpenXR + URP, Quest target — Unity 6.x)

**C# language-version ceiling — Unity 6.3 = C# 9.0 (Roslyn).** Therefore, in this project:

- **No file-scoped namespaces** (C# 10) — use **block-scoped** `namespace Foo { … }`. The `.editorconfig`
  pins `csharp_style_namespace_declarations = block_scoped`.
- **No global usings** (C# 10). `using` per file, outside the namespace.
- **Avoid `record` and `init`-only setters** unless you add an `IsExternalInit` shim, and **never use
  `record`/`init` in a serialized (`[SerializeField]`/SO) type.** Prefer `{ get; private set; }`.
- **Allowed C# 9:** target-typed `new()`, switch expressions, pattern matching, static lambdas, `nint`/`nuint`.
  (`record` and `init` are C# 9 too, but need the `IsExternalInit` shim noted above — avoid them here.)
- *(If a project bumps the Roslyn/lang version via `csc.rsp`, update this ceiling and the `.editorconfig`
  namespace rule together.)*

**Serialization / inspector idiom:**

- Expose inspector-editable data as **`[SerializeField] private T _field;`** — keep it private, never a
  public field. The Inspector strips the leading `_` and Title-cases it (`_heatCap` → "Heat Cap"), so the
  `_camelCase` field convention from Part 1 stays intact. Rider/VS handle the underscore on serialized fields.
- For an auto-property that must serialize, use **`[field: SerializeField] public T Value { get; private set; }`**.
- `ScriptableObject` for designer config/data; treat config assets as read-only at runtime.

**Hot-path / performance (overrides Part 1's "LINQ is fine"):**

- **No LINQ, no per-frame allocations, no boxing in `Update`/`FixedUpdate`/`LateUpdate`** or other per-frame
  gameplay loops. Cache references; reuse buffers; pool short-lived objects.
- No `GameObject.Find` / `FindObjectOfType` / `Camera.main` per frame (see the project guardrails).

**Lifecycle idioms:**

- Order MonoBehaviour messages conventionally: `Awake` → `OnEnable` → `Start` → `Update`/`FixedUpdate`/
  `LateUpdate` → `OnDisable` → `OnDestroy`. Subscribe in `OnEnable`, unsubscribe in `OnDisable`.
- **Keep testable rules out of MonoBehaviours** — pure C# (no `UnityEngine` statics, no `Time`/`Random`
  directly; inject a clock and a seeded random) so they run headless. MonoBehaviours are thin adapters.
- One assembly per layer via `.asmdef` when the project warrants it; first-party code under `Assets/_Project/`.

### Swapping the overlay for another platform (AR / 2D / mobile / desktop / Godot-C# / backend)

Replace the Unity block with your engine's equivalents. Examples of what changes — and only this changes:

- **C# ceiling:** a .NET 8 backend or current Godot-C# allows C# 12 → you may enable **file-scoped
  namespaces, global usings, records, collection expressions**; flip `csharp_style_namespace_declarations`
  to `file_scoped` in `.editorconfig`.
- **Serialization/inspector:** Godot `[Export]`; Unity-UI vs UI Toolkit; backend = none.
- **Hot path:** a 2D mobile game still bans per-frame allocs; a backend service cares about async I/O and
  `ConfigureAwait` instead.
- **Lifecycle:** Godot `_Ready`/`_Process`; ASP.NET DI lifetimes; etc.

Part 1 does **not** change. If you find yourself editing Part 1 for a platform reason, it belongs in the
overlay instead.

---

## Sources (verified June 2026)

- Microsoft — .NET / C# coding conventions (adopted from the dotnet/runtime + Roslyn guidelines):
  https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions
- Microsoft — Framework Design Guidelines (naming): https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/
- Microsoft — Code-style naming rules in `.editorconfig` (IDE1006, symbol groups, styles):
  https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/style-rules/naming-rules
- Google — C# Style Guide (private `_camelCase`, member ordering, modifier order):
  https://google.github.io/styleguide/csharp-style.html
- Unity — C# compiler and language version reference (Unity 6.3 = C# 9.0):
  https://docs.unity3d.com/6000.3/Documentation/Manual/csharp-compiler.html
- Unity — `SerializeField`: https://docs.unity3d.com/ScriptReference/SerializeField.html
