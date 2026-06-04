namespace StarforgeRelay.Gameplay
{
    /// <summary>
    /// The comfortable forward reach zone a spawn slot must fall inside (GDD §9): a horizontal angle arc
    /// (e.g. −50…+50° from player-forward — anything outside is too wide or behind the player) and a height
    /// band. Pure data; <see cref="Contains"/> is the validation the planner applies so no slot behind the
    /// player, outside the arc, or outside the height band is ever returned.
    /// </summary>
    public readonly struct SpawnArea
    {
        public SpawnArea(float minAngleDegrees, float maxAngleDegrees, float minHeight, float maxHeight)
        {
            MinAngleDegrees = minAngleDegrees;
            MaxAngleDegrees = maxAngleDegrees;
            MinHeight = minHeight;
            MaxHeight = maxHeight;
        }

        public float MinAngleDegrees { get; }
        public float MaxAngleDegrees { get; }
        public float MinHeight { get; }
        public float MaxHeight { get; }

        /// <summary>True when the slot's angle and height both fall within this reach zone.</summary>
        public bool Contains(FeederPadSlot slot) =>
            slot.AngleDegrees >= MinAngleDegrees && slot.AngleDegrees <= MaxAngleDegrees &&
            slot.Height >= MinHeight && slot.Height <= MaxHeight;
    }
}
