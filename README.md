# Starforge Relay

A small, polished **stationary VR arcade** for **Meta Quest 2 / 3**, built in Unity (URP)
with OpenXR + the XR Interaction Toolkit. You stand at a space-station reactor, grab glowing
energy **shards** with the Touch controllers, and insert each into the matching-coloured
**port** to stabilise a mini-star before a 90-second timer runs out or the heat meter maxes.

> Working title. Commercial product, in active development. Source-available under the [PolyForm Strict License](LICENSE) — not open-source.

## Status

A **playable vertical slice** runs in the editor (XR Device Simulator / Quest Link):

- **Gameplay rules — complete, unit-tested:** scoring, combo, heat, stabilisation, round
  timer, colour-match validation, shard-spawn planning, end-states (victory / overload /
  time-out) + star rating — **85 EditMode tests, green**.
- **VR slice — complete:** grab on Grip → insert into a port → correct / wrong / **expired**
  feedback → win / overload / time-out, with pooled shards, feeder-pad spawning, shard
  lifetime/expiry, and scripted return-to-pad (no ballistic throwing).
- **Wiring — in progress:** one composition root wires the object graph; an app/round state
  machine and world-space UI are next, then audio/VFX polish and on-device tuning.

The engine baseline is verified on a real Quest 2; the gameplay slice is exercised in-editor
(on-device validation of the full slice is a later milestone). Detailed live state:
[docs/handoff/current-status.md](docs/handoff/current-status.md).

## Engineering approach

A **thin Unity/XR layer over testable, plain-C# gameplay rules** — the core logic runs
headless in unit tests, no headset required:

- **Pure rules** (score, combo, heat, stabilisation, timer, validation, spawn planning) are
  `MonoBehaviour`-free and dependency-injected — covered by EditMode tests.
- **Thin adapters** own only Unity/XRI concerns (grab/socket interaction, pooling, motion)
  and publish typed events to the rules.
- **Manual dependency injection** through a single composition root — no singletons, no
  scene-wide lookups in hot paths.
- **VR-performance discipline:** object pooling, near-zero per-frame allocation in a round,
  Single Pass Instanced, fake-glow over heavy post-processing (Quest 2 is the baseline).

Design: [docs/product/game-design.md](docs/product/game-design.md) ·
engineering contract: [docs/architecture/implementation-guardrails.md](docs/architecture/implementation-guardrails.md).

## Tech

- **Unity 6.3 LTS** (`6000.3.16f1`), **URP**, Android / Meta Quest target.
- **OpenXR 1.16 + XR Interaction Toolkit 3.3** (Oculus Touch), new Input System, IL2CPP /
  ARM64, Vulkan, Single Pass Instanced.

## Build & run

- **Requires** Unity 6.3 LTS with Android Build Support; a Quest 2/3 in Developer Mode for a
  device build, or the XR Device Simulator for in-editor play.
- Open the project, open `Assets/_Project/Scenes/StarforgeRelay.unity`, then **Play**
  (XR Device Simulator / Quest Link) or **Build And Run** to a Quest.
- Exact commands: [docs/development/build-and-test.md](docs/development/build-and-test.md).

## Documentation

Documentation-driven; start at [docs/INDEX.md](docs/INDEX.md) — design, architecture,
build/test, the task plan, and current state.

## License

Licensed under the **[PolyForm Strict License 1.0.0](LICENSE)** — a standardised,
IP-lawyer-drafted source-available license. You may view and study this project for
**noncommercial** purposes; **commercial use, redistribution, and derivative works require the
copyright holder's written permission**. © 2026 Andrei Boika. Third-party components (Unity
packages; any assets per [docs/assets/asset-ledger.md](docs/assets/asset-ledger.md)) remain
under their own licenses.
