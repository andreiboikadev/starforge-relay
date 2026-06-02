---
paths:
  - "Assets/**/*.cs"
---

# Unity C# Rules — Starforge Relay

Auto-loads when you touch C#. This is the operational checklist; full rationale and the code-review
checklist live in [implementation-guardrails.md](../../docs/architecture/implementation-guardrails.md).

## Architecture in one line

Thin Unity layer over testable plain-C# rules. MonoBehaviours are **adapters** (Unity lifecycle,
serialized scene refs, XRI interactors/interactables, prefabs, animation). Gameplay rules — score,
combo, heat, stabilization progress, round timer, port color validation, shard-spawn planning — are
**plain C# classes**: no `MonoBehaviour`, no `UnityEngine` statics inside them.

## Forbidden in runtime / gameplay code (needs a written justification to break)

- `GameObject.Find`, `FindWithTag`, `FindObjectOfType`, `FindObjectsOfType`, `FindAnyObjectByType`
- `GetComponent` inside `Update` or per-frame loops; LINQ inside per-frame gameplay loops
- `Camera.main` per frame (cache the XR camera reference)
- Any new singleton or service locator (`GameManager.Instance`, `AudioManager.Instance`, …)
- `SendMessage` / string-based dispatch; string event names
- `Resources.Load` for normal assets; `Instantiate`/`Destroy` for shards/beams during a round (pool)
- Legacy input (`Input.GetButton/GetAxis`) or direct `UnityEngine.XR` device polling — use XRI actions
- Locomotion / teleport providers (out of MVP scope)
- Avoidable managed allocations during an active round (reuse buffers; no `new WaitForSeconds` in loops)

## Allowed at setup time only

`GetComponent` in `Awake`/`Start`; `Instantiate` during pool warmup or one-time scene setup;
tags/layers/interaction layers for filtering (not repeated tag *searches*); `FindAnyObjectByType` only
in editor-only tooling.

## Dependency wiring — manual DI is the MVP default (no container)

- One composition root wires everything; constructor-inject plain C# services.
- MonoBehaviours receive scene/prefab refs via `[SerializeField]`, then pass deps to services.
- Runtime objects (shards, beams, VFX) get deps from a factory/pool on spawn — never by searching the scene.
- **VContainer is the only sanctioned container** if one is ever justified (Zenject/Extenject are
  abandoned). Do not add it without an ADR. Expected custom singletons for this MVP: **zero**.

## Pooling

Pool shards, insert beams, and short-lived VFX via `UnityEngine.Pool.ObjectPool<T>`. On release, reset
visual/color, collider + grab-interactable state, lifetime, event subscriptions, socket/grab state, and
parent transform; guard double-release in dev builds. Do **not** pool the single core, rings, the three
ports, or static station props.

## Events

Typed events only; small immutable payloads (`readonly struct`). Subscribe on enable, unsubscribe on
disable/dispose. Feature-scoped channels, not one global `EventBus`. For a one-to-one relationship,
prefer a direct method call over an event.

## XR — OpenXR + XR Interaction Toolkit 3.x (verify against the installed package before relying on an API)

- Exactly **one** XR Origin (VR) and **one** XR Interaction Manager in the scene. Tracking origin =
  **Floor** (standing room-scale). Cache the XR camera; never `Camera.main` per frame.
- **Grab shards:** a Direct Interactor (near grab) — or the XRI 3.x Near-Far Interactor — bound to
  **Grip**. Shards are `XRGrabInteractable` with **`throwOnDetach = false`** and kinematic/scripted
  movement (no ballistic flight). Released in empty space → scripted lerp back to the feeder pad.
- **Insert shards:** ports are `XRSocketInteractor`s that accept **any** shard (one shared "Shard"
  interaction layer). On select, validate color **in code** (`PortValidationService`): match → correct
  insert → beam into core → return to pool; mismatch → force-eject, raise `WrongInsert` (heat/combo),
  shard returns to its pad. **Do NOT gate the socket by color** (per-color Interaction-Layer mask or an
  `IXRSelectFilter`), or the wrong-insert event never fires and the heat mechanic dies silently.
- **World-space UI:** XR Ray Interactor + XR UI Input Module, select on **Trigger**.
- **Not in MVP:** hand tracking, passthrough, locomotion/teleport, multiple XR providers for Android.
- Render Mode = **Single Pass Instanced**; **Vulkan**; avoid stacked transparent overdraw across the HMD.

## Naming

`*Controller` (coordinates a feature) · `*Service` (reusable, no scene ownership) · `*Presenter`
(state → view) · `*View` (owns scene objects/visuals) · `*Config`/`*Definition` (ScriptableObject
data) · `*State` · `*EventChannel` · `*CompositionRoot`/`*Installer` (wiring only, no rules).
Avoid `GameManager`, `Manager`, `Utils`, `Helper`, `Data`. One public type per file (= filename);
PascalCase types/methods, camelCase fields.

## Tests

Pure rule classes don't inherit `MonoBehaviour` and don't read `Time`/`Random`/scene objects directly
(inject a clock and a seeded random — especially `ShardSpawnPlanner`).

**Per-mechanic gate (Definition of Done) — full detail in [guardrails §17](../../docs/architecture/implementation-guardrails.md):**
- **Unit tests in the same change** for new/changed pure rules (score, combo, heat, stabilization,
  timer, port validation, spawn planning). Adapter-only code with no pure-rule logic is covered by the
  smoke pass instead.
- **Smoke before done:** a Play Mode and/or **MCP-driven** pass in human-realistic conditions
  (XR Device Simulator / Meta XR Simulator; Quest device where tracking/controllers/grab feel matter).
- **No regression:** re-run the full existing EditMode suite (+ relevant smoke) and confirm earlier
  mechanics still pass before calling the work done.

## Size limits — refactor when crossed

A class over ~300 lines, or a method over ~50 lines with mixed responsibilities. No single script mixes
XR + UI + score + heat + spawn + audio + persistence.
