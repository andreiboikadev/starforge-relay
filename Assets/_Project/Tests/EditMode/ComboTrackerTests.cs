using NUnit.Framework;
using StarforgeRelay.Gameplay;

namespace StarforgeRelay.Tests.EditMode
{
    public sealed class ComboTrackerTests
    {
        private const int Interval = 5; // GDD §12 combo milestone interval.

        private static ComboTracker NewTracker() => new ComboTracker(Interval);

        [Test]
        public void Starts_At_Zero()
        {
            Assert.AreEqual(0, NewTracker().Current);
        }

        [Test]
        public void Correct_Increments_Combo()
        {
            var combo = NewTracker();
            combo.RegisterCorrect();
            combo.RegisterCorrect();
            Assert.AreEqual(2, combo.Current);
        }

        [Test]
        public void Wrong_Resets_Combo()
        {
            var combo = NewTracker();
            combo.RegisterCorrect();
            combo.RegisterCorrect();
            combo.RegisterWrong();
            Assert.AreEqual(0, combo.Current);
        }

        [Test]
        public void Expired_While_Held_Resets_Combo()
        {
            var combo = NewTracker();
            combo.RegisterCorrect();
            combo.RegisterExpired(wasHeld: true);
            Assert.AreEqual(0, combo.Current);
        }

        [Test]
        public void Expired_While_Not_Held_Keeps_Combo()
        {
            var combo = NewTracker();
            combo.RegisterCorrect();
            combo.RegisterCorrect();
            combo.RegisterExpired(wasHeld: false);
            Assert.AreEqual(2, combo.Current);
        }

        [Test]
        public void Milestone_Reported_Every_Fifth()
        {
            var combo = NewTracker();

            for (int i = 1; i <= 4; i++)
            {
                Assert.IsFalse(combo.RegisterCorrect(), $"combo {i} should not be a milestone");
            }

            Assert.IsTrue(combo.RegisterCorrect(), "combo 5 should be a milestone");

            for (int i = 6; i <= 9; i++)
            {
                Assert.IsFalse(combo.RegisterCorrect(), $"combo {i} should not be a milestone");
            }

            Assert.IsTrue(combo.RegisterCorrect(), "combo 10 should be a milestone");
        }
    }
}
