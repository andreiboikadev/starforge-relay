namespace StarforgeRelay.Gameplay
{
    /// <summary>
    /// Raised by <see cref="RoundController"/> on a correct insert — the payload the T11 adapter forwards to
    /// combo HUD, beam / combo-pulse VFX, and the charge ring. Small immutable value (guardrails §15).
    /// </summary>
    public readonly struct CorrectInsertEvent
    {
        public CorrectInsertEvent(int combo, bool isMilestone, int stabilization)
        {
            Combo = combo;
            IsMilestone = isMilestone;
            Stabilization = stabilization;
        }

        /// <summary>Combo after this insert.</summary>
        public int Combo { get; }

        /// <summary>True when this insert hit a combo milestone (bonus + heat relief applied).</summary>
        public bool IsMilestone { get; }

        /// <summary>Stabilization progress after this insert.</summary>
        public int Stabilization { get; }
    }
}
