using NUnit.Framework;
using StarforgeRelay.Gameplay;

namespace StarforgeRelay.Tests.EditMode
{
    public sealed class ScoreServiceTests
    {
        // GDD §13 numbers, passed in directly (rules are config-injected, not SO-coupled).
        private static ScoreService NewService() => new ScoreService(
            correctScore: 10,
            comboBonusScore: 50,
            victoryTimeBonusPerSecond: 2,
            heatPenaltyPerHeat: 10);

        [Test]
        public void Starts_At_Zero()
        {
            Assert.AreEqual(0, NewService().Score);
        }

        [Test]
        public void AddCorrect_Awards_Ten_Each()
        {
            var score = NewService();
            score.AddCorrect();
            score.AddCorrect();
            score.AddCorrect();
            Assert.AreEqual(30, score.Score);
        }

        [Test]
        public void ComboMilestoneBonus_Awards_Fifty()
        {
            var score = NewService();
            score.AddComboMilestoneBonus();
            Assert.AreEqual(50, score.Score);
        }

        [Test]
        public void VictoryTimeBonus_Is_RemainingSeconds_Times_Two()
        {
            var score = NewService();
            score.AddVictoryTimeBonus(15);
            Assert.AreEqual(30, score.Score);
        }

        [Test]
        public void HeatPenalty_Subtracts_Ten_Per_Heat()
        {
            var score = NewService();
            score.AddCorrect();             // 10
            score.AddCorrect();             // 20
            score.AddComboMilestoneBonus(); // 70
            score.ApplyHeatPenalty(3);      // 70 - 30 = 40
            Assert.AreEqual(40, score.Score);
        }

        [Test]
        public void HeatPenalty_Never_Drops_Below_Zero()
        {
            var score = NewService();
            score.AddCorrect();        // 10
            score.ApplyHeatPenalty(8); // 10 - 80 -> clamp to 0
            Assert.AreEqual(0, score.Score);
        }
    }
}
