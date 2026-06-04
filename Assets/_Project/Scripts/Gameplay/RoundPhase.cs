namespace StarforgeRelay.Gameplay
{
    /// <summary>
    /// Lifecycle phase of a Stabilize Run round (GDD §12, §23). The three terminal phases are resolved by
    /// <see cref="RoundController"/> in priority order <c>Won &gt; Overloaded &gt; TimedOut</c> (guardrails §16).
    /// </summary>
    public enum RoundPhase
    {
        NotStarted,
        Playing,
        Won,
        Overloaded,
        TimedOut
    }
}
