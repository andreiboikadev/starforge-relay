using System;
using UnityEngine;
using UnityEngine.UI;

namespace StarforgeRelay.UI
{
    /// <summary>
    /// Pause menu (GDD §16). Dumb view: exposes Resume / Restart / Settings / Main Menu intents. The freeze
    /// itself is the state machine's Paused phase (<c>timeScale</c> + insert gate, T13); this view just surfaces
    /// the buttons. Settings opens as an overlay handled by the <c>AppFlowController</c>.
    /// </summary>
    public sealed class PauseMenuView : MonoBehaviour
    {
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _mainMenuButton;

        /// <summary>Raised when the player clicks Resume.</summary>
        public event Action ResumeClicked;

        /// <summary>Raised when the player clicks Restart.</summary>
        public event Action RestartClicked;

        /// <summary>Raised when the player clicks Settings.</summary>
        public event Action SettingsClicked;

        /// <summary>Raised when the player clicks Main Menu.</summary>
        public event Action MainMenuClicked;

        private void OnEnable()
        {
            if (_resumeButton != null)
            {
                _resumeButton.onClick.AddListener(RaiseResume);
            }

            if (_restartButton != null)
            {
                _restartButton.onClick.AddListener(RaiseRestart);
            }

            if (_settingsButton != null)
            {
                _settingsButton.onClick.AddListener(RaiseSettings);
            }

            if (_mainMenuButton != null)
            {
                _mainMenuButton.onClick.AddListener(RaiseMainMenu);
            }
        }

        private void OnDisable()
        {
            if (_resumeButton != null)
            {
                _resumeButton.onClick.RemoveListener(RaiseResume);
            }

            if (_restartButton != null)
            {
                _restartButton.onClick.RemoveListener(RaiseRestart);
            }

            if (_settingsButton != null)
            {
                _settingsButton.onClick.RemoveListener(RaiseSettings);
            }

            if (_mainMenuButton != null)
            {
                _mainMenuButton.onClick.RemoveListener(RaiseMainMenu);
            }
        }

        private void RaiseResume() => ResumeClicked?.Invoke();

        private void RaiseRestart() => RestartClicked?.Invoke();

        private void RaiseSettings() => SettingsClicked?.Invoke();

        private void RaiseMainMenu() => MainMenuClicked?.Invoke();
    }
}
