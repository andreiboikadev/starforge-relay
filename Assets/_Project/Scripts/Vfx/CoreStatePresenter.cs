using StarforgeRelay.App;
using StarforgeRelay.Gameplay;
using UnityEngine;

namespace StarforgeRelay.Vfx
{
    /// <summary>
    /// Scene adapter that maps round state to the reactor core's <see cref="CoreVisualState"/> and core-anchored
    /// bursts (guardrails §6/§16 — it presents state, it does not own scoring/heat rules). Peer of
    /// <c>VfxController</c> and <c>FeedbackController</c>: the composition root injects via
    /// <see cref="Initialize"/> (from <c>Awake</c>, before the flow calls <c>machine.Begin()</c>, so it catches
    /// the initial Dormant); subscribes there and unsubscribes in <see cref="OnDestroy"/>. It reads the live
    /// <c>Heat</c> / <c>Stabilization</c> meters (not the partial event payloads) so a combo-milestone heat
    /// relief is reflected immediately.
    /// </summary>
    public sealed class CoreStatePresenter : MonoBehaviour
    {
        // Bands are T21 tuning levers (confirm on device): heat within this of the cap → Heating; stabilization
        // within this of the requirement → AlmostStable.
        private const int HeatWarningBand = 2;
        private const int AlmostStableBand = 2;

        private RoundLoopController _roundLoop;
        private ReactorCoreView _core;
        private AppStateMachine _machine;
        private RoundConfig _config;

        /// <summary>Receive the round loop, core view, app machine, and config from the composition root and subscribe.</summary>
        public void Initialize(RoundLoopController roundLoop, ReactorCoreView core, AppStateMachine machine, RoundConfig config)
        {
            if (roundLoop == null || core == null || machine == null || config == null)
            {
                Debug.LogError("[CoreStatePresenter] Missing a dependency — core visuals disabled.", this);
                return;
            }

            _roundLoop = roundLoop;
            _core = core;
            _machine = machine;
            _config = config;

            _roundLoop.RoundStarted += OnRoundStarted;
            _roundLoop.CorrectInserted += OnCorrect;
            _roundLoop.WrongInserted += OnWrong;
            _roundLoop.ShardExpired += OnExpired;
            _roundLoop.RoundEnded += OnRoundEnded;
            _machine.PhaseChanged += OnPhaseChanged;
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

            if (_machine != null)
            {
                _machine.PhaseChanged -= OnPhaseChanged;
            }
        }

        private void OnRoundStarted() => _core.SetVisualState(CoreVisualState.Charging);

        private void OnCorrect(CorrectInsertEvent e)
        {
            RefreshState();
            if (e.IsMilestone)
            {
                _core.PlayComboPulse();
            }
        }

        private void OnWrong(WrongInsertEvent e) => RefreshState();

        private void OnExpired(ShardExpiredEvent e) => RefreshState();

        private void OnRoundEnded(RoundEndedEvent e)
        {
            switch (e.Result)
            {
                case RoundPhase.Won:
                    _core.SetVisualState(CoreVisualState.Stabilized);
                    _core.PlayVictory();
                    break;
                case RoundPhase.Overloaded:
                    _core.SetVisualState(CoreVisualState.Overloaded);
                    _core.PlayOverload();
                    break;
                default:
                    // TimedOut: hold the partial charge level reached (GDD §12 — no special burst).
                    break;
            }
        }

        private void OnPhaseChanged(AppPhase phase)
        {
            if (phase == AppPhase.MainMenu || phase == AppPhase.Calibration || phase == AppPhase.Boot)
            {
                _core.SetVisualState(CoreVisualState.Dormant);
            }
        }

        // Derive the steady core state from the current live meters (guardrails §16; decision 4): a high heat
        // shows Heating over the charge tint; otherwise near-victory shows AlmostStable; otherwise Charging.
        // Live getters (not event payloads) so a milestone heat relief is reflected on the same correct insert.
        private void RefreshState()
        {
            if (_roundLoop.Heat >= _config.HeatCap - HeatWarningBand)
            {
                _core.SetVisualState(CoreVisualState.Heating);
            }
            else if (_roundLoop.Stabilization >= _config.StabilizationRequirement - AlmostStableBand)
            {
                _core.SetVisualState(CoreVisualState.AlmostStable);
            }
            else
            {
                _core.SetVisualState(CoreVisualState.Charging);
            }
        }
    }
}
