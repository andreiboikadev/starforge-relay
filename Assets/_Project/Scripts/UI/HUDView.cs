using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StarforgeRelay.UI
{
    /// <summary>
    /// Diegetic gameplay HUD (GDD §16). Dumb view: the <c>HUDPresenter</c> formats round state and calls these
    /// setters; the view only displays. Primitive text in T14 (ring segments / bars are art polish, T17–19).
    /// </summary>
    public sealed class HUDView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private TextMeshProUGUI _scoreText;
        [SerializeField] private TextMeshProUGUI _comboText;
        [SerializeField] private TextMeshProUGUI _chargeText;
        [SerializeField] private TextMeshProUGUI _heatText;

        [Tooltip("Pause button on the HUD — enters the Paused state.")]
        [SerializeField] private Button _pauseButton;

        /// <summary>Raised when the player clicks Pause on the HUD (enters Paused).</summary>
        public event Action PauseClicked;

        private void OnEnable()
        {
            if (_pauseButton != null)
            {
                _pauseButton.onClick.AddListener(RaisePause);
            }
        }

        private void OnDisable()
        {
            if (_pauseButton != null)
            {
                _pauseButton.onClick.RemoveListener(RaisePause);
            }
        }

        private void RaisePause() => PauseClicked?.Invoke();

        /// <summary>Set the timer readout (already formatted as mm:ss by the presenter).</summary>
        public void SetTimer(string text)
        {
            if (_timerText != null)
            {
                _timerText.text = text;
            }
        }

        public void SetScore(int score)
        {
            if (_scoreText != null)
            {
                _scoreText.text = $"Score {score}";
            }
        }

        public void SetCombo(int combo)
        {
            if (_comboText != null)
            {
                _comboText.text = combo > 1 ? $"x{combo}" : string.Empty;
            }
        }

        public void SetCharge(int current, int required)
        {
            if (_chargeText != null)
            {
                _chargeText.text = $"Charge {current}/{required}";
            }
        }

        public void SetHeat(int current, int cap)
        {
            if (_heatText != null)
            {
                _heatText.text = $"Heat {current}/{cap}";
            }
        }
    }
}
