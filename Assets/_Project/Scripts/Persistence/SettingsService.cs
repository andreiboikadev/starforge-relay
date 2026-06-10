using System;

namespace StarforgeRelay.Persistence
{
    /// <summary>
    /// Runtime owner of the player's <see cref="GameSettings"/> (guardrails §6). Loads from an
    /// <see cref="ISettingsStore"/> on construction, exposes the <see cref="Current"/> snapshot, and on each
    /// real change persists it and raises <see cref="Changed"/> so consumers react — the Settings UI now, and
    /// AudioService / HapticService in T16. Plain C# (no <c>MonoBehaviour</c>) so it is unit-testable headless.
    /// </summary>
    public sealed class SettingsService
    {
        private readonly ISettingsStore _store;

        /// <summary>Build over a store and load the persisted settings (or defaults).</summary>
        public SettingsService(ISettingsStore store)
        {
            _store = store;
            Current = _store.Load();
        }

        /// <summary>Raised after a setting actually changes, carrying the new snapshot.</summary>
        public event Action<GameSettings> Changed;

        /// <summary>The current settings snapshot.</summary>
        public GameSettings Current { get; private set; }

        /// <summary>Enable/disable sound; persists and raises <see cref="Changed"/> only if the value changed.</summary>
        public void SetSoundEnabled(bool enabled)
        {
            if (Current.SoundEnabled == enabled)
            {
                return;
            }

            Apply(Current.WithSound(enabled));
        }

        /// <summary>Enable/disable haptics; persists and raises <see cref="Changed"/> only if the value changed.</summary>
        public void SetHapticsEnabled(bool enabled)
        {
            if (Current.HapticsEnabled == enabled)
            {
                return;
            }

            Apply(Current.WithHaptics(enabled));
        }

        private void Apply(GameSettings next)
        {
            Current = next;
            _store.Save(next);
            Changed?.Invoke(next);
        }
    }
}
