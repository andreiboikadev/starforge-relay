using System;

namespace StarforgeRelay.Gameplay
{
    /// <summary>
    /// The round countdown (GDD §12: 90 s → time-out). Pure C#: no <c>MonoBehaviour</c>, no Unity statics —
    /// the caller passes <c>deltaTime</c>, so it ticks identically in EditMode tests and on device
    /// (guardrails §11, §17). Owns only the clock; pausing the rest of the round (spawning, lifetimes) is
    /// coordinated centrally by <c>RoundController</c> (T06) / app flow, not here.
    /// </summary>
    public sealed class RoundTimer
    {
        /// <param name="roundDurationSeconds">Round length in seconds (GDD: 90). Injected from RoundConfig.</param>
        public RoundTimer(float roundDurationSeconds)
        {
            Remaining = roundDurationSeconds;
        }

        /// <summary>Seconds left in the round (drives the victory time bonus in <see cref="ScoreService"/>).</summary>
        public float Remaining { get; private set; }

        /// <summary>True while paused — <see cref="Tick"/> is a no-op (GDD §16: pause holds the clock).</summary>
        public bool IsPaused { get; private set; }

        /// <summary>Time has run out — the round times out (GDD §12: exactly when <see cref="Remaining"/> reaches 0).</summary>
        public bool IsTimedOut => Remaining <= 0f;

        /// <summary>
        /// Advance the clock by <paramref name="deltaTime"/> seconds. Ignored while paused; non-positive
        /// deltas are ignored (no time travel); <see cref="Remaining"/> clamps at 0 and never goes negative.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (IsPaused || deltaTime <= 0f)
            {
                return;
            }

            Remaining = Math.Max(0f, Remaining - deltaTime);
        }

        /// <summary>Hold the clock; the toggling frame neither loses nor double-counts time (only <see cref="Tick"/> advances).</summary>
        public void Pause() => IsPaused = true;

        /// <summary>Resume counting from the held remaining time.</summary>
        public void Resume() => IsPaused = false;
    }
}
