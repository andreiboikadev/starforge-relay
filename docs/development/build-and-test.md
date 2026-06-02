# Build & Test

Last verified: 2026-06-02

The **single source of truth for commands**. Don't duplicate these elsewhere — link here. All commands
assume the repo root (this folder) as the working directory and Unity **6.3 LTS (`6000.3.16f1`)**.

## Who does what

- **Assistant (Claude Code):** edits C#/assets, drives the Editor via the **MCP for Unity** server
  (create/modify GameObjects, components, scenes, materials; run the Test Runner; read the Console),
  and edits `Packages/manifest.json` to add packages. Read-only git only.
- **Human:** platform switch, parts of Player/OpenXR Settings, URP renderer feature, **Graphics API
  selection**, device build & run, Quest Link, and all git commits. These are unreliable or impossible via MCP.

## Compile / sanity check (every change)

Via MCP: trigger a domain reload / script compile and **read the Console** — "ready for review" means
**no compile errors and no Console errors**, and **Project Validation** passes (Project Settings →
XR Plug-in Management → Project Validation → **Android** tab; fix any OpenXR / Meta Quest issues it flags).

## Run tests

Pure gameplay logic is **EditMode** (no headset needed). Prefer the **MCP Test Runner** inside the open
Editor. Headless CLI alternative (close the Editor or use a second Unity instance):

```
<UnityEditor>\Unity.exe -runTests -batchmode -projectPath . ^
  -testPlatform EditMode -testResults .\TestResults-EditMode.xml
```

- `<UnityEditor>` = the 6000.3.16f1 editor (here: `D:\Soft\Unity\Editor\6000.3.16f1\Editor\Unity.exe`).
- `-testPlatform PlayMode` for Play mode tests. **Run a fresh Unity process per PlayMode run** (a known
  CLI quirk prevents running PlayMode tests twice in one invocation).
- Results are NUnit XML at the path given. Never claim a test passed without running it this session or
  citing exact evidence.

## Test VR in the Editor (no headset) — simulators

> **Setup note (important):** the in-Editor headset paths below (simulators **and** Quest Link) need
> OpenXR enabled for the **Standalone** target in XR Plug-in Management. So far only the **Android**
> target has OpenXR enabled (Part B), so until Standalone is enabled the **validated path is a device
> Build And Run** (below). Enabling Standalone OpenXR is a quick follow-up for fast iteration.

- **XR Device Simulator** (XRI Samples → import "XR Device Simulator"): keyboard/mouse drive the HMD +
  controllers in Play mode. The fast loop for grab / socket / UI interaction logic, no extra runtime.
- **Meta XR Simulator** (optional): emulates a Quest at the OpenXR API level for Quest-specific checks
  and automation; needs no extra Unity packages when the project is on OpenXR.

## Run in the headset via Quest Link (fast, no build)

1. Start **Meta Quest Link** on the PC; enable Link in the headset; set the active **OpenXR runtime =
   Meta** (Meta Quest Link → Settings → General).
2. Press **Play** in the Editor — the scene renders in the headset with real 6DoF + controllers.

## Build & run on device (human step)

Android **Player/OpenXR config is committed** (Vulkan, IL2CPP, ARM64, min API 32 / target API 34,
package id — see [ADR 0001](../architecture/adr/0001-tech-baseline.md)). The **active build target and
texture compression are editor-local** (in `Library/`, not version-controlled), so on a fresh clone
switch platform first.

1. Switch the active platform to **Android** if needed: `File → Build Profiles → Android → Switch
   Platform` (Texture Compression = **ASTC**).
2. Connect a Quest in Developer Mode over USB; confirm it is visible: `adb devices`.
3. `File → Build Profiles → Build And Run`. The only build scene is `Assets/Scenes/SampleScene.unity`.
4. Verify on device: stereo render, head 6DoF, controllers track. Inspect the OpenXR session / app
   process via logcat if needed: `adb logcat -d -s Unity` (look for `XR_SESSION_STATE_FOCUSED`).

## Restricted-API check (before handoff)

Search first-party runtime code for forbidden APIs (see
[guardrails §10](../architecture/implementation-guardrails.md)). Use the Grep tool, or `rg`:

```
rg -n "GameObject\.Find|FindWithTag|FindObjectOfType|FindObjectsOfType|FindAnyObjectByType|SendMessage|Resources\.Load|Camera\.main|Input\.Get" Assets/_Project
```

Any match in runtime code must be removed or justified in review.

## Per-mechanic gate: regression + smoke (see [guardrails §17](../architecture/implementation-guardrails.md))

Before a mechanic is "done":
- **Regression:** run the **full** EditMode suite (command above) — all green; confirm earlier mechanics
  still pass. For PlayMode, a fresh Unity process per run.
- **Smoke (human-realistic):** drive the actual flow and read the Console — via the **MCP** Test Runner /
  Play controls under the **XR Device Simulator** (or Meta XR Simulator), and a **Quest device / Quest
  Link** pass where tracking, controllers, or grab / socket feel matter. Use MCP only where it can
  realistically drive the case.

## What "done" means / what can't be automated yet

- **Mechanic done:** new pure rules have EditMode tests; the **full suite re-runs with no regression**;
  a **smoke pass** in human-realistic conditions passed (MCP / simulator, Quest where needed); no
  Console errors.
- **VR slice done in Editor:** grab → socket → correct/wrong/expired feedback works under the XR Device
  Simulator or Quest Link.
- **Demo-ready:** validated on a real **Quest 2** (the performance baseline) — see the demo bar in the
  game-design doc; this cannot be automated, it's a human device test.
