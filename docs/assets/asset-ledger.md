# Asset Ledger

Last verified: 2026-06-02

Every third-party asset used in a build must have a row here. **Nothing enters a showable build without
a ledger row and a confirmed license.** Re-verify the license on the page at download time and, if the
distribution workflow requires it, include the license text in the repo. Record AI-generated assets too
(tool, prompt summary, date, rights note).

## Status

**Nothing imported yet.** The rows below are the GDD's recommended **CC0** set (§19), marked **PLANNED**
/ **OPTIONAL**. Fill in Download date / Local path / Modifications when each is actually imported, and
change the status.

| Asset | Use | Source | Author | License | Download date | Local path | Modifications | Status |
|---|---|---|---|---|---|---|---|---|
| Kenney Space Station Kit | Station bay walls, floor, sci-fi interior props | https://kenney.nl/assets/space-station-kit | Kenney | CC0 | — | — | — | PLANNED |
| Kenney Space Kit | Background planets, asteroids, small ships | https://kenney.nl/assets/space-kit | Kenney | CC0 | — | — | — | PLANNED |
| Kenney Particle Pack | Glow sprites, sparkles, beam / fizzle textures | https://kenney.nl/assets/particle-pack | Kenney | CC0 | — | — | — | PLANNED |
| Kenney Sci-fi Sounds | Reactor / energy / laser SFX | https://kenney.nl/assets/sci-fi-sounds | Kenney | CC0 | — | — | — | PLANNED |
| Kenney Interface Sounds | UI clicks / menu feedback | https://kenney.nl/assets/interface-sounds | Kenney | CC0 | — | — | — | PLANNED |
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
