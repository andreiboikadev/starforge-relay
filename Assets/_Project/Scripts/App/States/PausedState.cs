namespace StarforgeRelay.App
{
    /// <summary>
    /// Paused (GDD §16). The freeze (timer / shard lifetime / spawning) is applied by the machine's
    /// <c>RequestPause</c> trigger via the lifecycle (<c>Time.timeScale = 0</c> + insert gate); resume restores
    /// it. T13 placeholder hooks; the pause menu is wired in T14.
    /// </summary>
    public sealed class PausedState : IAppState
    {
        public AppPhase Phase => AppPhase.Paused;

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
