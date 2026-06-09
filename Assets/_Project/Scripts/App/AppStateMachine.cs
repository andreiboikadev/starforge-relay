using System;
using System.Collections.Generic;
using StarforgeRelay.Gameplay;

namespace StarforgeRelay.App
{
    /// <summary>
    /// The app/round state machine (GDD §23). Owns the current <see cref="IAppState"/> and the validated
    /// transitions between phases. Transition triggers invoke <see cref="IRoundLifecycle"/> (start on a fresh
    /// play, resume on unpause — so a resume never restarts the round) and then swap state; a trigger invalid
    /// for the current phase is a silent no-op. Plain C# — no <c>MonoBehaviour</c>, no <c>UnityEngine</c> — so
    /// it is unit-testable headless; the composition root creates it, ticks it on unscaled time, and exposes
    /// it for the UI (T14).
    /// </summary>
    public sealed class AppStateMachine
    {
        private readonly IRoundLifecycle _lifecycle;
        private readonly Dictionary<AppPhase, IAppState> _states;
        private IAppState _current;

        /// <summary>Build the machine over a round lifecycle and the RoundComplete display delay (GDD §12).</summary>
        public AppStateMachine(IRoundLifecycle lifecycle, float roundCompleteDelaySeconds)
        {
            _lifecycle = lifecycle;
            _states = new Dictionary<AppPhase, IAppState>
            {
                [AppPhase.Boot] = new BootState(this),
                [AppPhase.MainMenu] = new MainMenuState(),
                [AppPhase.Calibration] = new CalibrationState(),
                [AppPhase.Playing] = new PlayingState(),
                [AppPhase.Paused] = new PausedState(),
                [AppPhase.RoundComplete] = new RoundCompleteState(this, roundCompleteDelaySeconds),
                [AppPhase.Results] = new ResultsState()
            };
            _current = _states[AppPhase.Boot];
            _lifecycle.RoundEnded += OnRoundEnded;
        }

        /// <summary>Raised after every phase change (incl. the initial enter) — the UI/audio hook (T14/T16).</summary>
        public event Action<AppPhase> PhaseChanged;

        /// <summary>The current phase.</summary>
        public AppPhase Phase => _current.Phase;

        /// <summary>The most recent round-end snapshot, for the Results screen to read (GDD §16).</summary>
        public RoundEndedEvent LastResult { get; private set; }

        /// <summary>Enter the initial Boot state. Call once after wiring.</summary>
        public void Begin()
        {
            _current.Enter();
            PhaseChanged?.Invoke(_current.Phase);
        }

        /// <summary>Advance the current state (only RoundComplete uses it). Unscaled so it runs while paused.</summary>
        public void Tick(float unscaledDeltaTime) => _current.Tick(unscaledDeltaTime);

        /// <summary>MainMenu → Calibration.</summary>
        public void RequestCalibration()
        {
            if (Phase == AppPhase.MainMenu)
            {
                ChangeState(AppPhase.Calibration);
            }
        }

        /// <summary>Start a fresh round from Calibration (Play), Results (Play-Again), or Paused (Restart).</summary>
        public void RequestStartRound()
        {
            if (Phase == AppPhase.Calibration || Phase == AppPhase.Results || Phase == AppPhase.Paused)
            {
                _lifecycle.StartRound();
                ChangeState(AppPhase.Playing);
            }
        }

        /// <summary>Playing → Paused (freezes the round).</summary>
        public void RequestPause()
        {
            if (Phase == AppPhase.Playing)
            {
                _lifecycle.PauseRound();
                ChangeState(AppPhase.Paused);
            }
        }

        /// <summary>Paused → Playing (resumes — does not restart).</summary>
        public void RequestResume()
        {
            if (Phase == AppPhase.Paused)
            {
                _lifecycle.ResumeRound();
                ChangeState(AppPhase.Playing);
            }
        }

        /// <summary>Return to the main menu from Calibration, Paused, or Results (clears any live round).</summary>
        public void RequestMainMenu()
        {
            if (Phase == AppPhase.Calibration || Phase == AppPhase.Paused || Phase == AppPhase.Results)
            {
                _lifecycle.StopRound();
                ChangeState(AppPhase.MainMenu);
            }
        }

        // Boot leaves on its first tick (not inside Enter — avoids re-entrant ChangeState).
        internal void AdvanceFromBoot()
        {
            if (Phase == AppPhase.Boot)
            {
                ChangeState(AppPhase.MainMenu);
            }
        }

        // RoundComplete leaves when its display delay elapses.
        internal void AdvanceToResults()
        {
            if (Phase == AppPhase.RoundComplete)
            {
                ChangeState(AppPhase.Results);
            }
        }

        private void OnRoundEnded(RoundEndedEvent e)
        {
            if (Phase != AppPhase.Playing)
            {
                return;
            }

            LastResult = e;
            ChangeState(AppPhase.RoundComplete);
        }

        private void ChangeState(AppPhase next)
        {
            _current.Exit();
            _current = _states[next];
            _current.Enter();
            PhaseChanged?.Invoke(next);
        }
    }
}
