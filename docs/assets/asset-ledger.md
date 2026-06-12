# Asset Ledger

Last verified: 2026-06-11

Every third-party asset used in a build must have a row here. **Nothing enters a showable build without
a ledger row and a confirmed license.** Re-verify the license on the page at download time and, if the
distribution workflow requires it, include the license text in the repo. Record AI-generated assets too
(tool, prompt summary, date, rights note).

## Status

**First imports landed (T16, 2026-06-11): Kenney Sci-fi Sounds + Interface Sounds** (the audio feedback
layer). The remaining rows are the GDD's recommended **CC0** set (§19), still **PLANNED** / **OPTIONAL**.
Fill in Download date / Local path / Modifications when each is actually imported, and change the status.

| Asset | Use | Source | Author | License | Download date | Local path | Modifications | Status |
|---|---|---|---|---|---|---|---|---|
| Kenney Space Station Kit | Station bay walls, floor, sci-fi interior props | https://kenney.nl/assets/space-station-kit | Kenney | CC0 | 2026-06-12 | `Assets/ThirdParty/Kenney Space Station Kit/` | **FBX format only** (redundant OBJ/GLB formats + `Previews/` removed); bay meshes overridden with the dark `BayMetalDark` material | ✅ IMPORTED |
| Kenney Space Kit | Background props (evaluated, not used) | https://kenney.nl/assets/space-kit | Kenney | CC0 | 2026-06-12 | — (removed) | Imported then **removed** (T18): all 13 FBX *identifier-uniqueness* import errors originated here, and its content is irrelevant to a single reactor bay. Background uses a dark star-dome + Particle stars (GDD §19); re-add a curated few props only if a future task needs them | ❌ REMOVED |
| Kenney Particle Pack | Glow / halo / star sprites | https://kenney.nl/assets/particle-pack | Kenney | CC0 | 2026-06-12 | `Assets/ThirdParty/Kenney Particle Pack/` | Full PNG sets imported; the fake-glow halo/VFX that reference them land at **T19** (not yet referenced at T18) | ✅ IMPORTED |
| iPoly3D Crystal Pack | Energy shard / crystal meshes (grabbable pickups) | https://poly.pizza/bundle/Crystal-Pack-AywAG7aywi | iPoly3D | CC0 | 2026-06-12 | `Assets/ThirdParty/iPoly3D Crystal Pack/` | 28 low-poly FBX crystals; pack shipped **no license file** → added `License.txt` (CC0 confirmed on the Poly Pizza listing + the CC0 1.0 deed). Folder is as-downloaded (1 `Crystal/` folder + 8 per-model hash subfolders). Shard-mesh integration (one crystal on the Unlit-HDR `Shard.mat`, tinted via `_BaseColor` MPB — no mesh/texture edit) is the T19 reskin. | ✅ IMPORTED |
| Kenney Sci-fi Sounds | Reactor / energy / laser SFX | https://kenney.nl/assets/sci-fi-sounds | Kenney | CC0 | 2026-06-11 | `Assets/ThirdParty/Kenney Sci-fi Sounds/` | None; full Audio pack imported, **3** clips referenced (RoundStart / ShardExpired / Overload) | ✅ IMPORTED |
| Kenney Interface Sounds | UI clicks / menu feedback | https://kenney.nl/assets/interface-sounds | Kenney | CC0 | 2026-06-11 | `Assets/ThirdParty/Kenney Interface Sounds/` | None; full Audio pack imported, **7** clips referenced (UiSelect / Grab / Release / Correct / Wrong / ComboMilestone / Victory) | ✅ IMPORTED |
| Kenney Modular Space Kit | Extra modular station pieces, rails, panels | https://kenney.nl/assets/modular-space-kit | Kenney | CC0 | — | — | — | OPTIONAL |
| Quaternius Ultimate Space Kit | Backup / alternate space props | https://quaternius.com/packs/ultimatespacekit.html | Quaternius | CC0 | — | — | — | OPTIONAL |
| Magic Spell SFX | Energy pickup / stabilization sweeteners | https://opengameart.org/content/magic-spell-sfx | JaggedStone | CC0 | — | — | — | OPTIONAL |
| 80 CC0 RPG SFX | Backup short magic / item sounds | https://opengameart.org/content/80-cc0-rpg-sfx | rubberduck | CC0 | — | — | — | OPTIONAL |
| Poly Haven HDRIs | Optional starry skybox / reflection source | https://polyhaven.com/ (license: https://polyhaven.com/license) | Poly Haven | CC0 | — | — | — | OPTIONAL |

## Notes

- **Recommended MVP set:** Kenney Space Station Kit (interior) + Space Kit (background props) + Particle
  Pack (fake glow / VFX) + Sci-fi Sounds (gameplay SFX) + Interface Sounds (UI). Skip the rest until the
  core loop is playable.
- Prefer one low-poly visual family (Kenney first); add Quaternius only after scale/materials are normalized.
- Core, shards, ports, and beams can be **primitives + emissive materials + halo billboards** — fake glow
  over URP bloom (GDD §17 / guardrails §18). Don't block the prototype on perfect art.
- Downscale large textures / HDRIs for Quest; keep unique materials and texture sizes low.
- **T16 audio (2026-06-11):** both sound packs are CC0 (Kenney). The full `Audio/` folders were dropped
  under `Assets/ThirdParty/…`; only the 10 clips above are referenced by `AudioCueConfig` (unreferenced
  clips are stripped from the build). Clip picks are first-pass — swappable in the `AudioCueConfig`
  Inspector; final mix/levels are a T21 tuning pass. Keep each pack's `License.txt` alongside the audio.
  **Git LFS — still pending** (the `.gitattributes` plan; T18 closed without running it) — these `.ogg` commit as plain binary.
- **T18 visual imports (2026-06-12):** Space Station Kit + Particle Pack imported under `Assets/ThirdParty/…`
  (both CC0, `License.txt` kept). SSK trimmed to **FBX only** (OBJ/GLB/Previews removed) and re-skinned dark
  via `BayMetalDark` for the GDD's dark-station look. **Space Kit removed** (see its row). **Git LFS migrate
  still pending** — the T18 art committed as plain binary; run the migrate when the art settles (recipe in
  the handoff). Final glow/halo on the interactables + the halo-billboard prefab are **T19**.
