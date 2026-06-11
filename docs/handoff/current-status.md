# Current Status

Last updated: 2026-06-11
Updated by: Claude Code (T16 ✅ closed — device smoke passed)
Branch/context: **`T07`–`T16` ✅ on `dev`** (#6–#15; **T16 = audio + haptics, commit `086cbd5`**). T16 shipped
`FeedbackCue` + `AudioCueConfig`/`HapticConfig` SOs + `AudioService` (central `AudioListener.volume` gate) +
`HapticService` (both controllers + gates the rig's 4 `SimpleHapticFeedback`) + `FeedbackController`; event
seams (`AppFlowController.UiSelected`, `ShardSpawner` grab/release via `ShardMotion.IsHeldByHand`,
`RoundLoopController.IsPaused`); 10 Kenney CC0 clips in `AudioCueConfig`. **Verified:** EditMode **103/103**,
restricted-API clean (XRI in C# now `PortSocket`/`ShardMotion`/`HapticService`/`StarforgeRelayCompositionRoot`),
MCP Play clean, **+ human device smoke on a real Quest 2 (standalone): audio cues + Sound-OFF gate, and
haptics on both controllers + Haptics-OFF gate (incl. rig select-buzz) all confirmed.** **Next: `T17`**
(VFX pool + core state visuals). Brief: [`../tasks/T16-audio-haptics.md`](../tasks/T16-audio-haptics.md).

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
- **M4 — feedback:** **`T16`** (AudioService + HapticService + configs) ✅ **done** (commit `086cbd5`;
  device smoke passed on Quest 2). **Next: `T17`** (VFX pool + core state visuals); then M5–M6 (art →
  device). Full matrix: [`../tasks/README.md`](../tasks/README.md).
- EditMode suite **green 103/103** (T15 added 7 `SettingsService`/`GameSettings` cases; no regression).
  XRI in C# stays confined to `PortSocket` + `ShardMotion` (API grep); new C# is IDE1006-clean (`dotnet format`).
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
  create/duplicate; `manage_components` `target` takes a **GameObject** id — a *component* id fails "not found".
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

- **Next: `T17`** (VFX pool + core state visuals). **Reuse the T16 seam:** VFX reacts to the **same** surfaced
  round/spawner events via its **own sibling controller** — keep `FeedbackController` audio+haptics-only; do
  **not** route VFX through `AudioService` or widen `FeedbackCue` (guardrails §6: Audio/VFX/Haptics are peers).
  **Per-result RoundComplete timing** (GDD §12: 2/1.5/1 s) folds into T17 with the feedback content.
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
