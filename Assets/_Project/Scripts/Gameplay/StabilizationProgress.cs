namespace StarforgeRelay.Gameplay
{
    /// <summary>
    /// Tracks accepted-shard progress → victory (GDD §12). Pure C#: no <c>MonoBehaviour</c>, no Unity
    /// statics — headless-testable (guardrails §16–17). Completion is exposed as queryable state;
    /// <c>RoundController</c> (T06) resolves it against the other end states.
    /// </summary>
    public sealed class StabilizationProgress
    {
        private readonly int requirement;

        /// <param name="stabilizationRequirement">Correct inserts needed to stabilize (GDD: 20). Injected from RoundConfig.</param>
        public StabilizationProgress(int stabilizationRequirement)
        {
            requirement = stabilizationRequirement;
        }

        /// <summary>Correct inserts accepted so far.</summary>
        public int Current { get; private set; }

        /// <summary>The core is stabilized — victory (GDD §12: exactly at <c>stabilizationRequirement</c>, not before).</summary>
        public bool IsComplete => Current >= requirement;

        /// <summary>A correct insert advances stabilization by 1 (GDD §12).</summary>
        public void RegisterCorrect() => Current++;
    }
}
