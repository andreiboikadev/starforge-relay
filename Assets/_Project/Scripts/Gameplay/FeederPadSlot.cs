namespace StarforgeRelay.Gameplay
{
    /// <summary>
    /// A predefined feeder-pad slot — pure placement data the planner reasons about, with no scene/Unity
    /// dependency (the world transform for <see cref="PadId"/> is owned by the spawner adapter, T10).
    /// <see cref="AngleDegrees"/> is measured from player-forward (− left / + right); <see cref="Height"/>
    /// is metres. Validated against <see cref="SpawnArea"/> so out-of-reach slots are never used (GDD §9).
    /// </summary>
    public readonly struct FeederPadSlot
    {
        public FeederPadSlot(int padId, float angleDegrees, float height)
        {
            PadId = padId;
            AngleDegrees = angleDegrees;
            Height = height;
        }

        /// <summary>Stable id linking the slot to its scene pad (resolved by the spawner adapter, T10).</summary>
        public int PadId { get; }

        /// <summary>Horizontal angle from player-forward in degrees (− left / + right).</summary>
        public float AngleDegrees { get; }

        /// <summary>Spawn height in metres.</summary>
        public float Height { get; }
    }
}
