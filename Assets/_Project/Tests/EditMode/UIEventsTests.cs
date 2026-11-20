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
        public void Requests_WithoutSubscribers_DoNotThrow()
        {
            Assert.DoesNotThrow(UIEvents.RequestRollDice);
            Assert.DoesNotThrow(UIEvents.RequestEndTurn);
        }

        [Test]
        public void ClearAll_RemovesSubscribers()
        {
            int calls = 0;
            UIEvents.RollDiceRequested += () => calls++;
            UIEvents.EndTurnRequested += () => calls++;

            UIEvents.ClearAll();
            UIEvents.RequestRollDice();
            UIEvents.RequestEndTurn();

            Assert.AreEqual(0, calls);
        }
    }
}
