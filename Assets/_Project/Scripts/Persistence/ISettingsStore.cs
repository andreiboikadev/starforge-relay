namespace StarforgeRelay.Persistence
{
    /// <summary>
    /// Persistence boundary for <see cref="GameSettings"/> (guardrails §6/§8): the rest of the app goes
    /// through this, never through <c>PlayerPrefs</c> directly. <see cref="PlayerPrefsSettingsStore"/> is the
    /// production implementation; tests substitute an in-memory fake to stay headless.
    /// </summary>
    public interface ISettingsStore
    {
        /// <summary>Load the saved settings, or <see cref="GameSettings.Default"/> when nothing is saved.</summary>
        GameSettings Load();

        /// <summary>Persist the given settings.</summary>
        void Save(GameSettings settings);
    }
}
