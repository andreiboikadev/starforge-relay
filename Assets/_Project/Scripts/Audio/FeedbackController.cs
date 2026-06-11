using StarforgeRelay.App;
using StarforgeRelay.Gameplay;
using StarforgeRelay.Persistence;
using UnityEngine;

namespace StarforgeRelay.Audio
{
    /// <summary>
    /// Scene adapter that maps the game's semantic events to <see cref="FeedbackCue"/>s and drives the
    /// <see cref="AudioService"/> + <see cref="HapticService"/> (guardrails §6 — gameplay never references a
    /// clip or a haptic call). Mirrors <c>HUDPresenter</c>: the composition root injects everything via
    /// <see cref="Initialize"/>; it subscribes there and unsubscribes in <see cref="OnDestroy"/>. Grab/release
    /// cues are suppressed while the round is paused (those events bypass the round loop's own gates).
    /// </summary>
    public sealed class FeedbackController : MonoBehaviour
    {
        private AudioService _audio;
        private HapticService _haptics;
        private RoundLoopController _roundLoop;
        private ShardSpawner _spawner;
        private AppFlowController _appFlow;
        private SettingsService _settings;

        /// <summary>Receive the services and event sources from the composition root and subscribe.</summary>
        public void Initialize(
            AudioService audio,
            HapticService haptics,
            RoundLoopController roundLoop,
            ShardSpawner spawner,
            AppFlowController appFlow,
            SettingsService settings)
        {
            if (audio == null || haptics == null || roundLoop == null || spawner == null
                || appFlow == null || settings == null)
            {
                Debug.LogError("[FeedbackController] Missing a dependency — feedback disabled.", this);
                return;
            }

            _audio = audio;
            _haptics = haptics;
            _roundLoop = roundLoop;
            _spawner = spawner;
            _appFlow = appFlow;
            _settings = settings;

            _roundLoop.RoundStarted += OnRoundStarted;
            _roundLoop.CorrectInserted += OnCorrect;
            _roundLoop.WrongInserted += OnWrong;
            _roundLoop.ShardExpired += OnExpired;
            _roundLoop.RoundEnded += OnRoundEnded;
            _spawner.ShardGrabbed += OnShardGrabbed;
            _spawner.ShardReleased += OnShardReleased;
            _appFlow.UiSelected += OnUiSelected;
            _settings.Changed += OnSettingsChanged;
        }

        private void OnDestroy()
        {
            if (_roundLoop != null)
            {
                _roundLoop.RoundStarted -= OnRoundStarted;
                _roundLoop.CorrectInserted -= OnCorrect;
                _roundLoop.WrongInserted -= OnWrong;
                _roundLoop.ShardExpired -= OnExpired;
                _roundLoop.RoundEnded -= OnRoundEnded;
            }

            if (_spawner != null)
            {
                _spawner.ShardGrabbed -= OnShardGrabbed;
                _spawner.ShardReleased -= OnShardReleased;
            }

            if (_appFlow != null)
            {
                _appFlow.UiSelected -= OnUiSelected;
            }

            if (_settings != null)
            {
                _settings.Changed -= OnSettingsChanged;
            }
        }

        private void OnRoundStarted() => Play(FeedbackCue.RoundStart);

        private void OnCorrect(CorrectInsertEvent e)
        {
            Play(FeedbackCue.CorrectInsert);
            if (e.IsMilestone)
            {
                Play(FeedbackCue.ComboMilestone);
            }
        }

        private void OnWrong(WrongInsertEvent e) => Play(FeedbackCue.WrongInsert);

        private void OnExpired(ShardExpiredEvent e) => Play(FeedbackCue.ShardExpired);

        private void OnRoundEnded(RoundEndedEvent e) => Play(ResultCue(e.Result));

        // Grab/release bypass the round loop's pause gates (XRI selection runs at timeScale 0), so gate here.
        private void OnShardGrabbed()
        {
            if (!_roundLoop.IsPaused)
            {
                Play(FeedbackCue.GrabShard);
            }
        }

        private void OnShardReleased()
        {
            if (!_roundLoop.IsPaused)
            {
                Play(FeedbackCue.ReleaseShard);
            }
        }

        private void OnUiSelected() => Play(FeedbackCue.UiSelect);

        // A real settings change is exactly one toggle click (idempotent sets raise nothing — T15).
        private void OnSettingsChanged(GameSettings settings) => Play(FeedbackCue.UiSelect);

        private void Play(FeedbackCue cue)
        {
            _audio.Play(cue);
            _haptics.Play(cue);
        }

        private static FeedbackCue ResultCue(RoundPhase result)
        {
            return result switch
            {
                RoundPhase.Won => FeedbackCue.Victory,
                RoundPhase.Overloaded => FeedbackCue.Overload,
                _ => FeedbackCue.TimedOut
            };
        }
    }
}
