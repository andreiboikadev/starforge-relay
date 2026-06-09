using StarforgeRelay.Gameplay;
using UnityEngine;

namespace StarforgeRelay.UI
{
    /// <summary>
    /// Drives the <see cref="HUDView"/> from the live round (T14). Subscribes the round-loop's surfaced events
    /// (T11/T14) for score / combo / charge / heat, and reads the remaining time each frame — updating the
    /// timer text only when the shown second changes (no per-frame text churn, guardrails §11/§14). Presentation
    /// only: it reads the meters the tested services compute and the caps from <see cref="RoundConfig"/>.
    /// </summary>
    public sealed class HUDPresenter : MonoBehaviour
    {
        [SerializeField] private HUDView _view;

        private RoundLoopController _roundLoop;
        private RoundConfig _config;
        private int _shownSeconds = -1;

        /// <summary>Receive the round loop + config from the composition root.</summary>
        public void Initialize(RoundLoopController roundLoop, RoundConfig config)
        {
            if (roundLoop == null || config == null || _view == null)
            {
                Debug.LogError("[HUDPresenter] Missing round loop, config, or HUD view — HUD disabled.", this);
                return;
            }

            _roundLoop = roundLoop;
            _config = config;
            _roundLoop.RoundStarted += OnRoundStarted;
            _roundLoop.CorrectInserted += OnCorrect;
            _roundLoop.WrongInserted += OnWrong;
            _roundLoop.ShardExpired += OnExpired;
        }

        private void OnDestroy()
        {
            if (_roundLoop == null)
            {
                return;
            }

            _roundLoop.RoundStarted -= OnRoundStarted;
            _roundLoop.CorrectInserted -= OnCorrect;
            _roundLoop.WrongInserted -= OnWrong;
            _roundLoop.ShardExpired -= OnExpired;
        }

        private void Update()
        {
            if (_roundLoop == null)
            {
                return;
            }

            int seconds = Mathf.Max(0, Mathf.CeilToInt(_roundLoop.TimeRemaining));
            if (seconds != _shownSeconds)
            {
                _shownSeconds = seconds;
                _view.SetTimer($"{seconds / 60}:{seconds % 60:00}");
            }
        }

        private void OnRoundStarted()
        {
            _shownSeconds = -1; // force the timer to refresh on the next frame
            RefreshMeters();
        }

        private void OnCorrect(CorrectInsertEvent e) => RefreshMeters();

        private void OnWrong(WrongInsertEvent e) => RefreshMeters();

        private void OnExpired(ShardExpiredEvent e) => RefreshMeters();

        private void RefreshMeters()
        {
            _view.SetScore(_roundLoop.Score);
            _view.SetCombo(_roundLoop.Combo);
            _view.SetCharge(_roundLoop.Stabilization, _config.StabilizationRequirement);
            _view.SetHeat(_roundLoop.Heat, _config.HeatCap);
        }
    }
}
