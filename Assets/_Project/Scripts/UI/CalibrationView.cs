using System;
using UnityEngine;
using UnityEngine.UI;

namespace StarforgeRelay.UI
{
    /// <summary>
    /// Calibration / comfort-note screen (GDD §16, §28: "Stand comfortably and face forward"). Dumb view:
    /// exposes Start / Back intents. Recenter is deferred (GDD "if easy").
    /// </summary>
    public sealed class CalibrationView : MonoBehaviour
    {
        [Tooltip("Start the round.")]
        [SerializeField] private Button _startButton;

        [Tooltip("Back to the main menu.")]
        [SerializeField] private Button _backButton;

        /// <summary>Raised when the player clicks Start.</summary>
        public event Action StartClicked;

        /// <summary>Raised when the player clicks Back.</summary>
        public event Action BackClicked;

        private void OnEnable()
        {
            if (_startButton != null)
            {
                _startButton.onClick.AddListener(RaiseStart);
            }

            if (_backButton != null)
            {
                _backButton.onClick.AddListener(RaiseBack);
            }
        }

        private void OnDisable()
        {
            if (_startButton != null)
            {
                _startButton.onClick.RemoveListener(RaiseStart);
            }

            if (_backButton != null)
            {
                _backButton.onClick.RemoveListener(RaiseBack);
            }
        }

        private void RaiseStart() => StartClicked?.Invoke();

        private void RaiseBack() => BackClicked?.Invoke();
    }
}
