using System;
using StarforgeRelay.App;
using StarforgeRelay.Audio;
using StarforgeRelay.Gameplay;
using StarforgeRelay.Persistence;
using StarforgeRelay.UI;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Feedback;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

namespace StarforgeRelay.Composition
{
    /// <summary>
    /// The single scene-level composition root (guardrails §6/§8/§9; ADR 0001 manual DI). <b>Construction-only:</b>
    /// it builds the plain-C# graph — a <see cref="RoundController"/> factory (a fresh round per play, T13), the
    /// pooled <see cref="ShardPool"/>, and the <see cref="AppStateMachine"/> — injects it into the scene
    /// adapters / presenters via their <c>Initialize</c> methods, and disposes the pool + audio/haptic services
    /// on teardown. It does <b>not</b> tick or coordinate (the <see cref="AppFlowController"/> owns the machine
    /// tick + flow, T14); no gameplay rules and <b>not</b> a singleton. It carries XRI-typed serialized refs
    /// (<c>HapticImpulsePlayer</c> / <c>SimpleHapticFeedback</c>) purely as §8 wiring for <c>HapticService</c> —
    /// it makes no XRI calls itself (T16).
    /// </summary>
    public sealed class StarforgeRelayCompositionRoot : MonoBehaviour
    {
        // Brief beat between a round ending and the Results screen (GDD §12). Per-result timing (victory 2 s /
        // overload 1.5 s / time-out 1 s) lands with the feedback content in T17.
        private const float RoundCompleteDelaySeconds = 1.5f;

        [Header("Config + pooling")]
        [Tooltip("Round constants (read-only at runtime).")]
        [SerializeField] private RoundConfig _config;

        [Tooltip("Pooled energy-shard prefab (root carries ShardView + ShardMotion + XRGrabInteractable).")]
        [SerializeField] private ShardView _shardPrefab;

        [Tooltip("Parent for pooled shards.")]
        [SerializeField] private Transform _shardContainer;

        [Header("Gameplay adapters")]
        [SerializeField] private RoundLoopController _roundLoop;
        [SerializeField] private ShardSpawner _spawner;

        [Header("App flow + UI")]
        [SerializeField] private AppFlowController _appFlow;
        [SerializeField] private HUDPresenter _hudPresenter;
        [SerializeField] private ResultsPresenter _resultsPresenter;
        [SerializeField] private SettingsPresenter _settingsPresenter;

        [Header("Feedback (T16)")]
        [Tooltip("Cue → clip + volume map.")]
        [SerializeField] private AudioCueConfig _audioCueConfig;

        [Tooltip("Cue → haptic impulse map.")]
        [SerializeField] private HapticConfig _hapticConfig;

        [Tooltip("One 2D AudioSource for all one-shot cues (playOnAwake off, spatialBlend 0).")]
        [SerializeField] private AudioSource _audioSource;

        [Tooltip("Left controller's haptic player (XR Origin/Camera Offset/Left Controller).")]
        [SerializeField] private HapticImpulsePlayer _leftHaptic;

        [Tooltip("Right controller's haptic player.")]
        [SerializeField] private HapticImpulsePlayer _rightHaptic;

        [Tooltip("The rig's built-in SimpleHapticFeedback components, gated by the Haptics setting.")]
        [SerializeField] private SimpleHapticFeedback[] _rigSelectHaptics;

        [SerializeField] private FeedbackController _feedbackController;

        private ShardPool _pool;
        private SettingsService _settings;
        private AudioService _audioService;
        private HapticService _hapticService;

        // Build the graph and inject it before the adapters act — Awake runs before any Start/Update.
        private void Awake()
        {
            if (_config == null || _shardPrefab == null || _shardContainer == null
                || _roundLoop == null || _spawner == null
                || _appFlow == null || _hudPresenter == null || _resultsPresenter == null
                || _settingsPresenter == null
                || _audioCueConfig == null || _hapticConfig == null || _audioSource == null
                || _leftHaptic == null || _rightHaptic == null
                || _rigSelectHaptics == null || _rigSelectHaptics.Length == 0
                || _feedbackController == null)
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
            var machine = new AppStateMachine(_roundLoop, RoundCompleteDelaySeconds);
            _appFlow.Initialize(machine);
            _resultsPresenter.Initialize(machine);
            _hudPresenter.Initialize(_roundLoop, _config);

            // Settings: load persisted values now (Awake, before the flow's Begin) and seed the Settings UI.
            _settings = new SettingsService(new PlayerPrefsSettingsStore());
            _settingsPresenter.Initialize(_settings);

            // Feedback (T16): audio + haptics are the first real consumers of the T15 settings seam
            // (Current + Changed). Both are IDisposable — disposed in OnDestroy.
            _audioService = new AudioService(_audioCueConfig, _audioSource, _settings);
            _hapticService = new HapticService(_hapticConfig, _leftHaptic, _rightHaptic, _rigSelectHaptics, _settings);
            _feedbackController.Initialize(_audioService, _hapticService, _roundLoop, _spawner, _appFlow, _settings);
        }

        private void OnDestroy()
        {
            _pool?.Dispose();
            _audioService?.Dispose();
            _hapticService?.Dispose();
        }
    }
}
