using StarforgeRelay.Gameplay;

namespace StarforgeRelay.App
{
    /// <summary>
    /// Per-result RoundComplete display delays (GDD §12): the brief beat between a round ending and the Results
    /// screen — victory ~2 s, overload ~1.5 s, time-out ~1 s. The composition root builds this from
    /// <c>RoundConfig</c> and hands it to <see cref="AppStateMachine"/>; <see cref="RoundCompleteState"/> reads
    /// the delay for the round's terminal phase. Small immutable value (guardrails §15).
    /// </summary>
    public readonly struct RoundCompleteDelays
    {
        public RoundCompleteDelays(float won, float overloaded, float timedOut)
        {
            Won = won;
            Overloaded = overloaded;
            TimedOut = timedOut;
        }

        /// <summary>Delay after a victory (GDD §12: ~2 s).</summary>
        public float Won { get; }

        /// <summary>Delay after an overload (GDD §12: ~1.5 s).</summary>
        public float Overloaded { get; }

        /// <summary>Delay after a time-out (GDD §12: ~1 s).</summary>
        public float TimedOut { get; }

        /// <summary>The display delay for a terminal <paramref name="result"/> phase (non-terminal → time-out delay).</summary>
        public float For(RoundPhase result)
        {
            return result switch
            {
                RoundPhase.Won => Won,
                RoundPhase.Overloaded => Overloaded,
                _ => TimedOut
            };
        }
    }
}
