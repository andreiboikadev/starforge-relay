namespace StarforgeRelay.App
{
    /// <summary>
    /// RoundComplete (GDD §23): a brief beat after the round ends before Results, while the victory / overload /
    /// time-out feedback plays (T17). The beat length is per-result (GDD §12: victory 2 s / overload 1.5 s /
    /// time-out 1 s) — selected on <see cref="Enter"/> from the machine's last result — then it advances to
    /// Results.
    /// </summary>
    public sealed class RoundCompleteState : IAppState
    {
        private readonly AppStateMachine _machine;
        private readonly RoundCompleteDelays _delays;
        private float _remaining;

        public RoundCompleteState(AppStateMachine machine, RoundCompleteDelays delays)
        {
            _machine = machine;
            _delays = delays;
        }

        public AppPhase Phase => AppPhase.RoundComplete;

        public void Enter() => _remaining = _delays.For(_machine.LastResult.Result);

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
