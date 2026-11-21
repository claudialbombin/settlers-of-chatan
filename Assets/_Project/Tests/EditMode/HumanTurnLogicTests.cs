using System;
using Chatan.Board;
using Chatan.Core;
using Chatan.Dev;
using Chatan.Players;
using Chatan.Rules;
using Chatan.UI;
using NUnit.Framework;

namespace Chatan.Tests
{
    /// <summary>Unit tests for <see cref="HumanTurnLogic"/>.</summary>
    public class HumanTurnLogicTests
    {
        private GameState _state;
        private FakeRulesQuery _rules;

        private PlayerState Me => _state.Players[FakeGameStateFactory.HumanId];

        [SetUp]
        public void SetUp()
        {
            _state = FakeGameStateFactory.Create();
            _state.Phase = GamePhase.Main;
            _rules = new FakeRulesQuery(vertexCount: 5, edgeCount: 4, tileCount: 3);
        }

        [Test]
        public void KindFor_MapsEachModeToItsPiece()
        {
            Assert.AreEqual(BoardTargetKind.Edge, HumanTurnLogic.KindFor(PlacementMode.Road));
            Assert.AreEqual(BoardTargetKind.Vertex, HumanTurnLogic.KindFor(PlacementMode.Settlement));
            Assert.AreEqual(BoardTargetKind.Vertex, HumanTurnLogic.KindFor(PlacementMode.City));
            Assert.AreEqual(BoardTargetKind.Tile, HumanTurnLogic.KindFor(PlacementMode.Robber));
        }

        [Test]
        public void ValidIds_AsksTheRightRule()
        {
            _rules.AddSettlement(0, 3);

            CollectionAssert.AreEquivalent(new[] { 0, 1, 2, 3 }, HumanTurnLogic.ValidIds(PlacementMode.Road, _rules, _state, 0));
            CollectionAssert.AreEquivalent(new[] { 0, 1, 2, 4 }, HumanTurnLogic.ValidIds(PlacementMode.Settlement, _rules, _state, 0));
            CollectionAssert.AreEquivalent(new[] { 3 }, HumanTurnLogic.ValidIds(PlacementMode.City, _rules, _state, 0));
            CollectionAssert.AreEquivalent(new[] { 1, 2 }, HumanTurnLogic.ValidIds(PlacementMode.Robber, _rules, _state, 0));
        }

        [Test]
        public void ValidIds_EmptyWithoutModeOrRules()
        {
            CollectionAssert.IsEmpty(HumanTurnLogic.ValidIds(PlacementMode.None, _rules, _state, 0));
            CollectionAssert.IsEmpty(HumanTurnLogic.ValidIds(PlacementMode.Road, null, _state, 0));
        }

        [Test]
        public void BuildActionFor_CreatesTheMatchingAction()
        {
            Assert.IsInstanceOf<BuildRoadAction>(HumanTurnLogic.BuildActionFor(PlacementMode.Road, 0, 2));
            Assert.IsInstanceOf<BuildSettlementAction>(HumanTurnLogic.BuildActionFor(PlacementMode.Settlement, 0, 2));
            var city = (BuildCityAction)HumanTurnLogic.BuildActionFor(PlacementMode.City, 0, 4);
            Assert.AreEqual(4, city.VertexId);
        }

        [Test]
        public void BuildActionFor_RejectsModesThatDoNotBuild()
        {
            Assert.Throws<ArgumentException>(() => HumanTurnLogic.BuildActionFor(PlacementMode.None, 0, 1));
            Assert.Throws<ArgumentException>(() => HumanTurnLogic.BuildActionFor(PlacementMode.Robber, 0, 1));
        }

        [Test]
        public void ForcedMode_RobberAfterASeven()
        {
            _state.Phase = GamePhase.MoveRobber;

            Assert.AreEqual(PlacementMode.Robber, HumanTurnLogic.ForcedMode(_state, _rules, Me));
            Assert.IsFalse(HumanTurnLogic.CanCancel(_state, _rules, Me));
        }

        [Test]
        public void ForcedMode_SetupAlternatesSettlementAndRoad()
        {
            _rules.FreePlacement = true;
            Me.SettlementsLeft = BuildChoices.MaxSettlements;
            Me.RoadsLeft = HumanTurnLogic.MaxRoads;
            Assert.AreEqual(PlacementMode.Settlement, HumanTurnLogic.ForcedMode(_state, _rules, Me));

            Me.SettlementsLeft--;
            Assert.AreEqual(PlacementMode.Road, HumanTurnLogic.ForcedMode(_state, _rules, Me));

            Me.RoadsLeft--;
            Assert.AreEqual(PlacementMode.Settlement, HumanTurnLogic.ForcedMode(_state, _rules, Me));
        }

        [Test]
        public void ForcedMode_NoneInTheMainPhase()
        {
            Assert.AreEqual(PlacementMode.None, HumanTurnLogic.ForcedMode(_state, _rules, Me));
            Assert.IsTrue(HumanTurnLogic.CanCancel(_state, _rules, Me));
        }

        [Test]
        public void AutomaticVictim_OnlyAsksWithSeveralPlayers()
        {
            Assert.AreEqual(MoveRobberAction.NoVictim, HumanTurnLogic.AutomaticVictim(new int[0]));
            Assert.AreEqual(2, HumanTurnLogic.AutomaticVictim(new[] { 2 }));
            Assert.IsNull(HumanTurnLogic.AutomaticVictim(new[] { 1, 2 }));
        }
    }
}
