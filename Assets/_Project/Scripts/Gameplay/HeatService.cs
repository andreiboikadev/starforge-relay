using System;

namespace StarforgeRelay.Gameplay
{
    /// <summary>
    /// Tracks reactor heat → overload (GDD §12). Pure C#: no <c>MonoBehaviour</c>, no Unity statics —
    /// headless-testable (guardrails §16–17). Heat never drops below 0; overload is exposed as queryable
    /// state — resolving it against the other end states belongs to <c>RoundController</c> (T06), not here.
    /// </summary>
    public sealed class HeatService
    {
        private readonly int heatCap;
        private readonly int comboHeatRelief;

        /// <param name="heatCap">Overload threshold (GDD: 8).</param>
        /// <param name="comboHeatRelief">Heat removed per combo milestone (GDD: 1). Injected from RoundConfig.</param>
        public HeatService(int heatCap, int comboHeatRelief)
        {
            this.heatCap = heatCap;
            this.comboHeatRelief = comboHeatRelief;
        }

        /// <summary>Current heat.</summary>
        public int Current { get; private set; }

        /// <summary>Heat has reached the cap — the round overloads (GDD §12: exactly at <c>heatCap</c>, not before).</summary>
        public bool IsOverloaded => Current >= heatCap;

        /// <summary>A wrong insert adds 1 heat (GDD §12).</summary>
        public void RegisterWrongInsert() => Gain();

        /// <summary>An expired shard adds 1 heat (GDD §12).</summary>
        public void RegisterExpiredShard() => Gain();

        /// <summary>
        /// A combo milestone relieves <c>comboHeatRelief</c> heat, but only when heat is above 0, and never
        /// below 0 (GDD §12). Driven by the milestone signal owned by <see cref="ComboTracker"/>.
        /// </summary>
        public void RelieveAtMilestone()
        {
            if (Current > 0)
            {
                Current = Math.Max(0, Current - comboHeatRelief);
            }
        }

        private void Gain() => Current++;
    }
}
