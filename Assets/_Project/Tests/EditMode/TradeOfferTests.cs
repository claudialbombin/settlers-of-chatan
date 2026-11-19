using Chatan.Core;
using Chatan.Negotiation;
using NUnit.Framework;

namespace Chatan.Tests
{
    /// <summary>Unit tests for <see cref="TradeOffer"/>.</summary>
    public class TradeOfferTests
    {
        private static TradeOffer TwoGrainForOneOre() => new TradeOffer
        {
            Id = 3,
            FromPlayerId = 0,
            ToPlayerId = 2,
            Give = ResourceBundle.Of(ResourceType.Grain, 2),
            Receive = ResourceBundle.Of(ResourceType.Ore, 1)
        };

        [Test]
        public void NewOffer_StartsAsProposed()
        {
            Assert.AreEqual(TradeStatus.Proposed, new TradeOffer().Status);
        }

        [Test]
        public void Reversed_SwapsPlayersAndSides()
        {
            TradeOffer offer = TwoGrainForOneOre();

            TradeOffer reversed = offer.Reversed();

            Assert.AreEqual(offer.Id, reversed.Id);
            Assert.AreEqual(2, reversed.FromPlayerId);
            Assert.AreEqual(0, reversed.ToPlayerId);
            Assert.AreEqual(offer.Receive, reversed.Give);
            Assert.AreEqual(offer.Give, reversed.Receive);
        }

        [Test]
        public void Reversed_DoesNotModifyTheOriginal()
        {
            TradeOffer offer = TwoGrainForOneOre();

            offer.Reversed();

            Assert.AreEqual(0, offer.FromPlayerId);
            Assert.AreEqual(2, offer.Give.Grain);
        }

        [Test]
        public void IsValidShape_AcceptsAWellFormedOffer()
        {
            Assert.IsTrue(TwoGrainForOneOre().IsValidShape);
        }

        [Test]
        public void IsValidShape_RejectsAnEmptySide()
        {
            TradeOffer gift = TwoGrainForOneOre();
            gift.Receive = ResourceBundle.Empty;

            Assert.IsFalse(gift.IsValidShape);
        }

        [Test]
        public void IsValidShape_RejectsTradingWithYourself()
        {
            TradeOffer offer = TwoGrainForOneOre();
            offer.ToPlayerId = offer.FromPlayerId;

            Assert.IsFalse(offer.IsValidShape);
        }

        [Test]
        public void ToString_DescribesTheDeal()
        {
            Assert.AreEqual("#3 J0→J2: da [2 trigo] por [1 mineral]", TwoGrainForOneOre().ToString());
        }
    }
}
