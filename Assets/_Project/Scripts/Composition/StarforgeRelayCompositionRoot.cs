using StarforgeRelay.Gameplay;
using UnityEngine;

namespace StarforgeRelay.Composition
{
    /// <summary>
    /// The single scene-level composition root (guardrails §6/§8/§9; ADR 0001 manual DI). It owns construction
    /// and lifecycle of the plain-C# graph: it builds the five rule services + <see cref="RoundController"/> and
    /// the pooled <see cref="ShardPool"/>, hands them to the scene adapters via their <c>Initialize</c> methods,
    /// and disposes the pool on teardown (it is <see cref="System.IDisposable"/> — undisposed it leaks on
    /// Play-stop). Wiring only — no gameplay rules, no XRI, and <b>not</b> a singleton (a scene-level root is
    /// allowed; expected custom singletons stay 0).
    /// </summary>
    public sealed class StarforgeRelayCompositionRoot : MonoBehaviour
    {
        [Tooltip("Round constants (read-only at runtime).")]
        [SerializeField] private RoundConfig _config;

        [Tooltip("Pooled energy-shard prefab (root carries ShardView + ShardMotion + XRGrabInteractable).")]
        [SerializeField] private ShardView _shardPrefab;

        [Tooltip("Parent for pooled shards.")]
        [SerializeField] private Transform _shardContainer;

        [Tooltip("Scene round-loop adapter that drives the Stabilize Run loop.")]
        [SerializeField] private RoundLoopController _roundLoop;

        [Tooltip("Scene shard spawner.")]
        [SerializeField] private ShardSpawner _spawner;

        private ShardPool _pool;

        // Build the graph and inject it before the adapters act — Awake runs before any Start/Update.
        private void Awake()
        {
            if (_config == null || _shardPrefab == null || _shardContainer == null
                || _roundLoop == null || _spawner == null)
            {
                Debug.LogError("[CompositionRoot] Missing a serialized reference — nothing wired.", this);
                return;
            }

            var round = new RoundController(
                new ScoreService(_config.CorrectScore, _config.ComboBonusScore, _config.VictoryTimeBonusPerSecond, _config.HeatPenaltyPerHeat),
                new ComboTracker(_config.ComboBonusInterval),
                new HeatService(_config.HeatCap, _config.ComboHeatRelief),
                new StabilizationProgress(_config.StabilizationRequirement),
                new RoundTimer(_config.RoundDurationSeconds));

            _pool = new ShardPool(() => Instantiate(_shardPrefab, _shardContainer), _shardContainer);
            _pool.Prewarm(_config.ActiveShardsDefault);

            // Round loop first: it sets the spawner's AcceptsProvider + subscribes its expiry before the spawner fills.
            _roundLoop.Initialize(round);
            _spawner.Initialize(_pool);
        }

        private void OnDestroy()
        {
            _pool?.Dispose();
        }
    }
}
