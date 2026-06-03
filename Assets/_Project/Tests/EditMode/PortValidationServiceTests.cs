using NUnit.Framework;
using StarforgeRelay.Gameplay;

namespace StarforgeRelay.Tests.EditMode
{
    public sealed class PortValidationServiceTests
    {
        private static PortValidationService NewService() => new PortValidationService();

        [TestCase(ShardColor.Solar)]
        [TestCase(ShardColor.Ion)]
        [TestCase(ShardColor.Pulse)]
        public void Match_Is_Correct(ShardColor color)
        {
            Assert.AreEqual(InsertOutcome.Correct, NewService().Validate(color, color));
        }

        [TestCase(ShardColor.Solar, ShardColor.Ion)]
        [TestCase(ShardColor.Solar, ShardColor.Pulse)]
        [TestCase(ShardColor.Ion, ShardColor.Solar)]
        [TestCase(ShardColor.Ion, ShardColor.Pulse)]
        [TestCase(ShardColor.Pulse, ShardColor.Solar)]
        [TestCase(ShardColor.Pulse, ShardColor.Ion)]
        public void Mismatch_Is_Wrong(ShardColor shard, ShardColor port)
        {
            Assert.AreEqual(InsertOutcome.Wrong, NewService().Validate(shard, port));
        }

        [TestCase(ShardColor.Solar)]
        [TestCase(ShardColor.Ion)]
        [TestCase(ShardColor.Pulse)]
        public void No_Port_Is_NoPenalty(ShardColor color)
        {
            Assert.AreEqual(InsertOutcome.NoPenalty, NewService().Validate(color, null));
        }
    }
}
