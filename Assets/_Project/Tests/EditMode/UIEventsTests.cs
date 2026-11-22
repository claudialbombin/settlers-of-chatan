using Chatan.UI;
using NUnit.Framework;

namespace Chatan.Tests
{
    /// <summary>Unit tests for <see cref="UIEvents"/>.</summary>
    public class UIEventsTests
    {
        [SetUp]
        public void SetUp() => UIEvents.ClearAll();

        [TearDown]
        public void TearDown() => UIEvents.ClearAll();

        [Test]
        public void RequestRollDice_NotifiesSubscribers()
        {
            int calls = 0;
            UIEvents.RollDiceRequested += () => calls++;

            UIEvents.RequestRollDice();

            Assert.AreEqual(1, calls);
        }

        [Test]
        public void RequestEndTurn_NotifiesSubscribers()
        {
            int calls = 0;
            UIEvents.EndTurnRequested += () => calls++;

            UIEvents.RequestEndTurn();

            Assert.AreEqual(1, calls);
        }

        [Test]
        public void SetPlacementMode_PassesTheMode()
        {
            PlacementMode received = PlacementMode.None;
            UIEvents.PlacementModeChanged += mode => received = mode;

            UIEvents.SetPlacementMode(PlacementMode.City);

            Assert.AreEqual(PlacementMode.City, received);
        }

        [Test]
        public void RequestBuyDevCard_NotifiesSubscribers()
        {
            int calls = 0;
            UIEvents.BuyDevCardRequested += () => calls++;

            UIEvents.RequestBuyDevCard();

            Assert.AreEqual(1, calls);
        }

        [Test]
        public void Requests_WithoutSubscribers_DoNotThrow()
        {
            Assert.DoesNotThrow(UIEvents.RequestRollDice);
            Assert.DoesNotThrow(UIEvents.RequestEndTurn);
            Assert.DoesNotThrow(UIEvents.RequestBuyDevCard);
            Assert.DoesNotThrow(() => UIEvents.SetPlacementMode(PlacementMode.Road));
        }

        [Test]
        public void ClearAll_RemovesSubscribers()
        {
            int calls = 0;
            UIEvents.RollDiceRequested += () => calls++;
            UIEvents.EndTurnRequested += () => calls++;
            UIEvents.PlacementModeChanged += _ => calls++;
            UIEvents.BuyDevCardRequested += () => calls++;

            UIEvents.ClearAll();
            UIEvents.RequestRollDice();
            UIEvents.RequestEndTurn();
            UIEvents.SetPlacementMode(PlacementMode.Road);
            UIEvents.RequestBuyDevCard();

            Assert.AreEqual(0, calls);
        }
    }
}
