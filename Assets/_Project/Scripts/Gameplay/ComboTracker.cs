namespace StarforgeRelay.Gameplay
{
    /// <summary>
    /// Tracks the correct-insert combo and detects milestones (GDD §12). Pure C#: no <c>MonoBehaviour</c>,
    /// no Unity statics — headless-testable (guardrails §16–17). Owns the milestone formula; <see
    /// cref="ScoreService"/> consumes the milestone signal rather than recomputing it (one owner per rule).
    /// </summary>
    public sealed class ComboTracker
    {
        private readonly int milestoneInterval;

        /// <param name="comboBonusInterval">Combo length between milestones (GDD: 5). Injected from RoundConfig.</param>
        public ComboTracker(int comboBonusInterval)
        {
            milestoneInterval = comboBonusInterval;
        }

        /// <summary>Current combo — consecutive correct inserts.</summary>
        public int Current { get; private set; }

        /// <summary>
        /// Register a correct insert: increments the combo and returns <c>true</c> when this insert lands on
        /// a milestone (every <see cref="milestoneInterval"/>: 5, 10, 15, …).
        /// </summary>
        public bool RegisterCorrect()
        {
            Current++;
            return IsMilestone;
        }

        /// <summary>A wrong insert always breaks the combo (GDD §12).</summary>
        public void RegisterWrong()
        {
            Current = 0;
        }

        /// <summary>An expired shard breaks the combo only if it was being held when it expired (GDD §12).</summary>
        public void RegisterExpired(bool wasHeld)
        {
            if (wasHeld)
            {
                Current = 0;
            }
        }

        private bool IsMilestone => milestoneInterval > 0 && Current > 0 && Current % milestoneInterval == 0;
    }
}
