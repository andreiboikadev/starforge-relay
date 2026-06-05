# T09 — Ports as accept-any sockets → validate colour in code

| | |
|---|---|
| Branch | `feature/port-socket` |
| Milestone | M2 — VR slice |
| Design ref | GDD §10 (Reactor Ports), §12 (correct/wrong insert); guardrails §6 (Reactor Core & Ports — **critical "don't color-gate" note**), §13 (XRI: sockets accept any, validate in code), §16 (ownership); [ADR 0001](../architecture/adr/0001-tech-baseline.md) |
| Depends on | T04 (`PortValidationService`, `InsertOutcome`), T08 (`Shard.prefab`, `ShardView.Color`) |
| Touches scenes/prefabs | yes — new `Port.prefab` + **3 port instances committed to the scene**. No `Shard.prefab` / rig changes (uses the **Default** interaction layer; the named "Shard" layer is deferred to T14). |
| Status | 🟡 brief — implement on command |

## Goal
The colour-matching mechanic. Each of the 3 reactor ports is an `XRSocketInteractor` that accepts **any**
shard, then validates colour **in code** (`PortValidationService`, T04): match → correct (accept), mismatch →
**force-eject** + wrong outcome. This is the project's key XR decision (guardrails §6 critical note) — never
colour-gate the socket, or the wrong-insert path dies silently. T09 produces port-level correct/wrong
outcomes; wiring them to scoring/heat (`RoundController`) + the rich `CorrectInsert`/`WrongInsert` events is T11.

## Architecture note (first XRI in the Runtime assembly)
T09 is where XRI legitimately enters `StarforgeRelay.Runtime` (`PortSocket` wraps `XRSocketInteractor`) — the
ref deferred from T08, per the accepted single-asmdef baseline (ADR 0001 / guardrails §4; no new assembly).
Pure rules (score/heat/timer/validation/spawn) stay `MonoBehaviour`-free and free of `UnityEngine` statics **by
discipline**. Because adding XRI removes the compile-time "no XR in pure rules" guard, the pre-handoff
restricted-API grep is **extended** to also flag `Interaction.Toolkit` usage outside the designated adapter
files (see Verification).

## Acceptance criteria
- **asmdef:** `StarforgeRelay.Runtime.asmdef` references `Unity.XR.Interaction.Toolkit` (by name). Pure rules unchanged.
- **Interaction layer = Default (shared):** the Shard (Default, from T08) and each `PortSocket` (Default) match,
  so the socket accepts **any** shard; the rig's Near-Far interactors already include Default (verified T08), so
  grab is unaffected. This satisfies guardrails §13 "one shared layer, not colour-gated". *(The dedicated named
  "Shard" layer + rig interaction-mask updates are deferred to **T14**, where separating shard-grab from the UI
  ray actually matters — introduce earlier only if the smoke shows Poke/other interactors grabbing shards on Default.)*
- **`PortSocket`** (MonoBehaviour adapter; wraps `XRSocketInteractor`):
  - Serialized `ShardColor _portColor`.
  - Accepts **any** shard (no per-colour select filter, no colour-gated layer). On the socket's `selectEntered`,
    reads `ShardView.Color`, calls `PortValidationService.Validate(shardColor, _portColor)`:
    - `Correct` → accept; raise a typed port-level **correct** signal. Shard stays socketed (consume→pool is T11;
      do **not** deactivate a socket-held shard here — it confuses the socket).
    - `Wrong` → **eject** (sequence below); raise a typed port-level **wrong** signal.
  - Yields **only Correct or Wrong**: the socket fires only when a shard is actually inside it, so `Validate`
    always gets a non-null port colour. (`NoPenalty` / "dropped outside any port" is the shard's empty-drop path
    → T10 lerp-back; it is **never** a port event — don't look for the port to emit it.)
  - **Does NOT** compute score/heat/combo or raise `RoundController`'s `CorrectInsertEvent`/`WrongInsertEvent`
    (those carry combo/heat/stabilization — owned by RoundController, wired T11). T09 emits only the port-level
    outcome (e.g. `event Action<PortSocket, ShardView, InsertOutcome>` or a small `PortInsert` readonly struct).
- **Eject sequence (Wrong) — the trap to get right:** after a force-exit the shard still sits inside the socket
  trigger, so the socket auto-re-selects → re-validates → re-ejects (flicker loop). Break it:
  1. **Release** the socket's hold via `socketActive = false` (or `allowSelect = false`) — both non-obsolete in 3.3.0.
  2. **Push the shard out** of the socket volume (scripted displacement; the real return-to-pad replaces this in T10).
  3. **Re-enable** the socket once the shard has left the trigger.
  4. **Guard** against re-enter+re-reject in a tight loop.
  **`XRInteractionManager.SelectExit` is obsolete in 3.3.0 — do not use it.** Confirm the exact non-obsolete
  release call (`socketActive`/`allowSelect`, or a current overload) via `unity_reflect get_member` at impl, and
  verify "no loop" in the smoke.
- **`PortView`** (minimal): primitive port visual + colour tint (MPB `_BaseColor`, as `ShardView`). Real shape
  markers (circle/triangle/diamond) + glow = T18–19.
- **`Port.prefab`** (`Prefabs/Gameplay/Port.prefab`): primitive + `XRSocketInteractor` (accept-any, Default layer,
  **oversized/forgiving** trigger volume — GDD §10) + `PortSocket` + `PortView`. **Not pooled** (guardrails §12).
- **3 port instances committed to the scene** (Solar / Ion / Pulse) in the comfortable forward arc at chest
  height (GDD §9), each `_portColor` + colour set. (The reactor core they sit on is T10; positions may be refined
  when it lands.)
- **Behaviour:** matching-colour shard socketed → **correct/accepted**; wrong-colour shard socketed → **pushed
  back out (ejected), no flicker/loop**, retry possible; dropped outside any port → no port event.

## Implementation notes
- **New:** `Scripts/Gameplay/PortSocket.cs`, `Scripts/Gameplay/PortView.cs` (+ maybe a `PortInsert` payload),
  `Prefabs/Gameplay/Port.prefab`, a port material. **Edit:** `StarforgeRelay.Runtime.asmdef` (+XRI), the scene
  (3 ports, **saved/committed**). **No `Shard.prefab` / rig changes** (Default layer).
- **Verified vs installed XRI 3.3.0:** `XRSocketInteractor` ∈ `…Interaction.Toolkit.Interactors` (assembly
  `Unity.XR.Interaction.Toolkit`), base `XRBaseInteractor`/`IXRSelectInteractor` → `selectEntered`/`selectExited`
  (current; `onSelect*` obsolete). Release levers (non-obsolete): `socketActive` (`XRSocketInteractor`),
  `allowSelect`/`keepSelectedTargetValid` (`XRBaseInteractor`). Tuning: `socketSnappingRadius`,
  `showInteractableHoverMeshes`, `recycleDelayTime`. **`XRInteractionManager.SelectExit` is obsolete** — avoid.
  `InsertOutcome` = Correct/Wrong/NoPenalty (T04); `PortValidationService.Validate(ShardColor, ShardColor?)` is the rule.
- **Critical (guardrails §6):** accept any shard, validate colour in `PortValidationService` on select; do **not**
  use a per-colour interaction-layer mask or an `IXRSelectFilter` returning `false` for the wrong colour — that
  silently kills the wrong-insert event + heat mechanic.
- **MCP discipline (from T08):** `manage_gameobject create` may not apply `component_properties` — set via
  `manage_components set_property` + **re-read to verify**. `refresh scope=all` for new files.
- **Style:** C# 9 (block namespace, no `record`/`init`), no `#nullable enable`, `_camelCase` fields, `sealed`,
  XML docs — as M1/T08.

## Out of scope
- Wiring port outcomes to `RoundController` (score/heat/combo/progress) + the rich `CorrectInsert`/`WrongInsert`/
  `ComboMilestone` events → **T11**.
- Reactor core visual + feeder pads + spawner + lerp-back-to-pad → **T10** (T10 also replaces the Wrong-eject
  push-out with the real return-to-pad).
- Named "Shard" interaction layer + rig-mask separation → **T14** (UI).
- Beam VFX (port→core), spark/reject VFX, combo-pulse → **T17**; audio/haptics → **T16**; shape markers/glow → **T18–19**.
- Consuming an accepted shard back into the pool (correct insert) — minimal/none in T09; the consume→beam→pool
  flow lands with T10/T11/T17.

## Verification
- **Tests:** no new pure rule — `PortValidationService` (correct/wrong/no-penalty) is already covered by T04
  EditMode tests; the full suite **stays green at 75/75** this session (the +XRI asmdef change must not regress
  compilation/tests). Any pure helper T09 adds ships EditMode tests in-change.
- **Restricted-API:** `rg` over `Assets/_Project` for the standard forbidden set (guardrails §10), **plus** flag
  `Interaction\.Toolkit` usage outside the adapter files (`PortSocket`/`PortView`/`ShardView`/`ShardPool`) — guards
  XR creeping into pure rules now that `Runtime` references XRI.
- **Smoke (human, XR Device Simulator / Quest):** place the 3 ports + a few `Shard` instances (T08 prefab,
  transient); socket a **matching** shard → accepted; socket a **wrong** shard → **ejected, no flicker/loop**,
  retry possible; drop outside → nothing; **grab still works**. Console clean (sim-haptic noise excepted, per T08).
- **Done =** full EditMode suite green at 75/75 this session + restricted-API (incl. the XRI-outside-adapters
  check) clean + the correct-accept / wrong-eject-no-loop smoke passes + no Console errors (sim-haptic excepted);
  then close docs (brief + matrix + current-status).

## What was actually done
— (filled on close)
