using NUnit.Framework;
using StarforgeRelay.Gameplay;

namespace StarforgeRelay.Tests.EditMode
{
    public sealed class StabilizationProgressTests
    {
        private const int Requirement = 20; // GDD §12

        private static StabilizationProgress NewProgress() => new StabilizationProgress(Requirement);

        [Test]
        public void Starts_At_Zero_Not_Complete()
        {
            var progress = NewProgress();
            Assert.AreEqual(0, progress.Current);
            Assert.IsFalse(progress.IsComplete);
        }

        [Test]
        public void Correct_Advances_By_One()
        {
            var progress = NewProgress();
            progress.RegisterCorrect();
            progress.RegisterCorrect();
            Assert.AreEqual(2, progress.Current);
        }

        [Test]
        public void Not_Complete_At_Nineteen()
        {
            var progress = NewProgress();
            for (int i = 0; i < 19; i++)
            {
                progress.RegisterCorrect();
            }
            Assert.IsFalse(progress.IsComplete, "19 correct must not be victory");
        }

        [Test]
        public void Complete_At_Exactly_Twenty()
        {
            var progress = NewProgress();
            for (int i = 0; i < 20; i++)
            {
                progress.RegisterCorrect();
            }
            Assert.IsTrue(progress.IsComplete, "20 correct must be victory");
        }
    }
}
