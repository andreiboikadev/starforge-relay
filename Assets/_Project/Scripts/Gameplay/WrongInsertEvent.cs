namespace StarforgeRelay.Gameplay
{
    /// <summary>
    /// Raised by <see cref="RoundController"/> on a wrong insert — the payload the T11 adapter forwards to
    /// the heat bar and spark VFX. Small immutable value (guardrails §15).
    /// </summary>
    public readonly struct WrongInsertEvent
    {
        public WrongInsertEvent(int heat) => Heat = heat;

        /// <summary>Heat after this wrong insert.</summary>
        public int Heat { get; }
    }
}
