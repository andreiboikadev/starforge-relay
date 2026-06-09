using System;
using StarforgeRelay.App;
using StarforgeRelay.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;

namespace StarforgeRelay.Composition
{
    /// <summary>
    /// The single scene-level composition root (guardrails §6/§8/§9; ADR 0001 manual DI). It builds the
    /// plain-C# graph — a <see cref="RoundController"/> factory (a fresh round per play, T13), the pooled
    /// <see cref="ShardPool"/>, and the <see cref="AppStateMachine"/> — hands them to the scene adapters via
    /// <c>Initialize</c>, ticks the state machine on unscaled time, and disposes the pool on teardown. Wiring +
    /// lifecycle only — no gameplay rules, no XRI, and <b>not</b> a singleton. The temporary debug keys drive
    /// the flow until T14's world-space UI replaces them.
    /// </summary>
    public sealed class StarforgeRelayCompositionRoot : MonoBehaviour
    {
        // Brief beat between a round ending and the Results screen (GDD §12). Per-result timing (victory 2 s /
        // overload 1.5 s / time-out 1 s) lands with the feedback content in T17.
        private const float RoundCompleteDelaySeconds = 1.5f;

        [Tooltip("Round constants (read-only at runtime).")]
        [SerializeField] private RoundConfig _config;

        [Tooltip("Pooled energy-shard prefab (root carries ShardView + ShardMotion + XRGrabInteractable).")]
        [SerializeField] private ShardView _shardPrefab;

        [Tooltip("Parent for pooled shards.")]
        [SerializeField] private Transform _shardContainer;

        [Tooltip("Scene round-loop adapter that drives the Stabilize Run loop (implements IRoundLifecycle).")]
        [SerializeField] private RoundLoopController _roundLoop;

        [Tooltip("Scene shard spawner.")]
        [SerializeField] private ShardSpawner _spawner;

        private ShardPool _pool;
        private AppStateMachine _machine;

        /// <summary>The app state machine, exposed so the UI presenters (T14) can drive + observe it.</summary>
        public AppStateMachine Machine => _machine;

        // Build the graph and inject it before the adapters act — Awake runs before any Start/Update.
        private void Awake()
        {
            if (_config == null || _shardPrefab == null || _shardContainer == null
                || _roundLoop == null || _spawner == null)
            {
                Debug.LogError("[CompositionRoot] Missing a serialized reference — nothing wired.", this);
                return;
            }

            // A fresh round per play (first start / Play-Again / Restart) — cheaper than resetting the tested
            // rule services in place, and it keeps that core untouched.
            Func<RoundController> roundFactory = () => new RoundController(
                new ScoreService(_config.CorrectScore, _config.ComboBonusScore, _config.VictoryTimeBonusPerSecond, _config.HeatPenaltyPerHeat),
                new ComboTracker(_config.ComboBonusInterval),
                new HeatService(_config.HeatCap, _config.ComboHeatRelief),
                new StabilizationProgress(_config.StabilizationRequirement),
                new RoundTimer(_config.RoundDurationSeconds));

            _pool = new ShardPool(() => Instantiate(_shardPrefab, _shardContainer), _shardContainer);
            _pool.Prewarm(_config.ActiveShardsDefault);

            _roundLoop.Initialize(roundFactory);
            _spawner.Initialize(_pool);

            // The round loop implements IRoundLifecycle; the machine gates round start on the Playing state.
            _machine = new AppStateMachine(_roundLoop, RoundCompleteDelaySeconds);
        }

        private void Start()
        {
            _machine?.Begin();
        }

        private void Update()
        {
            if (_machine == null)
            {
                return;
            }

            _machine.Tick(Time.unscaledDeltaTime);
            ReadDebugKeys();
        }

        private void OnDestroy()
        {
            _pool?.Dispose();
        }

        // TEMPORARY debug driver (removed in T14, replaced by world-space UI buttons): keyboard advances the
        // flow in the editor / XR Device Simulator. Input System (no legacy Input); null-safe with no keyboard.
        private void ReadDebugKeys()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                if (_machine.Phase == AppPhase.MainMenu)
                {
                    _machine.RequestCalibration();
                }
                else if (_machine.Phase == AppPhase.Calibration)
                {
                    _machine.RequestStartRound();
                }
            }

            if (keyboard.pKey.wasPressedThisFrame)
            {
                if (_machine.Phase == AppPhase.Playing)
                {
                    _machine.RequestPause();
                }
                else if (_machine.Phase == AppPhase.Paused)
                {
                    _machine.RequestResume();
                }
            }

            if (keyboard.rKey.wasPressedThisFrame)
            {
                _machine.RequestStartRound();
            }

            if (keyboard.mKey.wasPressedThisFrame)
            {
                _machine.RequestMainMenu();
            }
        }
    }
}
