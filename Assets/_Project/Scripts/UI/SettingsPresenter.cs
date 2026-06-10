using StarforgeRelay.Persistence;
using UnityEngine;

namespace StarforgeRelay.UI
{
    /// <summary>
    /// Binds the <see cref="SettingsView"/> to the <see cref="SettingsService"/> (T15): seeds the toggles from
    /// the current settings on init, and routes toggle changes back to the service (which persists and raises
    /// <c>Changed</c>). Presentation only — the service owns the state and persistence.
    /// </summary>
    public sealed class SettingsPresenter : MonoBehaviour
    {
        [SerializeField] private SettingsView _view;

        private SettingsService _settings;

        /// <summary>Receive the settings service from the composition root and seed the view.</summary>
        public void Initialize(SettingsService settings)
        {
            if (settings == null || _view == null)
            {
                Debug.LogError("[SettingsPresenter] Missing settings service or view — settings UI disabled.", this);
                return;
            }

            _settings = settings;
            _view.SoundToggled += OnSoundToggled;
            _view.HapticsToggled += OnHapticsToggled;

            _view.SetSound(_settings.Current.SoundEnabled);
            _view.SetHaptics(_settings.Current.HapticsEnabled);
        }

        private void OnDestroy()
        {
            if (_settings == null)
            {
                return;
            }

            _view.SoundToggled -= OnSoundToggled;
            _view.HapticsToggled -= OnHapticsToggled;
        }

        private void OnSoundToggled(bool on) => _settings.SetSoundEnabled(on);

        private void OnHapticsToggled(bool on) => _settings.SetHapticsEnabled(on);
    }
}
