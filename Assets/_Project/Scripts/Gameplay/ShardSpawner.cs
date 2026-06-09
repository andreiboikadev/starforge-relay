using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StarforgeRelay.Gameplay
{
    /// <summary>
    /// Scene adapter that turns the pure <see cref="ShardSpawnPlanner"/> (T05) and pooled <see cref="ShardPool"/>
    /// (T08) into shards resting on feeder pads (GDD §5, §12). It receives the pool from the composition root
    /// (T12), derives the planner's pad slots from the scene pads (a single source of truth), and fills to the
    /// active target with colour-distributed shards. It also ticks each shard's lifetime and raises an expiry
    /// for the round loop (T11b); consume-on-accept / respawn is T11. XRI-free: it reads held-state via
    /// <see cref="ShardMotion.IsHeld"/> and never references an XRI type.
    /// </summary>
    public sealed class ShardSpawner : MonoBehaviour
    {
        private static readonly ShardColor[] s_colors = { ShardColor.Solar, ShardColor.Ion, ShardColor.Pulse };

        [Header("Config")]
        [Tooltip("Round constants: active target, per-colour cap, respawn delay (read-only at runtime).")]
        [SerializeField] private RoundConfig _roundConfig;

        [Header("Pads + reach")]
        [Tooltip("Reactor root the pad angles are measured in (rotation-safe). Falls back to this transform if unset.")]
        [SerializeField] private Transform _reactorRoot;

        [Tooltip("Feeder pads; each pad's array index is its planner slot id.")]
        [SerializeField] private FeederPadView[] _pads;

        [Header("Reach zone (GDD §9) — must encompass the placed pads")]
        [Tooltip("Forward-arc min angle in degrees (negative = left).")]
        [SerializeField] private float _minAngleDegrees = -50f;

        [Tooltip("Forward-arc max angle in degrees (positive = right).")]
        [SerializeField] private float _maxAngleDegrees = 50f;

        [Tooltip("Lowest comfortable spawn height in metres (floor origin).")]
        [SerializeField] private float _minHeight = 0.9f;

        [Tooltip("Highest comfortable spawn height in metres (floor origin).")]
        [SerializeField] private float _maxHeight = 1.4f;

        [Header("Spawn")]
        [Tooltip("RNG seed; 0 uses a time-based seed (production variety).")]
        [SerializeField] private int _spawnSeed;

        private ShardSpawnPlanner _planner;
        private ShardPool _pool;
        private bool _active;

        // Active shard -> its pad id + lifetime + cached motion. The round loop (T11) frees the pad on
        // accept/expire; Update (T11b) ticks the lifetime. One record per shard → no pad/lifetime desync.
        private readonly Dictionary<ShardView, ActiveShard> _shardPads = new Dictionary<ShardView, ActiveShard>();

        // Reused each frame to collect shards that expired this tick, so we never raise (→ Despawn → mutate
        // _shardPads) while still enumerating it (T11b).
        private readonly List<ShardView> _expiredBuffer = new List<ShardView>();

        /// <summary>Current correct-insert count, injected by the round loop so a spawn picks the start/late
        /// lifetime (GDD §12). Null → 0 (start lifetime) — correct at the initial fill.</summary>
        public Func<int> AcceptsProvider { get; set; }

        /// <summary>Raised when an active shard's lifetime runs out (T11b); the bool is whether it was held.</summary>
        public event Action<ShardView, bool> ShardLifetimeExpired;

        /// <summary>
        /// Receive the pool from the composition root (T12). Stores it and validates the scene refs; the planner
        /// build + fill happen on <see cref="BeginFill"/> at round start (T13), not here — so the round no longer
        /// auto-starts on scene load.
        /// </summary>
        public void Initialize(ShardPool pool)
        {
            if (pool == null || _roundConfig == null || _pads == null || _pads.Length == 0)
            {
                Debug.LogError("[ShardSpawner] Missing pool, RoundConfig, or feeder pads — not spawning.", this);
                return;
            }

            _pool = pool;
        }

        private void Update()
        {
            if (!_active || _shardPads.Count == 0)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            float heldFactor = _roundConfig.HeldLifetimeFactor;

            foreach (KeyValuePair<ShardView, ActiveShard> entry in _shardPads)
            {
                ActiveShard active = entry.Value;
                bool held = active.Motion != null && active.Motion.IsHeld;
                active.Lifetime.Tick(deltaTime, held, heldFactor);
                if (active.Lifetime.IsExpired)
                {
                    _expiredBuffer.Add(entry.Key);
                }
            }

            // Raise AFTER the loop: each handler calls Despawn, which removes the shard from _shardPads — which
            // we are still enumerating above, so raising inside the foreach would throw.
            for (int i = 0; i < _expiredBuffer.Count; i++)
            {
                ShardView shard = _expiredBuffer[i];
                bool wasHeld = _shardPads.TryGetValue(shard, out ActiveShard active)
                    && active.Motion != null && active.Motion.IsHeld;
                ShardLifetimeExpired?.Invoke(shard, wasHeld);
            }

            _expiredBuffer.Clear();
        }

        /// <summary>Fill every free pad up to the active target with colour-distributed shards (GDD §12).</summary>
        public void SpawnToTarget()
        {
            while (_planner.TryPlanNextSpawn(out ShardSpawnPlan plan))
            {
                SpawnAt(plan);
            }

            int target = _roundConfig.ActiveShardsDefault;
            if (Debug.isDebugBuild && _planner.ActiveCount < target)
            {
                Debug.LogWarning(
                    $"[ShardSpawner] Filled only {_planner.ActiveCount}/{target} shards — a pad may be outside the " +
                    "reach zone (SpawnArea) or there are fewer pads than the target.",
                    this);
            }
        }

        /// <summary>
        /// Start a fresh fill (T13 round start): clear any leftover shards, rebuild the planner (clean per-round
        /// state — pad reservations, colour counts; with seed 0 also fresh variety), then fill to target. Called
        /// by the round lifecycle on Play / Play-Again / Restart.
        /// </summary>
        public void BeginFill()
        {
            if (_pool == null)
            {
                return;
            }

            ClearActive();
            _planner = BuildPlanner();
            SpawnToTarget();
            _active = true;
        }

        /// <summary>Return every active shard to the pool and forget it (round reset / teardown, T13). Safe when
        /// empty; cancels pending respawns.</summary>
        public void ClearActive()
        {
            foreach (KeyValuePair<ShardView, ActiveShard> entry in _shardPads)
            {
                entry.Value.Motion?.ForceRelease();
                _pool.Release(entry.Key);
            }

            _shardPads.Clear();
            StopAllCoroutines();
        }

        /// <summary>Consume a shard — accepted (T11) or expired (T11b): release any holder, pool it, free its
        /// pad, and schedule a replacement (GDD §12).</summary>
        public void Despawn(ShardView shard)
        {
            if (shard == null || !_shardPads.TryGetValue(shard, out ActiveShard active))
            {
                return;
            }

            _shardPads.Remove(shard);
            active.Motion?.ForceRelease(); // never pool a shard an interactor still selects (T09/T11)
            _planner.Release(active.PadId);
            _pool.Release(shard);

            if (_active)
            {
                StartCoroutine(RespawnAfterDelay(_planner.NextRespawnDelay()));
            }
        }

        /// <summary>Stop replacing consumed shards and cancel pending respawns (round end, T11).</summary>
        public void StopRespawns()
        {
            _active = false;
            StopAllCoroutines();
        }

        private IEnumerator RespawnAfterDelay(float delaySeconds)
        {
            yield return new WaitForSeconds(delaySeconds);
            if (_active)
            {
                SpawnToTarget();
            }
        }

        private ShardSpawnPlanner BuildPlanner()
        {
            Transform frame = _reactorRoot != null ? _reactorRoot : transform;
            var slots = new List<FeederPadSlot>(_pads.Length);
            for (int i = 0; i < _pads.Length; i++)
            {
                Transform anchor = _pads[i].Anchor;
                Vector3 local = frame.InverseTransformPoint(anchor.position);
                float angle = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
                slots.Add(new FeederPadSlot(i, angle, anchor.position.y));
            }

            IRandom random = _spawnSeed != 0 ? new SeededRandom(_spawnSeed) : new SeededRandom();
            return new ShardSpawnPlanner(
                slots,
                new SpawnArea(_minAngleDegrees, _maxAngleDegrees, _minHeight, _maxHeight),
                s_colors,
                _roundConfig.ActiveShardsDefault,
                _roundConfig.MaxShardsPerColor,
                _roundConfig.RespawnDelayMin,
                _roundConfig.RespawnDelayMax,
                random);
        }

        private void SpawnAt(ShardSpawnPlan plan)
        {
            Transform anchor = _pads[plan.PadId].Anchor;
            ShardView shard = _pool.Get();
            shard.transform.SetPositionAndRotation(anchor.position, anchor.rotation);
            shard.SetColor(plan.Color);

            ShardMotion motion = shard.GetComponent<ShardMotion>();
            if (motion != null)
            {
                motion.SetHome(anchor);
            }

            int accepts = AcceptsProvider != null ? AcceptsProvider() : 0;
            float duration = ShardLifetime.DurationForAccepts(
                accepts, _roundConfig.ShardLifetimeStart, _roundConfig.ShardLifetimeLate, _roundConfig.LateLifetimeAfterAccepts);

            _shardPads[shard] = new ActiveShard(plan.PadId, new ShardLifetime(duration), motion);
        }

        // Per-active-shard state: its pad, lifetime countdown, and a cached motion ref (so Update reads
        // held-state without a per-frame GetComponent). One record → no pad/lifetime desync.
        private sealed class ActiveShard
        {
            public ActiveShard(int padId, ShardLifetime lifetime, ShardMotion motion)
            {
                PadId = padId;
                Lifetime = lifetime;
                Motion = motion;
            }

            public int PadId { get; }
            public ShardLifetime Lifetime { get; }
            public ShardMotion Motion { get; }
        }
    }
}
