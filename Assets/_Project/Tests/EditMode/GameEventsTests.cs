using Chatan.Core;
using NUnit.Framework;

namespace Chatan.Tests
{
    /// <summary>Unit tests for <see cref="GameEvents"/>.</summary>
    public class GameEventsTests
    {
        // The events are static, so every test starts and ends with a clean board.
        [SetUp]
        public void SetUp() => GameEvents.ClearAll();

        [TearDown]
        public void TearDown() => GameEvents.ClearAll();

        [Test]
        public void RaiseDiceRolled_NotifiesSubscribersWithBothDice()
        {
            int receivedA = 0, receivedB = 0;
            GameEvents.DiceRolled += (a, b) => { receivedA = a; receivedB = b; };

            GameEvents.RaiseDiceRolled(3, 4);

            Assert.AreEqual(3, receivedA);
            Assert.AreEqual(4, receivedB);
        }

        [Test]
        public void RaiseStateChanged_PassesTheSameStateInstance()
        {
            var state = new GameState();
            GameState received = null;
            GameEvents.StateChanged += s => received = s;

            GameEvents.RaiseStateChanged(state);

            Assert.AreSame(state, received);
        }

        [Test]
        public void Raise_WithoutSubscribers_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                GameEvents.RaiseStateChanged(new GameState());
                GameEvents.RaiseDiceRolled(1, 1);
                GameEvents.RaiseTurnStarted(new PlayerState());
                GameEvents.RaisePlayerRobbed(0, 1);
            });
        }

        [Test]
        public void ClearAll_RemovesEverySubscriber()
        {
            int calls = 0;
            GameEvents.StateChanged += _ => calls++;
            GameEvents.DiceRolled += (_, _) => calls++;
            GameEvents.TurnStarted += _ => calls++;
            GameEvents.PlayerRobbed += (_, _) => calls++;

            GameEvents.ClearAll();
            GameEvents.RaiseStateChanged(new GameState());
            GameEvents.RaiseDiceRolled(1, 2);
            GameEvents.RaiseTurnStarted(new PlayerState());
            GameEvents.RaisePlayerRobbed(0, 1);

            Assert.AreEqual(0, calls);
        }
    }
}
