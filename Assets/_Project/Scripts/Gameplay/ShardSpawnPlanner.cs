using System.Collections.Generic;

namespace StarforgeRelay.Gameplay
{
    /// <summary>
    /// Decides what shard color spawns on which free pad, and the delay before a replacement (GDD §12 spawn
    /// rules, §9 reach zone). Pure C#: no <c>MonoBehaviour</c>, no Unity statics, randomness via an injected
    /// <see cref="IRandom"/> — fully headless-testable (guardrails §16–17). Owns the active-shard bookkeeping
    /// (one shard per pad, per-color counts); turning a <see cref="ShardSpawnPlan"/> into a pooled shard at
    /// the pad transform is the spawner adapter (T10). MVP keeps the target fixed — no 5/6 escalation.
    /// </summary>
    public sealed class ShardSpawnPlanner
    {
        private readonly List<FeederPadSlot> _validSlots = new();
        private readonly Dictionary<int, ShardColor> _occupied = new();
        private readonly List<ShardColor> _colors;
        private readonly int _activeTarget;
        private readonly int _maxShardsPerColor;
        private readonly float _respawnDelayMin;
        private readonly float _respawnDelayMax;
        private readonly IRandom _random;

        // Reusable scratch buffers. Planning is event-driven (on accept/expire), not a per-frame path, but
        // keeping the lists off the hot call costs nothing and avoids per-spawn allocations (guardrails §10).
        private readonly List<FeederPadSlot> _freeBuffer = new();
        private readonly List<ShardColor> _candidateBuffer = new();

        /// <param name="slots">Predefined feeder-pad slots; any outside <paramref name="area"/> are dropped and never used.</param>
        /// <param name="area">Comfortable reach zone (arc + height band) every returned slot must fall inside (GDD §9).</param>
        /// <param name="colors">The shard colors in play (GDD §12: Solar / Ion / Pulse).</param>
        /// <param name="activeShardsTarget">Active shards to maintain (GDD: 4). Injected from RoundConfig.</param>
        /// <param name="maxShardsPerColor">Cap of one color active at once (GDD: 3). Injected from RoundConfig.</param>
        /// <param name="respawnDelayMin">Min replacement delay in seconds (GDD: 0.3). Injected from RoundConfig.</param>
        /// <param name="respawnDelayMax">Max replacement delay in seconds (GDD: 0.8). Injected from RoundConfig.</param>
        /// <param name="random">Randomness seam — seeded in production, a fake in tests.</param>
        public ShardSpawnPlanner(
            IReadOnlyList<FeederPadSlot> slots,
            SpawnArea area,
            IReadOnlyList<ShardColor> colors,
            int activeShardsTarget,
            int maxShardsPerColor,
            float respawnDelayMin,
            float respawnDelayMax,
            IRandom random)
        {
            _colors = new List<ShardColor>(colors);
            _activeTarget = activeShardsTarget;
            _maxShardsPerColor = maxShardsPerColor;
            _respawnDelayMin = respawnDelayMin;
            _respawnDelayMax = respawnDelayMax;
            _random = random;

            for (int i = 0; i < slots.Count; i++)
            {
                if (area.Contains(slots[i]))
                {
                    _validSlots.Add(slots[i]);
                }
            }
        }

        /// <summary>Shards currently active (reserved pads).</summary>
        public int ActiveCount => _occupied.Count;

        /// <summary>True when the active target is met — no more spawns until a pad frees.</summary>
        public bool IsAtTarget => _occupied.Count >= _activeTarget;

        /// <summary>How many active shards are currently <paramref name="color"/>.</summary>
        public int CountOfColor(ShardColor color)
        {
            int count = 0;
            foreach (ShardColor active in _occupied.Values)
            {
                if (active == color)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Plan the next spawn: an under-represented color (never over the per-color cap) on a random free,
        /// in-reach pad. Returns <c>false</c> when the target is met, no pad is free, or every color is capped
        /// — the caller waits and retries. Reserves the chosen pad until <see cref="Release"/>.
        /// </summary>
        public bool TryPlanNextSpawn(out ShardSpawnPlan plan)
        {
            plan = default;

            if (_occupied.Count >= _activeTarget)
            {
                return false;
            }

            _freeBuffer.Clear();
            foreach (FeederPadSlot slot in _validSlots)
            {
                if (!_occupied.ContainsKey(slot.PadId))
                {
                    _freeBuffer.Add(slot);
                }
            }

            if (_freeBuffer.Count == 0)
            {
                return false;
            }

            if (!TryPickUnderRepresentedColor(out ShardColor color))
            {
                return false;
            }

            FeederPadSlot pad = _freeBuffer[_random.NextInt(0, _freeBuffer.Count)];
            _occupied[pad.PadId] = color;
            plan = new ShardSpawnPlan(pad.PadId, color);
            return true;
        }

        /// <summary>A replacement delay in [respawnDelayMin, respawnDelayMax] after an accept/expire (GDD §12).</summary>
        public float NextRespawnDelay() => _random.NextFloat(_respawnDelayMin, _respawnDelayMax);

        /// <summary>Free a pad when its shard is accepted or expires, so a replacement can spawn there (GDD §12).</summary>
        public void Release(int padId) => _occupied.Remove(padId);

        // Prefer the eligible color with the fewest active shards (GDD §12); ties broken randomly. This
        // naturally keeps ≥2 colors active — a 0-count color always beats an occupied one. Returns false only
        // when every color is already at the cap.
        private bool TryPickUnderRepresentedColor(out ShardColor color)
        {
            color = default;

            int minCount = int.MaxValue;
            for (int i = 0; i < _colors.Count; i++)
            {
                int count = CountOfColor(_colors[i]);
                if (count < _maxShardsPerColor && count < minCount)
                {
                    minCount = count;
                }
            }

            if (minCount == int.MaxValue)
            {
                return false;
            }

            _candidateBuffer.Clear();
            for (int i = 0; i < _colors.Count; i++)
            {
                if (CountOfColor(_colors[i]) == minCount)
                {
                    _candidateBuffer.Add(_colors[i]);
                }
            }

            color = _candidateBuffer[_random.NextInt(0, _candidateBuffer.Count)];
            return true;
        }
    }
}
