namespace StarforgeRelay.App
{
    /// <summary>
    /// RoundComplete (GDD §23): a brief beat after the round ends before Results. T13 just counts down the
    /// delay (the victory / overload / time-out feedback content — and its per-result timing, GDD §12 — is
    /// T17), then advances to Results.
    /// </summary>
    public sealed class RoundCompleteState : IAppState
    {
        private readonly AppStateMachine _machine;
        private readonly float _delaySeconds;
        private float _remaining;

        public RoundCompleteState(AppStateMachine machine, float delaySeconds)
        {
            _machine = machine;
            _delaySeconds = delaySeconds;
        }

        public AppPhase Phase => AppPhase.RoundComplete;

        public void Enter() => _remaining = _delaySeconds;

        public void Exit()
        {
        }

        public void Tick(float unscaledDeltaTime)
        {
            _remaining -= unscaledDeltaTime;
            if (_remaining <= 0f)
            {
                _machine.AdvanceToResults();
            }
        }
    }
}
