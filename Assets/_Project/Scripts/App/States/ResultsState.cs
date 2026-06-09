namespace StarforgeRelay.App
{
    /// <summary>
    /// Results (GDD §16). The snapshot to display is <see cref="AppStateMachine.LastResult"/>. T13 placeholder
    /// — the results view + Play-Again / Main-Menu buttons are wired in T14.
    /// </summary>
    public sealed class ResultsState : IAppState
    {
        public AppPhase Phase => AppPhase.Results;

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
