using System;

namespace StarforgeRelay.Gameplay
{
    /// <summary>
    /// Pure orchestrator for a Stabilize Run round (GDD §12–13; guardrails §16). It wires the five M1 rule
    /// services: routes correct / wrong / expired outcomes into score, combo, heat, and stabilization, ticks
    /// the timer, resolves the concurrent end-state priority (<c>Won &gt; Overloaded &gt; TimedOut</c>), and
    /// computes the star rating. Plain C# — no <c>MonoBehaviour</c>, no Unity statics; the MonoBehaviour tick
    /// driver and XR/HUD event plumbing are the M2 adapter (T11). It <b>wires</b> the T01–T04 rules, it does
    /// not re-implement them; Results <b>reads</b> the final state rather than recomputing it.
    /// </summary>
    public sealed class RoundController
    {
        private const int TwoStarThreshold = 12; // GDD §13
        private const int OneStarThreshold = 6;  // GDD §13

        private readonly ScoreService _score;
        private readonly ComboTracker _combo;
        private readonly HeatService _heat;
        private readonly StabilizationProgress _stabilization;
        private readonly RoundTimer _timer;

        /// <summary>Inject the five M1 rule services (manual DI) — this class owns none of their formulas.</summary>
        public RoundController(
            ScoreService score,
            ComboTracker combo,
            HeatService heat,
            StabilizationProgress stabilization,
            RoundTimer timer)
        {
            _score = score;
            _combo = combo;
            _heat = heat;
            _stabilization = stabilization;
            _timer = timer;
        }

        /// <summary>Raised on a correct insert, after the meters update.</summary>
        public event Action<CorrectInsertEvent> CorrectInserted;

        /// <summary>Raised on a wrong insert.</summary>
        public event Action<WrongInsertEvent> WrongInserted;

        /// <summary>Raised when a shard expires.</summary>
        public event Action<ShardExpiredEvent> ShardExpired;

        /// <summary>Raised once when the round reaches a terminal phase.</summary>
        public event Action<RoundEndedEvent> Ended;

        /// <summary>Current lifecycle phase.</summary>
        public RoundPhase Phase { get; private set; } = RoundPhase.NotStarted;

        /// <summary>True once the round has reached a terminal phase.</summary>
        public bool IsOver =>
            Phase == RoundPhase.Won || Phase == RoundPhase.Overloaded || Phase == RoundPhase.TimedOut;

        public int Score => _score.Score;
        public int Combo => _combo.Current;
        public int Heat => _heat.Current;
        public int Stabilization => _stabilization.Current;
        public float TimeRemaining => _timer.Remaining;

        /// <summary>
        /// Star rating from correct inserts (GDD §13): 0 (0–5 correct, incl. overload before 6), 1 (6–11),
        /// 2 (12–19), 3 (reactor stabilized). The 3-star case tracks the actual requirement via
        /// <see cref="StabilizationProgress.IsComplete"/>, so it stays correct if the requirement is retuned.
        /// </summary>
        public int Stars
        {
            get
            {
                if (_stabilization.IsComplete)
                {
                    return 3;
                }

                int correct = _stabilization.Current;
                if (correct >= TwoStarThreshold)
                {
                    return 2;
                }

                if (correct >= OneStarThreshold)
                {
                    return 1;
                }

                return 0;
            }
        }

        /// <summary>Begin the round (GDD §23). Apply/Tick are no-ops outside <see cref="RoundPhase.Playing"/>.</summary>
        public void Start() => Phase = RoundPhase.Playing;

        /// <summary>
        /// Correct insert (GDD §12): +1 combo, +10 score, +1 stabilization; on a combo milestone also +50
        /// bonus and −1 heat relief. May end the round in victory.
        /// </summary>
        public void ApplyCorrect()
        {
            if (Phase != RoundPhase.Playing)
            {
                return;
            }

            bool milestone = _combo.RegisterCorrect();
            _score.AddCorrect();
            _stabilization.RegisterCorrect();

            if (milestone)
            {
                _score.AddComboMilestoneBonus();
                _heat.RelieveAtMilestone();
            }

            CorrectInserted?.Invoke(new CorrectInsertEvent(_combo.Current, milestone, _stabilization.Current));
            CheckEndConditions();
        }

        /// <summary>Wrong insert (GDD §12): +1 heat, combo reset. May end the round in overload.</summary>
        public void ApplyWrong()
        {
            if (Phase != RoundPhase.Playing)
            {
                return;
            }

            _heat.RegisterWrongInsert();
            _combo.RegisterWrong();

            WrongInserted?.Invoke(new WrongInsertEvent(_heat.Current));
            CheckEndConditions();
        }

        /// <summary>Expired shard (GDD §12): +1 heat; combo resets only if it was held. May end in overload.</summary>
        public void ApplyExpired(bool wasHeld)
        {
            if (Phase != RoundPhase.Playing)
            {
                return;
            }

            _heat.RegisterExpiredShard();
            _combo.RegisterExpired(wasHeld);

            ShardExpired?.Invoke(new ShardExpiredEvent(_heat.Current, wasHeld));
            CheckEndConditions();
        }

        /// <summary>Advance the round clock; may end the round in time-out (GDD §12).</summary>
        public void Tick(float deltaTime)
        {
            if (Phase != RoundPhase.Playing)
            {
                return;
            }

            _timer.Tick(deltaTime);
            CheckEndConditions();
        }

        // Resolve concurrent end states in priority order victory > overload > time-out (guardrails §16).
        private void CheckEndConditions()
        {
            if (Phase != RoundPhase.Playing)
            {
                return;
            }

            if (_stabilization.IsComplete)
            {
                End(RoundPhase.Won);
            }
            else if (_heat.IsOverloaded)
            {
                End(RoundPhase.Overloaded);
            }
            else if (_timer.IsTimedOut)
            {
                End(RoundPhase.TimedOut);
            }
        }

        private void End(RoundPhase phase)
        {
            Phase = phase;

            // Finalize the results score (GDD §13) before publishing the snapshot the Results screen reads
            // (RoundEndedEvent "never recomputes"): a win adds the remaining-time bonus, and every outcome
            // applies the heat penalty (clamped at 0 by ScoreService).
            if (phase == RoundPhase.Won)
            {
                _score.AddVictoryTimeBonus((int)_timer.Remaining);
            }

            _score.ApplyHeatPenalty(_heat.Current);

            Ended?.Invoke(new RoundEndedEvent(phase, Stars, _score.Score, _stabilization.Current, _heat.Current));
        }
    }
}
