using StarforgeRelay.App;
using StarforgeRelay.Gameplay;
using UnityEngine;

namespace StarforgeRelay.UI
{
    /// <summary>
    /// Fills the <see cref="ResultsView"/> when the app enters Results (T14). Reads the machine's
    /// <see cref="AppStateMachine.LastResult"/> snapshot (GDD §16 — Results reads, never recomputes) and maps
    /// the terminal <see cref="RoundPhase"/> to the screen title. Presentation only.
    /// </summary>
    public sealed class ResultsPresenter : MonoBehaviour
    {
        [SerializeField] private ResultsView _view;

        private AppStateMachine _machine;

        /// <summary>Receive the state machine from the composition root.</summary>
        public void Initialize(AppStateMachine machine)
        {
            if (machine == null || _view == null)
            {
                Debug.LogError("[ResultsPresenter] Missing machine or results view — results disabled.", this);
                return;
            }

            _machine = machine;
            _machine.PhaseChanged += OnPhaseChanged;
        }

        private void OnDestroy()
        {
            if (_machine != null)
            {
                _machine.PhaseChanged -= OnPhaseChanged;
            }
        }

        private void OnPhaseChanged(AppPhase phase)
        {
            if (phase != AppPhase.Results)
            {
                return;
            }

            RoundEndedEvent result = _machine.LastResult;
            _view.SetResults(TitleFor(result.Result), result.Stars, result.Stabilization, result.Heat, result.Score);
        }

        // GDD §16 / §28 result titles.
        private static string TitleFor(RoundPhase result)
        {
            switch (result)
            {
                case RoundPhase.Won:
                    return "Core Stable!";
                case RoundPhase.Overloaded:
                    return "Overloaded";
                default:
                    return "Partial Relay";
            }
        }
    }
}
