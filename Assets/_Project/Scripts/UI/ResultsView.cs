using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace StarforgeRelay.UI
{
    /// <summary>
    /// Results screen (GDD §16). Dumb view: <see cref="SetResults"/> fills the read-only snapshot the
    /// presenter passes in (it never recomputes — guardrails §16), and it exposes Play Again / Main Menu intents.
    /// </summary>
    public sealed class ResultsView : MonoBehaviour
    {
        [Header("Text")]
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _starsText;
        [SerializeField] private TextMeshProUGUI _correctText;
        [SerializeField] private TextMeshProUGUI _heatText;
        [SerializeField] private TextMeshProUGUI _scoreText;

        [Header("Buttons")]
        [SerializeField] private Button _playAgainButton;
        [SerializeField] private Button _mainMenuButton;

        /// <summary>Raised when the player clicks Play Again.</summary>
        public event Action PlayAgainClicked;

        /// <summary>Raised when the player clicks Main Menu.</summary>
        public event Action MainMenuClicked;

        /// <summary>Fill the screen from the finalized round snapshot (the presenter formats; the view only displays).</summary>
        public void SetResults(string title, int stars, int correct, int heat, int score)
        {
            if (_titleText != null)
            {
                _titleText.text = title;
            }

            if (_starsText != null)
            {
                _starsText.text = $"{stars}/3";
            }

            if (_correctText != null)
            {
                _correctText.text = $"Shards {correct}";
            }

            if (_heatText != null)
            {
                _heatText.text = $"Heat {heat}";
            }

            if (_scoreText != null)
            {
                _scoreText.text = $"Score {score}";
            }
        }

        private void OnEnable()
        {
            if (_playAgainButton != null)
            {
                _playAgainButton.onClick.AddListener(RaisePlayAgain);
            }

            if (_mainMenuButton != null)
            {
                _mainMenuButton.onClick.AddListener(RaiseMainMenu);
            }
        }

        private void OnDisable()
        {
            if (_playAgainButton != null)
            {
                _playAgainButton.onClick.RemoveListener(RaisePlayAgain);
            }

            if (_mainMenuButton != null)
            {
                _mainMenuButton.onClick.RemoveListener(RaiseMainMenu);
            }
        }

        private void RaisePlayAgain() => PlayAgainClicked?.Invoke();

        private void RaiseMainMenu() => MainMenuClicked?.Invoke();
    }
}
