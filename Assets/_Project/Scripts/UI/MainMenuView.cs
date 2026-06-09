using System;
using UnityEngine;
using UnityEngine.UI;

namespace StarforgeRelay.UI
{
    /// <summary>
    /// Main-menu screen (GDD §16). Dumb world-space view: it exposes the Play intent as a typed event; the
    /// <c>AppFlowController</c> routes it to the state machine. (How To / Settings / Credits are deferred — T15/later.)
    /// </summary>
    public sealed class MainMenuView : MonoBehaviour
    {
        [Tooltip("Primary Play button.")]
        [SerializeField] private Button _playButton;

        /// <summary>Raised when the player clicks Play.</summary>
        public event Action PlayClicked;

        private void OnEnable()
        {
            if (_playButton != null)
            {
                _playButton.onClick.AddListener(RaisePlay);
            }
        }

        private void OnDisable()
        {
            if (_playButton != null)
            {
                _playButton.onClick.RemoveListener(RaisePlay);
            }
        }

        private void RaisePlay() => PlayClicked?.Invoke();
    }
}
