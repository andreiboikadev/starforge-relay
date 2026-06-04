using System;

namespace StarforgeRelay.Gameplay
{
    /// <summary>
    /// Owns the round score (GDD §13). Pure C#: no <c>MonoBehaviour</c>, no Unity statics — headless-testable
    /// (guardrails §16–17). Consumes the combo-milestone signal from <see cref="ComboTracker"/>; it does not
    /// recompute the milestone formula. Results reads <see cref="Score"/>; it never recomputes the total.
    /// </summary>
    public sealed class ScoreService
    {
        private readonly int _correctScore;
        private readonly int _comboBonusScore;
        private readonly int _victoryTimeBonusPerSecond;
        private readonly int _heatPenaltyPerHeat;

        /// <summary>All values injected from RoundConfig (GDD §13) so the rule stays SO-free and testable.</summary>
        public ScoreService(int correctScore, int comboBonusScore, int victoryTimeBonusPerSecond, int heatPenaltyPerHeat)
        {
            _correctScore = correctScore;
            _comboBonusScore = comboBonusScore;
            _victoryTimeBonusPerSecond = victoryTimeBonusPerSecond;
            _heatPenaltyPerHeat = heatPenaltyPerHeat;
        }

        /// <summary>Accumulated score.</summary>
        public int Score { get; private set; }

        /// <summary>A correct insert awards <c>correctScore</c> (GDD §13: +10).</summary>
        public void AddCorrect()
        {
            Score += _correctScore;
        }

        /// <summary>Each combo milestone awards <c>comboBonusScore</c> (GDD §12–13: +50 every 5).</summary>
        public void AddComboMilestoneBonus()
        {
            Score += _comboBonusScore;
        }

        /// <summary>On victory, award remaining whole seconds × <c>victoryTimeBonusPerSecond</c> (GDD §13).</summary>
        public void AddVictoryTimeBonus(int remainingSeconds)
        {
            if (remainingSeconds < 0)
            {
                remainingSeconds = 0;
            }

            Score += remainingSeconds * _victoryTimeBonusPerSecond;
        }

        /// <summary>
        /// On results, subtract <c>heat × heatPenaltyPerHeat</c>, clamped so the final score never drops
        /// below 0 (GDD §13).
        /// </summary>
        public void ApplyHeatPenalty(int heat)
        {
            Score = Math.Max(0, Score - heat * _heatPenaltyPerHeat);
        }
    }
}
