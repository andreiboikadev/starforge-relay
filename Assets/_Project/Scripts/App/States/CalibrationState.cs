namespace StarforgeRelay.App
{
    /// <summary>Calibration / comfort note (GDD §16). T13 placeholder — UI + recenter are wired in T14.</summary>
    public sealed class CalibrationState : IAppState
    {
        public AppPhase Phase => AppPhase.Calibration;

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
