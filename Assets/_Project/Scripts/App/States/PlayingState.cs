namespace StarforgeRelay.App
{
    /// <summary>
    /// Playing (GDD §23). The round itself is started by the machine's <c>RequestStartRound</c> trigger (not
    /// here — so a resume from Paused does not restart it) and ticked by the round-loop adapter. T13 placeholder
    /// hooks; HUD is wired in T14.
    /// </summary>
    public sealed class PlayingState : IAppState
    {
        public AppPhase Phase => AppPhase.Playing;

        public void Enter()
        {
        }

        public void Exit()
        {
        }

        public void Tick(float unscaledDeltaTime)
        {
        }
    }
}
