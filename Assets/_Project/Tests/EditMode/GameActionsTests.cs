using Chatan.Core;
using Chatan.Rules;
using NUnit.Framework;

namespace Chatan.Tests
{
    /// <summary>Checks the placeholder actions keep what they are given.</summary>
    public class GameActionsTests
    {
        [Test]
        public void SimpleActions_KeepThePlayer()
        {
            Assert.AreEqual(2, new RollDiceAction(2).PlayerId);
            Assert.AreEqual(2, new EndTurnAction(2).PlayerId);
            Assert.AreEqual(2, new BuyDevCardAction(2).PlayerId);
        }

        [Test]
        public void BuildActions_KeepTheTarget()
        {
            Assert.AreEqual(12, new BuildRoadAction(0, 12).EdgeId);
            Assert.AreEqual(5, new BuildSettlementAction(0, 5).VertexId);
            Assert.AreEqual(7, new BuildCityAction(0, 7).VertexId);
        }

        [Test]
        public void MoveRobber_KeepsTileAndVictim()
        {
            var action = new MoveRobberAction(0, 3, MoveRobberAction.NoVictim);

            Assert.AreEqual(3, action.TileId);
            Assert.AreEqual(-1, action.VictimPlayerId);
        }

        [Test]
        public void Discard_KeepsTheCards()
        {
            var cards = new ResourceBundle(wood: 2, ore: 2);

            Assert.AreEqual(cards, new DiscardAction(1, cards).Cards);
        }

        [Test]
        public void ToString_IsReadableInTheLog()
        {
            Assert.AreEqual("BuildRoad(P0, edge 12)", new BuildRoadAction(0, 12).ToString());
            Assert.AreEqual("MoveRobber(P0, tile 3, victim 2)", new MoveRobberAction(0, 3, 2).ToString());
            Assert.AreEqual("Discard(P1, 2 madera)", new DiscardAction(1, new ResourceBundle(wood: 2)).ToString());
        }
    }
}
