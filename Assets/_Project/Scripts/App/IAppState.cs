namespace StarforgeRelay.App
{
    /// <summary>
    /// One application state (GDD §23). The <see cref="AppStateMachine"/> drives the lifecycle:
    /// <see cref="Enter"/> on becoming current, <see cref="Tick"/> each frame while current, <see cref="Exit"/>
    /// on leaving. Round side-effects live in the machine's transition triggers, not here — in T13 most states
    /// keep empty hooks (reserved for T14 UI / T16 audio); only RoundComplete uses <see cref="Tick"/>.
    /// </summary>
    public interface IAppState
    {
        /// <summary>The phase this state represents.</summary>
        AppPhase Phase { get; }

        /// <summary>Called once when this state becomes current.</summary>
        void Enter();

        /// <summary>Called once when this state stops being current.</summary>
        void Exit();

        /// <summary>Called each frame while current, with unscaled delta (so it runs even when the game is paused via timeScale).</summary>
        void Tick(float unscaledDeltaTime);
    }
}
