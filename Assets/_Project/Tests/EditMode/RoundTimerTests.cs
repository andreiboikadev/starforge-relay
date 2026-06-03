using NUnit.Framework;
using StarforgeRelay.Gameplay;

namespace StarforgeRelay.Tests.EditMode
{
    public sealed class RoundTimerTests
    {
        private const float Duration = 90f; // GDD §12
        private const float Tol = 1e-4f;

        private static RoundTimer NewTimer() => new RoundTimer(Duration);

        [Test]
        public void Starts_At_Duration_Not_TimedOut()
        {
            var timer = NewTimer();
            Assert.AreEqual(90f, timer.Remaining, Tol);
            Assert.IsFalse(timer.IsTimedOut);
        }

        [Test]
        public void Tick_Counts_Down_By_Delta()
        {
            var timer = NewTimer();
            timer.Tick(1f);
            Assert.AreEqual(89f, timer.Remaining, Tol);
        }

        [Test]
        public void Multiple_Ticks_Accumulate()
        {
            var timer = NewTimer();
            timer.Tick(1f);
            timer.Tick(2f);
            timer.Tick(7f);
            Assert.AreEqual(80f, timer.Remaining, Tol);
        }

        [Test]
        public void Paused_Tick_Does_Nothing()
        {
            var timer = NewTimer();
            timer.Tick(5f);
            timer.Pause();
            timer.Tick(10f);
            Assert.AreEqual(85f, timer.Remaining, Tol);
            Assert.IsTrue(timer.IsPaused);
        }

        [Test]
        public void Resume_Continues_From_Held_Remaining()
        {
            var timer = NewTimer();
            timer.Tick(5f);
            timer.Pause();
            timer.Tick(100f); // ignored while paused
            timer.Resume();
            timer.Tick(5f);
            Assert.AreEqual(80f, timer.Remaining, Tol);
            Assert.IsFalse(timer.IsPaused);
        }

        [Test]
        public void Not_TimedOut_Before_Zero()
        {
            var timer = NewTimer();
            timer.Tick(89.5f);
            Assert.IsFalse(timer.IsTimedOut, "0.5 s left must not time out");
        }

        [Test]
        public void TimedOut_At_Zero()
        {
            var timer = NewTimer();
            timer.Tick(90f);
            Assert.AreEqual(0f, timer.Remaining, Tol);
            Assert.IsTrue(timer.IsTimedOut);
        }

        [Test]
        public void Remaining_Never_Goes_Negative()
        {
            var timer = NewTimer();
            timer.Tick(1000f);
            Assert.AreEqual(0f, timer.Remaining, Tol);
            Assert.IsTrue(timer.IsTimedOut);
        }

        [Test]
        public void Negative_Delta_Does_Not_Add_Time()
        {
            var timer = NewTimer();
            timer.Tick(10f);
            timer.Tick(-5f); // guarded: no time travel
            Assert.AreEqual(80f, timer.Remaining, Tol);
        }
    }
}
