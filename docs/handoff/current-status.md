# Current Status

Last updated: 2026-06-12
Updated by: Claude Code (T18 🟡 — station bay art dressing + fake-glow kit; Quest-2 smoked)
Branch/context: **`T07`–`T17` ✅** (#6–#17). **`T18` 🟡 in progress on `feature/art-dressing`** — dark-metal
Station Bay + fake-glow kit (Quest-2 smoked; carry-forwards in "Notes for next chat"). _Historical T17 detail:_ T17 (VFX pool + core state visuals) added the second feedback peer (sibling to T16 audio/
haptics): `StarforgeRelay.Vfx` = `PooledVfx`/`VfxPool<T>` (mirrors `ShardPool`) + `BeamVfx`/`SparkVfx`/
`FizzleVfx` + `VfxController` (3 new **gated spatial companion events** on `RoundLoopController` —
`CorrectInsertedAt`/`WrongInsertedAt`/`ShardExpiredAt`) + `CoreStatePresenter` (round events + `PhaseChanged`
→ `CoreVisualState`, reads live `Heat`/`Stabilization`); `ReactorCoreView` now state-driven (emissive glow +
ring + bursts, default Dormant); **per-result RoundComplete timing** (GDD §12 2/1.5/1 s) via
`RoundCompleteDelays` + `RoundConfig` fields. **No gameplay-rule change.** **Verified this session:** compile
0 errors; **EditMode 105/105** (103 + 2 per-result-timing cases, no regression); MCP **Play boot clean** with
the pools prewarming **16 instances** (4+6+6); restricted-API clean, XRI grep set **unchanged**. **Human
XR-sim smoke passed** (beams in all 3 colours, spark on wrong, fizzle on expiry, core charge/heat/stabilized/
overload glow, combo pulse, per-result RoundComplete beat). Brief: [`../tasks/T17-vfx-pool.md`](../tasks/T17-vfx-pool.md).

> **This file is a state snapshot, not a changelog.** Where-we-are / blockers / what's-next live here.
> Per-task detail lives in the `Tnn` briefs ("What was actually done"); the full task map in
> [`../tasks/README.md`](../tasks/README.md); commit/merge history in git. Don't duplicate those here.

## Where we are

- **M0 — engine setup:** ✅ OpenXR + XRI rig, Android/Quest config, **verified on a real Quest 2** (see
  [ADR 0001](../architecture/adr/0001-tech-baseline.md)).
- **M1 — pure rules:** ✅ **complete & merged to `dev`** (`T01`–`T06`; T06 = PR #5).
- **M2 — VR slice:** ✅ complete & merged — **`T07`–`T11b`** (#6–#11): the slice is whole — grab → colour-validate → correct / wrong / **expired** → win / overload / time-out.
- **M3 — wiring:** ✅ **complete** — **`T12`** (composition root) → **`T13`** (app state machine) →
  **`T14`** (world-space UI on ray+Trigger, #14) → **`T15`** (Settings + persistence).
- **M4 — feedback:** ✅ **complete & merged** — **`T16`** (commit `086cbd5`) + **`T17`** (#17).
- **M5 — art:** 🟡 **in progress** — **`T18`** (import Kenney + dress bay + fake-glow kit) substantially done &
  **Quest-2 smoked**; remaining = carry-forwards (below). Then **`T19`** (final glow on core/shards/ports),
  then **M6 device** (`T20`–`T21`). Full matrix: [`../tasks/README.md`](../tasks/README.md).
- EditMode suite **green 105/105** (T17 added 2 per-result RoundComplete-timing cases to `AppStateMachineTests`;
  no regression). XRI in C# stays confined to `PortSocket`/`ShardMotion`/`HapticService`/`StarforgeRelayCompositionRoot`
  (API grep — the new `StarforgeRelay.Vfx` code is XRI-free); new C# is **`dotnet format`-clean** (IDE1006
  naming + whitespace, `--verify-no-changes`, run this session on the touched files via the project `.csproj`).
- **C# code style adopted & enforced:** `docs/architecture/csharp-style.md` (from the upstream package) + a
  repo-root `.editorconfig`; the M1 pure-rule classes + `RoundConfig` were conformed (`_camelCase` fields, no
  `this.`). New code must follow it.

## Still to build / watch

- Scene runs the **complete M2 slice**: grab (T08) + 3 colour-validating sockets (T09) + reactor core /
  4 feeder pads / spawner with return-to-pad (T10) + round-loop wiring + consume/respawn + score finalization
  (T11) + shard **lifetime/expiry** (T11b) + the **T14–T15 UI** (5 screens + Settings overlay). Nothing
  left in M2; **M3 wiring complete** (`T12`–`T15` ✅); **next `T16`** (M4 feedback).
- **Env note (scene/rig tasks):** `execute_code` is broken on **both** dev machines (CodeDom `mono.exe`
  "filename or extension is too long"; no Roslyn) — re-verified on the work machine 2026-06-05; use
  structural MCP tools. Prefab **unpack** is a manual 1-click editor step; the MCP asset-rename tool reports
  "failed" but succeeds on disk (verify via filesystem).
- **MCP gotchas (scene/prefab tasks, T08 + T10):** `refresh_unity scope=scripts` recompiles but does **not
  import new files** (use `scope=all`); `manage_gameobject create` **silently ignores** `component_properties`
  **and** `components_to_remove` (set/remove via `manage_components` + **re-read to verify**); a **component-array**
  serialized ref must be set with `[{"instanceID":…}]` objects (bare ints resolve to `null`); `create
  save_as_prefab` leaves a **stray temp instance** in the scene (delete it); the **first Play after each
  recompile** throws a benign `routine is null` / AABB domain-reload cascade (a settled re-Play is clean), and
  for the same reason **never edit scripts while the Editor is in Play**. **T15 additions:** creating/
  duplicating UI **under the 0.001-scaled world-space canvas** corrupts the child RectTransform (scale
  ×1000, garbage pos/rot) — reset `localScale` / `anchoredPosition3D` / `localEulerAngles` after every
  create/duplicate; `manage_components` `target` takes a **GameObject** id — a *component* id fails "not found". **T18 additions
  (materials/scene):** `manage_material set_material_color` reads RGB as **0–255** (÷255) — set 0–1 / HDR
  colours via `set_material_shader_property _BaseColor [r,g,b,a]`; `manage_material` writes blend **floats**
  but **can't flip the URP `_SURFACE_TYPE_TRANSPARENT` keyword / render-queue**, so additive/transparent
  materials can't be authored via MCP (`execute_code` broken) — defer or use a 1-click human toggle (**this
  blocks the T19 halo-billboard**); **XR preloaded assets** (`XRGeneralSettingsPerBuildTarget` +
  `OpenXRPackageSettings`) can get **dropped from `ProjectSettings.preloadedAssets` during Play/test runs** —
  re-check / `git checkout` before a device build; `manage_gameobject create` **ignores `is_static`** and
  **adds a default collider** to primitives (strip it on dressing); `assign_material_to_renderer` resolves
  `target` **by name** (a bare instance-id string fails).
- **Env note (XR-sim smoke):** grabbing in the **XR Device Simulator** logs 2 benign XRI errors —
  `Failed to get haptic capabilities of XRSimulatedController … Continuing assuming a single haptic channel`
  (simulated controllers have no haptics; XRI falls back). Not a code defect; absent on real Touch
  controllers — re-confirm a clean device console at **T20**.

## Blockers

- None.

## Decisions

- Tech baseline: [ADR 0001](../architecture/adr/0001-tech-baseline.md) — Unity 6.3 LTS, URP, OpenXR 1.16 +
  XRI 3.3.0 + Meta Quest + Oculus Touch, Single Pass Instanced, Vulkan, IL2CPP/ARM64, manual DI, single
  scene, Floor origin, pooling via `ObjectPool<T>`.
- In-repo docs are the single source of truth.
- **2026-06-10 — docs synced with the upstream docs base** (validated that day by a cold-run test):
  `.claude/settings.json` hardened (bare-verb `git push/commit/reset` denies + `*.p12`/`*.pem` read-denies);
  `csharp-style.md` Part 1 aligned with the genericized spine + `t_` enforcement note (`.editorconfig`
  comments updated to match); guardrails §7 records the GDD-§12 shard-escalation config fields (not in
  as-built `RoundConfig` — 4 pads in MVP); `docs/reference/*` refreshed (deny-list one-home dedup, Windows
  PowerShell hook variant, release-stage + code-navigation-graph-layer graduate guidance). Docs-only change.
- **C# code style** = the package convention (.NET-runtime naming: `_camelCase` / `s_camelCase` fields,
  `PascalCase` types/methods/properties/consts), home in `docs/architecture/csharp-style.md`, enforced by the
  repo-root `.editorconfig` (rule `IDE1006`) — recorded in [ADR 0002](../architecture/adr/0002-csharp-style.md).

## Notes for next chat

- **T18 🟡 (this session):** Kenney imported & curated (Space Kit removed; SSK **FBX-only**), dark-metal
  **Station Bay** (floor/walls + window framing the reactor) + HDR glow accents + space backdrop (planet +
  stars); dark skybox, Directional Light shadows OFF, bloom kept. **Quest-2 smoked (device-approved).**
  Interactables stay **primitive placeholders → T19**. **Deferred (with rationale):** halo-billboard prefab →
  **T19** (additive-transparent material needs a render-queue/keyword the MCP material tool can't set +
  `execute_code` broken — build it with the interactable glow); **Static flags → T21** (URP SRP Batcher
  covers MVP batching; static pairs with the T21 bake/occlusion pass); accent intensity + starfield density →
  **T21** look-tuning (device-approved as-is). **Git LFS migrate still pending** — T18 art committed plain;
  run `git lfs install` + `git lfs migrate import` for the art globs when the art settles.
  **T17 carry-forwards:** wrong-insert "port red flash" is realised as the spark burst (dedicated `PortView`
  material-flash → T19); core has **no ring child** yet (ring spin is a no-op until T18/T19 adds one);
  AlmostStable/Heating thresholds + burst sizes/durations are **T21 tuning levers** (sensible defaults now).
- **T16 carry-forwards:** clip picks are **first-pass** (swappable in the `AudioCueConfig` Inspector; final
  mix/levels → T21). **Git LFS still deferred to T18** — the T16 `.ogg` are committed as plain binary; the
  T18 `git lfs migrate` (when the bulk art lands) sweeps them in (the `.gitattributes` plan). On the **Quest 2
  device run the sim haptic-capability warnings were absent**, as expected (resolves the §"Env note" T20 recheck).
- **Device finding → T20 (confirmed 2026-06-11):** the **first standalone build of the full game runs
  immersive on Quest 2** (OpenXR `XR_SESSION_STATE_FOCUSED`, no crashes). An initial "empty scene" was
  **start-orientation**, not a defect — the reactor + world-space UI sit at a fixed world pose and there is
  **no in-app recenter** yet (deferred T13/T14), so a player starting off-centre/mis-facing sees the empty bay;
  standing centred + the system recenter brings content in front. **T20: wire the deferred Recenter/calibration
  (GDD §9)** so the player always starts facing the reactor.
- **T15 carry-forwards:** the Settings toggles **persist but mute/buzz nothing yet — by design** (no
  consumer until T16; don't mistake silent toggles for a bug). **Best score** was cut from T15 → a small
  follow-up on the persistence seam (GDD §13/§16: store the max, show it on Results).
- **Diagnosis carry-forward:** the Play-stop `Leak Detected: Persistent N allocations` / `routine is null` cascade is the **first-Play-after-recompile transient** (a settled re-Play is clean) — confirmed at T12; not a code leak.
- **Tuning (T21, on device — not now):** held-slow `0.25` (≈56 s held) is generous, and idle shards expire in a synchronized wave (equal spawn + lifetime). Both are **GDD-§14 tuning levers confirmed on device**, deliberately **not** changed speculatively in code.
- **Carry-forward reminders:** **don't re-break the wrong-insert eject** — `ShardMotion.ReturnToPad()` is an **instant teleport** (a gradual lerp lets the socket's `keepSelectedTargetValid` re-snap the shard, the T10 bug). Shards/ports stay on the **Default** interaction layer — the named "Shard" layer stayed **deferred** (T14: Near-Far's near/far region split + `blockUIOnInteractableSelection` already separate grab from the UI ray; revisit only if a conflict surfaces). Spawn-colour variety within a round is a planner **tuning candidate (T21)**, not a bug.
- **Style covers ALL first-party C#** — runtime **and** tests (`.editorconfig` / IDE1006). New C#: `_camelCase` fields (static `s_camelCase`), `PascalCase` types/methods/properties/consts; C# 9 (block namespace, no `record`/`init`), no `#nullable enable`.
- **Per-mechanic test gate** (guardrails §17): pure rules ship EditMode tests in-change and the full suite
  re-runs green; interaction mechanics (M2+) also need a smoke / device pass. Pure-rule tasks need no device pass.
- **Hard rule — no git writes:** propose a commit message; the human commits. Read-only git only.
- Verify any XR API against the installed **XRI 3.3.0 / OpenXR 1.16** packages before relying on it.
- **In-Editor XR testing is enabled** (OpenXR on Android + Standalone): Quest Link Play and the XR Device
  Simulator work in the Editor; a device **Build And Run** stays the final validation.
