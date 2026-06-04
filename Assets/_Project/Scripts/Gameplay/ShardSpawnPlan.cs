namespace StarforgeRelay.Gameplay
{
    /// <summary>
    /// A single spawn decision from <see cref="ShardSpawnPlanner"/>: which color to spawn on which free pad.
    /// A pure value — turning it into a pooled shard at the pad's transform is the spawner adapter (T10).
    /// </summary>
    public readonly struct ShardSpawnPlan
    {
        public ShardSpawnPlan(int padId, ShardColor color)
        {
            PadId = padId;
            Color = color;
        }

        /// <summary>The free feeder pad to spawn on.</summary>
        public int PadId { get; }

        /// <summary>The shard color to spawn.</summary>
        public ShardColor Color { get; }
    }
}
