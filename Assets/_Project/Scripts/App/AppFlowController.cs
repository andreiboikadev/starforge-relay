using StarforgeRelay.UI;
using UnityEngine;

namespace StarforgeRelay.App
{
    /// <summary>
    /// Scene host for the app/round state machine (T13/T14): it owns + ticks the <see cref="AppStateMachine"/>
    /// on unscaled time and presents the flow — shows the screen for the current phase and routes screen-button
    /// intents to the machine's transition triggers. The composition root injects the machine via
    /// <see cref="Initialize"/>, which keeps the root construction-only (guardrails §6). Views are dumb; the
    /// HUD and Results are separate presenters.
    /// </summary>
    public sealed class AppFlowController : MonoBehaviour
    {
        [SerializeField] private MainMenuView _mainMenu;
        [SerializeField] private CalibrationView _calibration;
        [SerializeField] private HUDView _hud;
        [SerializeField] private PauseMenuView _pauseMenu;
        [SerializeField] private ResultsView _results;

        private AppStateMachine _machine;

        /// <summary>Receive the state machine from the composition root and wire the views to its triggers.</summary>
        public void Initialize(AppStateMachine machine)
        {
            if (machine == null || _mainMenu == null || _calibration == null || _hud == null
                || _pauseMenu == null || _results == null)
            {
                Debug.LogError("[AppFlowController] Missing machine or a view reference — flow disabled.", this);
                return;
            }

            _machine = machine;
            _machine.PhaseChanged += OnPhaseChanged;

            _mainMenu.PlayClicked += OnPlay;
            _calibration.StartClicked += OnStartRound;
            _calibration.BackClicked += OnMainMenu;
            _pauseMenu.ResumeClicked += OnResume;
            _pauseMenu.RestartClicked += OnStartRound;
            _pauseMenu.MainMenuClicked += OnMainMenu;
            _results.PlayAgainClicked += OnStartRound;
            _results.MainMenuClicked += OnMainMenu;
        }

        // Enter the initial Boot state once, after the root has injected (Awake) — Start runs after all Awakes.
        private void Start()
        {
            if (_machine != null)
            {
                _machine.Begin();
            }
        }

        // The machine owns the round tick (T13 gated it). Unscaled so it advances while the round is paused.
        private void Update()
        {
            if (_machine != null)
            {
                _machine.Tick(Time.unscaledDeltaTime);
            }
        }

        private void OnDestroy()
        {
            if (_machine == null)
            {
                return;
            }

            _machine.PhaseChanged -= OnPhaseChanged;
            _mainMenu.PlayClicked -= OnPlay;
            _calibration.StartClicked -= OnStartRound;
            _calibration.BackClicked -= OnMainMenu;
            _pauseMenu.ResumeClicked -= OnResume;
            _pauseMenu.RestartClicked -= OnStartRound;
            _pauseMenu.MainMenuClicked -= OnMainMenu;
            _results.PlayAgainClicked -= OnStartRound;
            _results.MainMenuClicked -= OnMainMenu;
        }

        private void OnPlay() => _machine.RequestCalibration();

        private void OnStartRound() => _machine.RequestStartRound();

        private void OnResume() => _machine.RequestResume();

        private void OnMainMenu() => _machine.RequestMainMenu();

        private void OnPhaseChanged(AppPhase phase)
        {
            SetActive(_mainMenu, phase == AppPhase.MainMenu);
            SetActive(_calibration, phase == AppPhase.Calibration);
            SetActive(_hud, phase == AppPhase.Playing || phase == AppPhase.Paused);
            SetActive(_pauseMenu, phase == AppPhase.Paused);
            SetActive(_results, phase == AppPhase.Results);
        }

        private static void SetActive(Component view, bool active)
        {
            if (view != null && view.gameObject.activeSelf != active)
            {
                view.gameObject.SetActive(active);
            }
        }
    }
}
