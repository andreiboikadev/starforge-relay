namespace StarforgeRelay.Gameplay
{
    /// <summary>
    /// Pure countdown for one shard's lifetime (GDD §10, §12). It drains in real time while the shard is free
    /// and strongly slowed — but never fully paused — while it is grabbed, and expires at zero. The start-vs-late
    /// duration choice (14 s → 12 s after N accepts) is <see cref="DurationForAccepts"/>. Plain C# (no
    /// <c>MonoBehaviour</c>, no Unity statics): the spawner drives it, so it stays headless-testable.
    /// </summary>
    public sealed class ShardLifetime
    {
        private float _remaining;

        /// <summary>Start a lifetime of <paramref name="duration"/> seconds.</summary>
        public ShardLifetime(float duration) => _remaining = duration;

        /// <summary>Seconds left before expiry (never negative).</summary>
        public float Remaining => _remaining;

        /// <summary>True once the lifetime has run out.</summary>
        public bool IsExpired => _remaining <= 0f;

        /// <summary>Restart the countdown for pool reuse (GDD §12: a respawned shard starts fresh).</summary>
        public void Begin(float duration) => _remaining = duration;

        /// <summary>
        /// Advance the countdown by <paramref name="deltaTime"/>; while held the drain is scaled by
        /// <paramref name="heldFactor"/> (GDD §10 "slows strongly but does not fully pause"). Clamps at zero.
        /// </summary>
        public void Tick(float deltaTime, bool isHeld, float heldFactor)
        {
            _remaining -= isHeld ? deltaTime * heldFactor : deltaTime;
            if (_remaining < 0f)
            {
                _remaining = 0f;
            }
        }

        /// <summary>
        /// The lifetime a shard spawned now should get (GDD §12): the <paramref name="late"/> value once
        /// <paramref name="accepts"/> reaches <paramref name="lateAfterAccepts"/>, otherwise <paramref name="start"/>.
        /// </summary>
        public static float DurationForAccepts(int accepts, float start, float late, int lateAfterAccepts) =>
            accepts >= lateAfterAccepts ? late : start;
    }
}
