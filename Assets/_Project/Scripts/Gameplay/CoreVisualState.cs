namespace StarforgeRelay.Gameplay
{
    /// <summary>
    /// The reactor core's display state (GDD §10 Star Core). Derived from round events by
    /// <c>CoreStatePresenter</c> (T17) and rendered by <see cref="ReactorCoreView"/> — the view computes no
    /// progress/heat itself (guardrails §6/§16). The GDD "Active" state folds into <see cref="Charging"/> (the
    /// three ports are always present in the MVP scene, so there is no "ports appear" transition).
    /// </summary>
    public enum CoreVisualState
    {
        Dormant,
        Charging,
        Heating,
        AlmostStable,
        Stabilized,
        Overloaded
    }
}
