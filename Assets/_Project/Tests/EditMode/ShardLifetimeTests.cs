using NUnit.Framework;
using StarforgeRelay.Gameplay;

namespace StarforgeRelay.Tests.EditMode
{
    public sealed class ShardLifetimeTests
    {
        [Test]
        public void Counts_Down_When_Free()
        {
            var life = new ShardLifetime(14f);
            life.Tick(2f, isHeld: false, heldFactor: 0.25f);
            Assert.AreEqual(12f, life.Remaining, 1e-4f);
            Assert.IsFalse(life.IsExpired);
        }

        [Test]
        public void Held_Drains_Slower_But_Not_Paused()
        {
            var life = new ShardLifetime(14f);
            life.Tick(2f, isHeld: true, heldFactor: 0.25f);
            Assert.AreEqual(13.5f, life.Remaining, 1e-4f); // 14 − 2×0.25
            Assert.Less(life.Remaining, 14f);              // still decreases — not fully paused
        }

        [Test]
        public void Expires_At_Zero_And_Clamps()
        {
            var life = new ShardLifetime(1f);
            life.Tick(5f, isHeld: false, heldFactor: 0.25f);
            Assert.AreEqual(0f, life.Remaining);
            Assert.IsTrue(life.IsExpired);
        }

        [Test]
        public void Not_Expired_Before_Zero()
        {
            var life = new ShardLifetime(0.5f);
            Assert.IsFalse(life.IsExpired);
            life.Tick(0.4f, isHeld: false, heldFactor: 0.25f);
            Assert.IsFalse(life.IsExpired);
        }

        [Test]
        public void Begin_Resets_For_Pool_Reuse()
        {
            var life = new ShardLifetime(14f);
            life.Tick(14f, isHeld: false, heldFactor: 0.25f);
            Assert.IsTrue(life.IsExpired);

            life.Begin(12f);
            Assert.AreEqual(12f, life.Remaining, 1e-4f);
            Assert.IsFalse(life.IsExpired);
        }

        [Test]
        public void DurationForAccepts_Switches_At_Threshold()
        {
            Assert.AreEqual(14f, ShardLifetime.DurationForAccepts(9, 14f, 12f, 10));
            Assert.AreEqual(12f, ShardLifetime.DurationForAccepts(10, 14f, 12f, 10)); // GDD "after 10 accepts"
            Assert.AreEqual(12f, ShardLifetime.DurationForAccepts(20, 14f, 12f, 10));
        }
    }
}
