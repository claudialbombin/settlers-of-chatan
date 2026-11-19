using Chatan.Core;
using NUnit.Framework;

namespace Chatan.Tests
{
    /// <summary>Unit tests for <see cref="ResourceBundle"/>.</summary>
    public class ResourceBundleTests
    {
        [Test]
        public void Indexer_ReadsAndWritesTheMatchingField()
        {
            var bundle = new ResourceBundle();

            bundle[ResourceType.Ore] = 4;

            Assert.AreEqual(4, bundle.Ore);
            Assert.AreEqual(4, bundle[ResourceType.Ore]);
            Assert.AreEqual(0, bundle.Wood);
        }

        [Test]
        public void Total_SumsAllFiveAmounts()
        {
            var bundle = new ResourceBundle(1, 2, 3, 4, 5);

            Assert.AreEqual(15, bundle.Total);
        }

        [Test]
        public void IsEmpty_IsTrueOnlyWhenEveryAmountIsZero()
        {
            Assert.IsTrue(ResourceBundle.Empty.IsEmpty);
            Assert.IsFalse(ResourceBundle.Of(ResourceType.Wool, 1).IsEmpty);
            Assert.IsFalse(new ResourceBundle(wood: 1, ore: -1).IsEmpty, "Opposite amounts must not cancel out.");
        }

        [Test]
        public void Contains_IsTrueWhenEveryAmountIsEnough()
        {
            var hand = new ResourceBundle(grain: 2, ore: 3, wool: 1);

            Assert.IsTrue(hand.Contains(ResourceBundle.CityCost));
            Assert.IsTrue(hand.Contains(ResourceBundle.Empty));
        }

        [Test]
        public void Contains_IsFalseWhenAnyAmountIsMissing()
        {
            var hand = new ResourceBundle(grain: 2, ore: 2);

            Assert.IsFalse(hand.Contains(ResourceBundle.CityCost));
        }

        [Test]
        public void Operators_AddAndSubtractPerResource()
        {
            var a = new ResourceBundle(wood: 2, grain: 1);
            var b = new ResourceBundle(wood: 1, ore: 3);

            Assert.AreEqual(new ResourceBundle(wood: 3, grain: 1, ore: 3), a + b);
            Assert.AreEqual(new ResourceBundle(wood: 1, grain: 1, ore: -3), a - b);
        }

        [Test]
        public void Of_CreatesABundleWithASingleResource()
        {
            var bundle = ResourceBundle.Of(ResourceType.Grain, 2);

            Assert.AreEqual(2, bundle.Grain);
            Assert.AreEqual(2, bundle.Total);
        }

        [Test]
        public void Costs_MatchTheBoardGameRules()
        {
            Assert.AreEqual(new ResourceBundle(wood: 1, brick: 1), ResourceBundle.RoadCost);
            Assert.AreEqual(new ResourceBundle(wood: 1, brick: 1, wool: 1, grain: 1), ResourceBundle.SettlementCost);
            Assert.AreEqual(new ResourceBundle(grain: 2, ore: 3), ResourceBundle.CityCost);
            Assert.AreEqual(new ResourceBundle(wool: 1, grain: 1, ore: 1), ResourceBundle.DevCardCost);
        }

        [Test]
        public void CopyingABundle_DoesNotShareState()
        {
            var original = ResourceBundle.Of(ResourceType.Brick, 1);
            var copy = original;

            copy.Brick = 5;

            Assert.AreEqual(1, original.Brick);
        }

        [Test]
        public void ToString_ListsOnlyNonZeroAmountsInSpanish()
        {
            var bundle = new ResourceBundle(grain: 2, ore: 1);

            Assert.AreEqual("2 trigo, 1 mineral", bundle.ToString());
            Assert.AreEqual("nada", ResourceBundle.Empty.ToString());
        }
    }
}
