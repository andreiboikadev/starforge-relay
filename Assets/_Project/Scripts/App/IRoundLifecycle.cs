using System;
using StarforgeRelay.Gameplay;

namespace StarforgeRelay.App
{
    /// <summary>
    /// The seam between the app state machine and the round adapters (T11/T12). The machine's transition
    /// triggers call these; the implementor (<c>RoundLoopController</c>) owns the round + spawner. Keeps the
    /// state machine free of Unity/XR types and unit-testable with a fake.
    /// </summary>
    public interface IRoundLifecycle
    {
        /// <summary>Raised when the round reaches a terminal phase — carries the Results snapshot (GDD §16).</summary>
        event Action<RoundEndedEvent> RoundEnded;

        /// <summary>Start a fresh round: build/reset it, clear + fill shards, unfreeze time. Play / Play-Again / Restart.</summary>
        void StartRound();

        /// <summary>Freeze the active round (timer, shard lifetime, spawning) and block inserts.</summary>
        void PauseRound();

        /// <summary>Unfreeze the active round (does <b>not</b> restart it).</summary>
        void ResumeRound();

        /// <summary>Stop the round and clear active shards (return to menu); leaves no live round.</summary>
        void StopRound();
    }
}
