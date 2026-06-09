namespace StarforgeRelay.App
{
    /// <summary>Main menu (GDD §16). T13 placeholder — the world-space UI is wired in T14.</summary>
    public sealed class MainMenuState : IAppState
    {
        public AppPhase Phase => AppPhase.MainMenu;

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
