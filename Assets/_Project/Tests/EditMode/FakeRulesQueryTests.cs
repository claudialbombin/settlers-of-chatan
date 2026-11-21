using System.Linq;
using Chatan.Dev;
using NUnit.Framework;

namespace Chatan.Tests
{
    /// <summary>Unit tests for the sandbox rules, <see cref="FakeRulesQuery"/>.</summary>
    public class FakeRulesQueryTests
    {
        private FakeRulesQuery _rules;

        [SetUp]
        public void SetUp() => _rules = new FakeRulesQuery(vertexCount: 4, edgeCount: 3, tileCount: 3);

        [Test]
        public void EveryFreeSpotIsValid()
        {
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3 }, _rules.ValidSettlementVertices(null, 0).ToArray());
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, _rules.ValidRoadEdges(null, 0).ToArray());
        }

        [Test]
        public void BuiltSpots_AreNoLongerOffered()
        {
            _rules.AddRoad(1);
            _rules.AddSettlement(0, 2);

            CollectionAssert.AreEqual(new[] { 0, 2 }, _rules.ValidRoadEdges(null, 0).ToArray());
            CollectionAssert.AreEqual(new[] { 0, 1, 3 }, _rules.ValidSettlementVertices(null, 0).ToArray());
        }

        [Test]
        public void Cities_OnlyOnYourOwnSettlements()
        {
            _rules.AddSettlement(0, 1);
            _rules.AddSettlement(2, 3);

            CollectionAssert.AreEqual(new[] { 1 }, _rules.ValidCityVertices(null, 0).ToArray());

            _rules.UpgradeToCity(1);
            CollectionAssert.IsEmpty(_rules.ValidCityVertices(null, 0).ToArray());
        }

        [Test]
        public void Robber_CannotStayOnItsTile()
        {
            _rules.MoveRobber(1);

            CollectionAssert.AreEqual(new[] { 0, 2 }, _rules.ValidRobberTiles(null).ToArray());
        }

        [Test]
        public void RobbablePlayers_NeverIncludeTheThief()
        {
            CollectionAssert.AreEqual(new[] { 1, 2 }, _rules.RobbablePlayersAt(null, 0, 0).ToArray());
            CollectionAssert.AreEqual(new[] { 2 }, _rules.RobbablePlayersAt(null, 0, 1).ToArray());
        }

        [Test]
        public void FreePlacement_IsOffUnlessTurnedOn()
        {
            Assert.IsFalse(_rules.IsFreePlacement(null));

            _rules.FreePlacement = true;
            Assert.IsTrue(_rules.IsFreePlacement(null));
        }
    }
}
