# Starforge Relay

## What this is

Starforge Relay is a small **stationary VR arcade** built in Unity for **Meta Quest 2/3**. The player
stands at a space-station reactor, grabs glowing energy **shards** with the Touch controllers, and
inserts each into the matching colored **port** to stabilize a mini-star before a 90-second timer runs
out or the heat meter maxes. Tense-but-cozy, readable, one round at a time. *(Working title — re-check
name availability before any public release.)*

## Current status

**Baseline only.** Unity + URP + OpenXR + XR Interaction Toolkit are configured for Quest, with a
`SampleScene` that contains an XR Origin rig and **no gameplay code yet**. Verified building and
launching on a Quest 2. Latest detailed state:
[docs/handoff/current-status.md](docs/handoff/current-status.md).

## Requirements

- **Unity 6.3 LTS** (`6000.3.16f1`) — use this exact version.
- Unity modules: **Android Build Support** (incl. OpenJDK + Android SDK & NDK).
- Target device: **Meta Quest 2 or 3** (standalone, Android / Horizon OS) in Developer Mode.
- Editor testing without a headset: **XR Device Simulator** (XRI Samples) or **Meta XR Simulator**.
- Optional: **Meta Quest Link** (run from the Editor in the headset), **MCP for Unity** server (editor automation).

## Quick start

1. Open this folder in Unity 6.3 LTS via Unity Hub; let it restore packages on first open.
2. Open `Assets/Scenes/SampleScene.unity`.
3. **Test on device (the validated path):** make sure the active platform is **Android**
   (`File → Build Profiles → Switch Platform` — the active target is *not* version-controlled), then
   **Build And Run** to a Quest in Developer Mode.
4. **Faster in-Editor iteration:** OpenXR is enabled for **both Android and Standalone**, so with
   **Meta Quest Link** running (active OpenXR runtime = Meta) you can press **Play** and the scene
   renders in the headset; the **XR Device Simulator** (XRI Samples) also works without a headset. See
   [docs/development/build-and-test.md](docs/development/build-and-test.md).

## Build and test

See [docs/development/build-and-test.md](docs/development/build-and-test.md) — the single source of
truth for commands.

## Documentation

Start at [docs/INDEX.md](docs/INDEX.md).

## AI-assisted development

Claude Code must read [CLAUDE.md](CLAUDE.md) and
[docs/handoff/current-status.md](docs/handoff/current-status.md) before editing. Hard rules (no git
writes, no secret/keystore reads) are enforced in `.claude/settings.json`; C# architecture rules
auto-load from `.claude/rules/`.
