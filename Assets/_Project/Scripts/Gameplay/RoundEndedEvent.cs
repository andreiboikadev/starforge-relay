namespace StarforgeRelay.Gameplay
{
    /// <summary>
    /// Raised by <see cref="RoundController"/> when the round ends — exactly the data the Results screen reads
    /// (GDD §16). Results consumes this snapshot; it never recomputes score/heat (guardrails §16).
    /// </summary>
    public readonly struct RoundEndedEvent
    {
        public RoundEndedEvent(RoundPhase result, int stars, int score, int stabilization, int heat)
        {
            Result = result;
            Stars = stars;
            Score = score;
            Stabilization = stabilization;
            Heat = heat;
        }

        /// <summary>Terminal phase: <see cref="RoundPhase.Won"/> / <see cref="RoundPhase.Overloaded"/> / <see cref="RoundPhase.TimedOut"/>.</summary>
        public RoundPhase Result { get; }

        public int Stars { get; }
        public int Score { get; }
        public int Stabilization { get; }
        public int Heat { get; }
    }
}
