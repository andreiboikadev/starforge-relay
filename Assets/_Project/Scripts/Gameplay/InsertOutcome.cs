namespace StarforgeRelay.Gameplay
{
    /// <summary>
    /// The decision a port release produces (GDD §12). Computed by <see cref="PortValidationService"/> from
    /// color match; applied to heat/combo/score by <c>RoundController</c> (T06) and to eject / push-back by
    /// the port-socket adapter (T09). A pure decision — it carries no side effects.
    /// </summary>
    public enum InsertOutcome
    {
        /// <summary>Shard color matched the port — accept (GDD §12 correct insert).</summary>
        Correct,

        /// <summary>Shard released clearly inside a wrong-color port — reject + penalize (GDD §12 wrong insert).</summary>
        Wrong,

        /// <summary>Released outside any port — neither correct nor penalized (GDD §10/§12 empty-space drop).</summary>
        NoPenalty
    }
}
