using System.Collections.Generic;
using NUnit.Framework;
using StarforgeRelay.Gameplay;

namespace StarforgeRelay.Tests.EditMode
{
    public sealed class ShardSpawnPlannerTests
    {
        private const int Target = 4;        // GDD §12 activeShardsDefault
        private const int MaxPerColor = 3;   // GDD §12
        private const float DelayMin = 0.3f; // GDD §12
        private const float DelayMax = 0.8f; // GDD §12

        private static readonly ShardColor[] AllColors = { ShardColor.Solar, ShardColor.Ion, ShardColor.Pulse };
        private static readonly SpawnArea Area = new SpawnArea(-50f, 50f, 1.0f, 1.4f);

        private static List<FeederPadSlot> FourValidPads() => new List<FeederPadSlot>
        {
            new FeederPadSlot(1, -40f, 1.1f),
            new FeederPadSlot(2, -15f, 1.2f),
            new FeederPadSlot(3, 15f, 1.2f),
            new FeederPadSlot(4, 40f, 1.1f),
        };

        private static ShardSpawnPlanner NewPlanner(
            List<FeederPadSlot> pads = null,
            IReadOnlyList<ShardColor> colors = null,
            int target = Target,
            IRandom random = null)
        {
            return new ShardSpawnPlanner(
                pads ?? FourValidPads(),
                Area,
                colors ?? AllColors,
                target,
                MaxPerColor,
                DelayMin,
                DelayMax,
                random ?? new FakeRandom());
        }

        [Test]
        public void Plans_Up_To_Target_Then_Stops()
        {
            var planner = NewPlanner();
            for (int i = 0; i < Target; i++)
            {
                Assert.IsTrue(planner.TryPlanNextSpawn(out _), $"spawn {i + 1} should plan");
            }

            Assert.AreEqual(Target, planner.ActiveCount);
            Assert.IsTrue(planner.IsAtTarget);
            Assert.IsFalse(planner.TryPlanNextSpawn(out _), "must not exceed the active target");
        }

        [Test]
        public void Never_Exceeds_Max_Per_Color()
        {
            // One color available, cap 3, target 4: the per-color cap must bind before the target.
            var planner = NewPlanner(colors: new[] { ShardColor.Solar });

            int planned = 0;
            while (planner.TryPlanNextSpawn(out _))
            {
                planned++;
            }

            Assert.AreEqual(MaxPerColor, planned, "cap of 3 per color must hold even below target");
            Assert.AreEqual(MaxPerColor, planner.CountOfColor(ShardColor.Solar));
        }

        [Test]
        public void Keeps_At_Least_Two_Colors_At_Target()
        {
            var planner = NewPlanner();
            while (planner.TryPlanNextSpawn(out _)) { }

            int distinct = 0;
            foreach (ShardColor color in AllColors)
            {
                if (planner.CountOfColor(color) > 0)
                {
                    distinct++;
                }
            }

            Assert.GreaterOrEqual(distinct, 2, "at least 2 colors must be active at target");
        }

        [Test]
        public void Prefers_Under_Represented_Color()
        {
            var planner = NewPlanner();

            // All colors tied at 0 → the fake picks the first = Solar.
            Assert.IsTrue(planner.TryPlanNextSpawn(out ShardSpawnPlan first));
            Assert.AreEqual(ShardColor.Solar, first.Color);

            // Solar now has 1; the next spawn must prefer an under-represented (0-count) color.
            Assert.IsTrue(planner.TryPlanNextSpawn(out ShardSpawnPlan second));
            Assert.AreNotEqual(ShardColor.Solar, second.Color, "must prefer a less-represented color");
        }

        [Test]
        public void Respawn_Delay_Always_Within_Bounds()
        {
            var planner = NewPlanner(random: new SeededRandom(12345));

            for (int i = 0; i < 1000; i++)
            {
                float delay = planner.NextRespawnDelay();
                Assert.GreaterOrEqual(delay, DelayMin);
                Assert.LessOrEqual(delay, DelayMax);
            }
        }

        [Test]
        public void Reserved_Pad_Not_Reused_Until_Released()
        {
            // A single pad makes reservation observable: the first plan takes it, the second has no free pad.
            var pads = new List<FeederPadSlot> { new FeederPadSlot(7, 0f, 1.2f) };
            var planner = NewPlanner(pads: pads);

            Assert.IsTrue(planner.TryPlanNextSpawn(out ShardSpawnPlan first));
            Assert.AreEqual(7, first.PadId);
            Assert.IsFalse(planner.TryPlanNextSpawn(out _), "a reserved pad must not double-spawn");

            planner.Release(7);
            Assert.IsTrue(planner.TryPlanNextSpawn(out ShardSpawnPlan again), "a released pad is reusable");
            Assert.AreEqual(7, again.PadId);
        }

        [Test]
        public void Invalid_Slots_Never_Returned()
        {
            var pads = new List<FeederPadSlot>
            {
                new FeederPadSlot(1, 0f, 1.2f),  // valid
                new FeederPadSlot(2, 80f, 1.2f), // outside the ±50° arc (too wide / toward behind)
                new FeederPadSlot(3, 0f, 2.5f),  // above the height band
            };
            var planner = NewPlanner(pads: pads, target: 9);

            while (planner.TryPlanNextSpawn(out ShardSpawnPlan plan))
            {
                Assert.AreEqual(1, plan.PadId, "only the in-reach pad may be returned");
            }

            Assert.AreEqual(1, planner.ActiveCount, "only 1 valid pad exists");
        }
    }
}
