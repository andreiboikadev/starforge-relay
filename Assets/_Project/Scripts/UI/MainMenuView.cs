using System;
using UnityEngine;
using UnityEngine.UI;

namespace StarforgeRelay.UI
{
    /// <summary>
    /// Main-menu screen (GDD §16). Dumb world-space view: exposes Play and Settings intents as typed events;
    /// the <c>AppFlowController</c> routes Play to the state machine and opens Settings as an overlay.
    /// (How To / Credits remain deferred.)
    /// </summary>
    public sealed class MainMenuView : MonoBehaviour
    {
        [Tooltip("Primary Play button.")]
        [SerializeField] private Button _playButton;

        [Tooltip("Secondary Settings button (GDD §16).")]
        [SerializeField] private Button _settingsButton;

        /// <summary>Raised when the player clicks Play.</summary>
        public event Action PlayClicked;

        /// <summary>Raised when the player clicks Settings.</summary>
        public event Action SettingsClicked;

        private void OnEnable()
        {
            if (_playButton != null)
            {
                _playButton.onClick.AddListener(RaisePlay);
            }

            if (_settingsButton != null)
            {
                _settingsButton.onClick.AddListener(RaiseSettings);
            }
        }

        private void OnDisable()
        {
            if (_playButton != null)
            {
                _playButton.onClick.RemoveListener(RaisePlay);
            }

            if (_settingsButton != null)
            {
                _settingsButton.onClick.RemoveListener(RaiseSettings);
            }
        }

        private void RaisePlay() => PlayClicked?.Invoke();

        private void RaiseSettings() => SettingsClicked?.Invoke();
    }
}
