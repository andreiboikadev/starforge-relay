namespace StarforgeRelay.Audio
{
    /// <summary>
    /// The shared vocabulary of semantic feedback cues (GDD §18) that audio and haptics react to. Both
    /// <see cref="AudioCueConfig"/> and <see cref="HapticConfig"/> key off this enum; a cue with no config
    /// entry (or no clip) is a silent no-op, so optional cues need no special casing. The 10 GDD-§18 required
    /// sounds plus <see cref="TimedOut"/> (the GDD does not require a time-out sound — left unmapped by default).
    /// </summary>
    public enum FeedbackCue
    {
        UiSelect,
        RoundStart,
        GrabShard,
        ReleaseShard,
        CorrectInsert,
        WrongInsert,
        ShardExpired,
        ComboMilestone,
        Victory,
        Overload,
        TimedOut
    }
}
