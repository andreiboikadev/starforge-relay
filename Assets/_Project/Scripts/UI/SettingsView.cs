using System;
using UnityEngine;
using UnityEngine.UI;

namespace StarforgeRelay.UI
{
    /// <summary>
    /// Settings screen (GDD §16). Dumb world-space view: two on/off toggles (sound, haptics) + Back. Exposes
    /// the toggle changes and Back as typed events; the <see cref="SettingsPresenter"/> binds them to the
    /// settings service and the <c>AppFlowController</c> handles Back. Seeding a toggle via <see cref="SetSound"/>
    /// / <see cref="SetHaptics"/> uses <c>SetIsOnWithoutNotify</c>, so initializing the UI does not re-raise the
    /// change event back into the service.
    /// </summary>
    public sealed class SettingsView : MonoBehaviour
    {
        [SerializeField] private Toggle _soundToggle;
        [SerializeField] private Toggle _hapticsToggle;
        [SerializeField] private Button _backButton;

        /// <summary>Raised when the player toggles sound (carries the new value).</summary>
        public event Action<bool> SoundToggled;

        /// <summary>Raised when the player toggles haptics (carries the new value).</summary>
        public event Action<bool> HapticsToggled;

        /// <summary>Raised when the player clicks Back.</summary>
        public event Action BackClicked;

        /// <summary>Set the sound toggle without raising <see cref="SoundToggled"/> (seeding from saved settings).</summary>
        public void SetSound(bool on)
        {
            if (_soundToggle != null)
            {
                _soundToggle.SetIsOnWithoutNotify(on);
            }
        }

        /// <summary>Set the haptics toggle without raising <see cref="HapticsToggled"/>.</summary>
        public void SetHaptics(bool on)
        {
            if (_hapticsToggle != null)
            {
                _hapticsToggle.SetIsOnWithoutNotify(on);
            }
        }

        private void OnEnable()
        {
            if (_soundToggle != null)
            {
                _soundToggle.onValueChanged.AddListener(RaiseSound);
            }

            if (_hapticsToggle != null)
            {
                _hapticsToggle.onValueChanged.AddListener(RaiseHaptics);
            }

            if (_backButton != null)
            {
                _backButton.onClick.AddListener(RaiseBack);
            }
        }

        private void OnDisable()
        {
            if (_soundToggle != null)
            {
                _soundToggle.onValueChanged.RemoveListener(RaiseSound);
            }

            if (_hapticsToggle != null)
            {
                _hapticsToggle.onValueChanged.RemoveListener(RaiseHaptics);
            }

            if (_backButton != null)
            {
                _backButton.onClick.RemoveListener(RaiseBack);
            }
        }

        private void RaiseSound(bool on) => SoundToggled?.Invoke(on);

        private void RaiseHaptics(bool on) => HapticsToggled?.Invoke(on);

        private void RaiseBack() => BackClicked?.Invoke();
    }
}
