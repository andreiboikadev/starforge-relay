namespace StarforgeRelay.Persistence
{
    /// <summary>
    /// Immutable snapshot of the player's MVP settings (GDD §16/§29): sound and haptics on/off. Passed
    /// between <see cref="ISettingsStore"/> and <see cref="SettingsService"/>; a fresh install starts at
    /// <see cref="Default"/> (both enabled).
    /// </summary>
    public readonly struct GameSettings
    {
        /// <summary>Settings as a fresh install starts them — sound and haptics both enabled (GDD §16).</summary>
        public static readonly GameSettings Default = new GameSettings(true, true);

        /// <summary>Create a settings snapshot.</summary>
        public GameSettings(bool soundEnabled, bool hapticsEnabled)
        {
            SoundEnabled = soundEnabled;
            HapticsEnabled = hapticsEnabled;
        }

        /// <summary>Whether game/UI sound is enabled.</summary>
        public bool SoundEnabled { get; }

        /// <summary>Whether controller haptics are enabled.</summary>
        public bool HapticsEnabled { get; }

        /// <summary>Return a copy with <see cref="SoundEnabled"/> set to <paramref name="value"/>.</summary>
        public GameSettings WithSound(bool value) => new GameSettings(value, HapticsEnabled);

        /// <summary>Return a copy with <see cref="HapticsEnabled"/> set to <paramref name="value"/>.</summary>
        public GameSettings WithHaptics(bool value) => new GameSettings(SoundEnabled, value);
    }
}
