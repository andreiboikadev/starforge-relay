using NUnit.Framework;
using StarforgeRelay.Gameplay;

namespace StarforgeRelay.Tests.EditMode
{
    public sealed class HeatServiceTests
    {
        private const int HeatCap = 8;         // GDD §12
        private const int ComboHeatRelief = 1; // GDD §12

        private static HeatService NewService() => new HeatService(HeatCap, ComboHeatRelief);

        [Test]
        public void Starts_At_Zero_Not_Overloaded()
        {
            var heat = NewService();
            Assert.AreEqual(0, heat.Current);
            Assert.IsFalse(heat.IsOverloaded);
        }

        [Test]
        public void WrongInsert_Adds_One()
        {
            var heat = NewService();
            heat.RegisterWrongInsert();
            heat.RegisterWrongInsert();
            Assert.AreEqual(2, heat.Current);
        }

        [Test]
        public void ExpiredShard_Adds_One()
        {
            var heat = NewService();
            heat.RegisterExpiredShard();
            Assert.AreEqual(1, heat.Current);
        }

        [Test]
        public void Milestone_Relieves_One_When_Above_Zero()
        {
            var heat = NewService();
            heat.RegisterWrongInsert();
            heat.RegisterWrongInsert(); // 2
            heat.RelieveAtMilestone();  // 1
            Assert.AreEqual(1, heat.Current);
        }

        [Test]
        public void Milestone_Relief_Does_Nothing_At_Zero()
        {
            var heat = NewService();
            heat.RelieveAtMilestone();
            Assert.AreEqual(0, heat.Current);
        }

        [Test]
        public void Heat_Never_Goes_Below_Zero()
        {
            var heat = NewService();
            heat.RegisterWrongInsert(); // 1
            heat.RelieveAtMilestone();  // 0
            heat.RelieveAtMilestone();  // still 0
            Assert.AreEqual(0, heat.Current);
        }

        [Test]
        public void Not_Overloaded_At_Seven()
        {
            var heat = NewService();
            for (int i = 0; i < 7; i++)
            {
                heat.RegisterWrongInsert();
            }
            Assert.AreEqual(7, heat.Current);
            Assert.IsFalse(heat.IsOverloaded, "heat 7 must not overload");
        }

        [Test]
        public void Overloaded_At_Exactly_Eight()
        {
            var heat = NewService();
            for (int i = 0; i < 8; i++)
            {
                heat.RegisterWrongInsert();
            }
            Assert.AreEqual(8, heat.Current);
            Assert.IsTrue(heat.IsOverloaded, "heat 8 must overload");
        }
    }
}
