using System.Linq;
using Chatan.Core;
using Chatan.Dev;
using NUnit.Framework;

namespace Chatan.Tests
{
    /// <summary>Unit tests for <see cref="FakeGameStateFactory"/>.</summary>
    public class FakeGameStateFactoryTests
    {
        [Test]
        public void Create_HasOneHumanAndThreeBots()
        {
            GameState state = FakeGameStateFactory.Create();

            Assert.AreEqual(4, state.Players.Count);
            Assert.AreEqual(1, state.Players.Count(p => p.IsHuman));
            Assert.AreEqual(FakeGameStateFactory.HumanId, state.Players.Single(p => p.IsHuman).Id);
            CollectionAssert.AllItemsAreUnique(state.Players.Select(p => p.Id));
        }

        [Test]
        public void Create_StartsOnTheHumanTurnInTheRollPhase()
        {
            GameState state = FakeGameStateFactory.Create();

            Assert.IsTrue(state.CurrentPlayer.IsHuman);
            Assert.AreEqual(GamePhase.Roll, state.Phase);
            Assert.AreEqual(0, state.LastDiceA);
        }

        [Test]
        public void Create_HumanHasTheDocumentedHandAndPoints()
        {
            PlayerState human = FakeGameStateFactory.Create().Players[0];

            Assert.AreEqual(new ResourceBundle(wood: 2, brick: 1, wool: 3, grain: 2, ore: 0), human.Resources);
            Assert.AreEqual(2, human.VisiblePoints);
            Assert.AreEqual(1, human.HiddenPoints);
        }

        [Test]
        public void Create_ReturnsIndependentStates()
        {
            GameState a = FakeGameStateFactory.Create();
            GameState b = FakeGameStateFactory.Create();

            a.Players[0].Resources.Grain += 5;

            Assert.AreEqual(2, b.Players[0].Resources.Grain);
        }
    }
}
