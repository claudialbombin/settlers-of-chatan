using Chatan.Core;
using NUnit.Framework;

namespace Chatan.Tests
{
    /// <summary>Unit tests for <see cref="GameState"/> and <see cref="PlayerState"/>.</summary>
    public class GameStateTests
    {
        private static GameState TwoPlayerState()
        {
            var state = new GameState { Phase = GamePhase.Main, CurrentPlayerIndex = 1, TurnNumber = 7 };
            state.Players.Add(new PlayerState { Id = 0, Name = "Human", IsHuman = true });
            state.Players.Add(new PlayerState { Id = 1, Name = "Bot", Resources = ResourceBundle.Of(ResourceType.Wood, 2) });
            state.Players[1].DevCards.Add(DevCardType.Knight);
            return state;
        }

        [Test]
        public void CurrentPlayer_ReturnsThePlayerAtCurrentPlayerIndex()
        {
            Assert.AreEqual("Bot", TwoPlayerState().CurrentPlayer.Name);
        }

        [Test]
        public void TotalPoints_AddsVisibleAndHiddenPoints()
        {
            var player = new PlayerState { VisiblePoints = 7, HiddenPoints = 2 };

            Assert.AreEqual(9, player.TotalPoints);
        }

        [Test]
        public void Clone_CopiesScalarFields()
        {
            GameState clone = TwoPlayerState().Clone();

            Assert.AreEqual(GamePhase.Main, clone.Phase);
            Assert.AreEqual(1, clone.CurrentPlayerIndex);
            Assert.AreEqual(7, clone.TurnNumber);
            Assert.AreEqual(2, clone.Players.Count);
        }

        [Test]
        public void Clone_IsDeep_ChangingTheCopyLeavesTheOriginalIntact()
        {
            GameState original = TwoPlayerState();
            GameState clone = original.Clone();

            clone.Players[1].Resources.Wood = 0;
            clone.Players[1].DevCards.Clear();
            clone.Players.RemoveAt(0);

            Assert.AreEqual(2, original.Players.Count);
            Assert.AreEqual(2, original.Players[1].Resources.Wood);
            Assert.AreEqual(1, original.Players[1].DevCards.Count);
        }
    }
}
