# ADR 0001: Technical baseline

Status: Accepted
Date: 2026-06-02
Decision owner: Project owner

## Context

Starforge Relay is a short VR MVP for Meta Quest 2/3 (see [game design](../../product/game-design.md)).
The Unity project was scaffolded and the XR engine setup (Part B of the setup plan) completed and
**verified on a Quest 2** before gameplay work began. This ADR records the baseline that is **already in
place and accepted**, so future sessions don't re-decide it. Versions here are the source of truth;
cross-check against `Packages/manifest.json` and `ProjectSettings/ProjectVersion.txt`.

## Decision

- **Engine:** Unity **6.3 LTS** (`6000.3.16f1`). Use this exact version.
- **Render pipeline:** **URP** (Universal). Mobile and PC renderer/asset variants under
  `Assets/Settings/`; the Quest build uses the mobile URP asset.
- **XR stack:** **OpenXR 1.16** + **XR Interaction Toolkit 3.3.0** (xr.management 4.5.4, xr.core-utils
  2.6.0), target **Android / Meta Quest 2/3**. OpenXR provider enabled for Android with **Meta Quest
  Support**; **Oculus Touch Controller** interaction profile; **XRI Starter Assets** rig.
- **Stereo / graphics:** Render Mode = **Single Pass Instanced** (delivered as Multiview on Quest);
  **Vulkan** only (no OpenGLES3); **IL2CPP** + **ARM64**; min API **32 (Android 12L)**, target API **34**;
  Texture Compression **ASTC**; Color Space **Linear**.
- **Input:** new **Input System** via XRI actions (XRI Default Input Actions). No legacy `Input`.
- **Editor automation:** **MCP for Unity** (CoplayDev) for GameObject/component/scene/material/Test
  Runner/Console operations.
- **Editor VR testing:** **XR Device Simulator** (XRI Samples) and/or **Meta XR Simulator**; **Quest
  Link** to run from the Editor in the headset.
- **Dependency injection:** **manual DI** (composition root + constructor injection + serialized refs +
  factories/pools). **No container** for MVP. VContainer is the only sanctioned container if one is
  later justified — via its own ADR (Zenject/Extenject rejected as abandoned).
- **Scenes:** a **single** `Assets/_Project/Scenes/SampleScene.unity` for the MVP (the only build scene), with one
  XR Origin rig (tracking origin **Floor**) and one XR Interaction Manager.
- **Pooling:** `UnityEngine.Pool.ObjectPool<T>` for shards, insert beams, and repeated VFX.

## Consequences

- Easier: no DI-container learning curve or reflection cost; one scene to reason about; in-Editor VR
  iteration via the XR Device Simulator / Quest Link keeps the loop fast without a full build.
- Constrained: gameplay must stay testable as plain C# (manual DI demands explicit wiring — a feature,
  not a bug); adopting a DI container or splitting scenes later requires a new ADR.
- **Interaction note (guardrails §6):** ports must accept any shard and validate color **in code** (not a
  color-gated socket filter), or wrong-insert events never fire. No ballistic throwing; no locomotion in
  MVP — the Starter Assets rig's Locomotion branch will be stripped during implementation.

## Follow-Up

- Strip the Starter Assets Locomotion providers from the rig at the first interaction slice.
- Revisit manual DI if the service graph grows non-trivially beyond the MVP (would be ADR 0002).
- Revisit single-scene if additive loading becomes worthwhile.
- Record the chosen grab interactor (Direct vs Near-Far) once the grab slice is implemented and tested.
