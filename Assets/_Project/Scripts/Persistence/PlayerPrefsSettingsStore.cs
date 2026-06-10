using UnityEngine;

namespace StarforgeRelay.Persistence
{
    /// <summary>
    /// <see cref="ISettingsStore"/> backed by <see cref="PlayerPrefs"/> (guardrails §8 — a Unity static API
    /// wrapped at the boundary; nothing else in the app reads/writes <c>PlayerPrefs</c>). An absent key reads
    /// as its <see cref="GameSettings.Default"/> value, so a fresh install starts with sound and haptics on.
    /// </summary>
    public sealed class PlayerPrefsSettingsStore : ISettingsStore
    {
        private const string SoundKey = "settings.sound";
        private const string HapticsKey = "settings.haptics";

        /// <inheritdoc />
        public GameSettings Load()
        {
            bool sound = ReadBool(SoundKey, GameSettings.Default.SoundEnabled);
            bool haptics = ReadBool(HapticsKey, GameSettings.Default.HapticsEnabled);
            return new GameSettings(sound, haptics);
        }

        /// <inheritdoc />
        public void Save(GameSettings settings)
        {
            PlayerPrefs.SetInt(SoundKey, settings.SoundEnabled ? 1 : 0);
            PlayerPrefs.SetInt(HapticsKey, settings.HapticsEnabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        private static bool ReadBool(string key, bool fallback)
        {
            return PlayerPrefs.GetInt(key, fallback ? 1 : 0) != 0;
        }
    }
}
