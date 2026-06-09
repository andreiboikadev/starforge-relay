namespace StarforgeRelay.App
{
    /// <summary>
    /// High-level application/round phases (GDD §23). The flat set the <see cref="AppStateMachine"/> moves
    /// through: Boot → MainMenu → Calibration → Playing ⇄ Paused → RoundComplete → Results. (TrackingLost is
    /// deferred — optional per GDD §23.)
    /// </summary>
    public enum AppPhase
    {
        Boot,
        MainMenu,
        Calibration,
        Playing,
        Paused,
        RoundComplete,
        Results
    }
}
