namespace StarforgeRelay.App
{
    /// <summary>Boot (GDD §23): on its first tick, advances to the main menu. No asset loading needed yet.</summary>
    public sealed class BootState : IAppState
    {
        private readonly AppStateMachine _machine;

        public BootState(AppStateMachine machine)
        {
            _machine = machine;
        }

        public AppPhase Phase => AppPhase.Boot;

        public void Enter()
        {
        }

        public void Exit()
        {
        }

        public void Tick(float unscaledDeltaTime) => _machine.AdvanceFromBoot();
    }
}
