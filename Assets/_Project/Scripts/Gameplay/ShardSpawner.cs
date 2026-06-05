using System.Collections.Generic;
using UnityEngine;

namespace StarforgeRelay.Gameplay
{
    /// <summary>
    /// Scene adapter that turns the pure <see cref="ShardSpawnPlanner"/> (T05) and pooled <see cref="ShardPool"/>
    /// (T08) into shards resting on feeder pads (GDD §5, §12). On start it derives the planner's pad slots from
    /// the scene pads (a single source of truth), pre-warms the pool, and fills to the active target with
    /// colour-distributed shards. The consume-on-accept / expiry / respawn loop and the round rules are T11 —
    /// this lays the scaffolding and does the initial fill only. XRI-free: it gets <see cref="ShardView"/> /
    /// <see cref="ShardMotion"/> off the prefab, never an XRI type.
    /// </summary>
    public sealed class ShardSpawner : MonoBehaviour
    {
        private static readonly ShardColor[] s_colors = { ShardColor.Solar, ShardColor.Ion, ShardColor.Pulse };

        [Header("Config + prefab")]
        [Tooltip("Round constants: active target, per-colour cap, respawn delay (read-only at runtime).")]
        [SerializeField] private RoundConfig _roundConfig;

        [Tooltip("Pooled energy-shard prefab (root carries ShardView + ShardMotion + XRGrabInteractable).")]
        [SerializeField] private ShardView _shardPrefab;

        [Tooltip("Parent for pooled shards. Falls back to this transform if unset.")]
        [SerializeField] private Transform _shardContainer;

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

        // Active shard -> its pad id; the round loop (T11) reads this to free the pad on accept/expire.
        private readonly Dictionary<ShardView, int> _shardPads = new Dictionary<ShardView, int>();

        private void Start()
        {
            if (_roundConfig == null || _shardPrefab == null || _pads == null || _pads.Length == 0)
            {
                Debug.LogError("[ShardSpawner] Missing RoundConfig, shard prefab, or feeder pads — not spawning.", this);
                return;
            }

            Transform container = _shardContainer != null ? _shardContainer : transform;
            _pool = new ShardPool(() => Instantiate(_shardPrefab, container), container);
            _planner = BuildPlanner();

            _pool.Prewarm(_roundConfig.ActiveShardsDefault);
            SpawnToTarget();
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

            _shardPads[shard] = plan.PadId;
        }
    }
}
