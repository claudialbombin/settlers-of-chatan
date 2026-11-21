using Chatan.Core;
using Chatan.Dev;
using Chatan.UI;
using NUnit.Framework;

namespace Chatan.Tests
{
    /// <summary>Unit tests for <see cref="BuildChoices"/>.</summary>
    public class BuildChoicesTests
    {
        private GameState _state;

        private PlayerState Me => _state.Players[FakeGameStateFactory.HumanId];

        [SetUp]
        public void SetUp()
        {
            _state = FakeGameStateFactory.Create();
            _state.Phase = GamePhase.Main;
        }

        [Test]
        public void Cost_MatchesTheBuildingCosts()
        {
            Assert.AreEqual(ResourceBundle.RoadCost, BuildChoices.Cost(BuildKind.Road));
            Assert.AreEqual(ResourceBundle.SettlementCost, BuildChoices.Cost(BuildKind.Settlement));
            Assert.AreEqual(ResourceBundle.CityCost, BuildChoices.Cost(BuildKind.City));
            Assert.AreEqual(ResourceBundle.DevCardCost, BuildChoices.Cost(BuildKind.DevCard));
        }

        [Test]
        public void CostText_ListsTheResources()
        {
            Assert.AreEqual("Coste: 1 madera, 1 ladrillo", BuildChoices.CostText(BuildKind.Road));
            Assert.AreEqual("Coste: 2 trigo, 3 mineral", BuildChoices.CostText(BuildKind.City));
        }

        [Test]
        public void CanUse_WhenAffordableInMainPhase()
        {
            // Factory hand: 2 wood, 1 brick, 3 wool, 2 grain, 0 ore.
            Assert.IsTrue(BuildChoices.CanUse(BuildKind.Road, _state, FakeGameStateFactory.HumanId));
            Assert.IsTrue(BuildChoices.CanUse(BuildKind.Settlement, _state, FakeGameStateFactory.HumanId));
        }

        [Test]
        public void CanUse_FalseWhenItCannotBePaid()
        {
            Assert.IsFalse(BuildChoices.CanUse(BuildKind.City, _state, FakeGameStateFactory.HumanId));
            Assert.IsFalse(BuildChoices.CanUse(BuildKind.DevCard, _state, FakeGameStateFactory.HumanId));
        }

        [Test]
        public void CanUse_FalseOutsideTheMainPhase()
        {
            _state.Phase = GamePhase.Roll;

            Assert.IsFalse(BuildChoices.CanUse(BuildKind.Road, _state, FakeGameStateFactory.HumanId));
        }

        [Test]
        public void CanUse_FalseOnSomebodyElsesTurn()
        {
            _state.CurrentPlayerIndex = 1;

            Assert.IsFalse(BuildChoices.CanUse(BuildKind.Road, _state, FakeGameStateFactory.HumanId));
        }

        [Test]
        public void CanUse_FalseWithoutPiecesLeft()
        {
            Me.RoadsLeft = 0;

            Assert.IsFalse(BuildChoices.CanUse(BuildKind.Road, _state, FakeGameStateFactory.HumanId));
        }

        [Test]
        public void City_NeedsASettlementOnTheBoard()
        {
            Me.Resources = new ResourceBundle(grain: 2, ore: 3);
            Me.SettlementsLeft = BuildChoices.MaxSettlements;

            Assert.IsFalse(BuildChoices.CanUse(BuildKind.City, _state, FakeGameStateFactory.HumanId));

            Me.SettlementsLeft = BuildChoices.MaxSettlements - 1;
            Assert.IsTrue(BuildChoices.CanUse(BuildKind.City, _state, FakeGameStateFactory.HumanId));
        }

        [Test]
        public void CanUse_FalseWithoutState()
        {
            Assert.IsFalse(BuildChoices.CanUse(BuildKind.Road, null, 0));
            Assert.IsFalse(BuildChoices.CanUse(BuildKind.Road, _state, 9));
        }
    }
}
