namespace StarforgeRelay.Gameplay
{
    /// <summary>
    /// Raised by <see cref="RoundController"/> when a shard expires — the payload the T11 adapter forwards to
    /// the heat bar and fizzle VFX. Small immutable value (guardrails §15).
    /// </summary>
    public readonly struct ShardExpiredEvent
    {
        public ShardExpiredEvent(int heat, bool comboWasReset)
        {
            Heat = heat;
            ComboWasReset = comboWasReset;
        }

        /// <summary>Heat after this expiry.</summary>
        public int Heat { get; }

        /// <summary>True when the combo broke (the shard was held when it expired) (GDD §12).</summary>
        public bool ComboWasReset { get; }
    }
}
