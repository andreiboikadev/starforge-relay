# T05 — ShardSpawnPlanner (seeded RNG)

| | |
|---|---|
| Branch | `feature/shard-spawn-planner` |
| Milestone | M1 — Pure rules |
| Design ref | GDD §12 (spawn rules), §9 (reach zone); guardrails §16–17 |
| Depends on | T01 (RoundConfig + ShardColor) |
| Touches scenes/prefabs | no |
| Status | ▫ not started |

## Goal

The pure planner that decides **what color spawns on which free pad, and when** — the most logic-heavy
rule, so it must be fully testable with a seeded random source (no `UnityEngine.Random`).

## Acceptance criteria

- **`ShardSpawnPlanner`** (plain C#) maintains the active target count (`activeShardsDefault=4`) and:
  - never more than **3 shards of one color** active at once;
  - keeps **at least 2 different colors** active;
  - **prefers the under-represented color** when choosing the next spawn;
  - returns a **respawn delay within 0.3–0.8 s** after an accept/expire;
  - **one shard per pad** — a reserved pad (idle / grabbed / returning) does not double-spawn;
  - never returns a spawn slot **behind the player, outside the −50…+50° front arc, or outside the height
    band** (slots come from `ReactorConfig`-style data, validated here).
- Randomness is injected behind an interface / seeded provider (`IRandom` or a passed seed) — **no
  `UnityEngine.Random`** inside the class.
- **EditMode tests** (with a seeded/fake random): target count maintained; color cap (≤3) never exceeded;
  ≥2 colors always present; under-represented color preferred; delay always in [0.3, 0.8]; reserved pad not
  reused; out-of-arc / behind-player slots never returned.

## Implementation notes

- `Assets/_Project/Scripts/Gameplay/`; feed pad slots + arc/height bounds in as plain data (don't read the
  scene). The active-shard escalation to 5/6 is **out of scope** (MVP keeps 4; only add if extra feeder
  slots exist — GDD §12).
- The `ShardSpawner` MonoBehaviour that turns a plan into pooled prefabs is T10.

## Out of scope

- Actual spawning / pooling / scene positions (T10); shard lifetime ticking and expiry (shard view, M2);
  the 5→6 active-shard escalation (post-MVP per GDD).

## Verification

- Tests: the EditMode fixtures above (seeded RNG → deterministic).
- Done = full EditMode suite green + no Console errors.

## What was actually done

—
