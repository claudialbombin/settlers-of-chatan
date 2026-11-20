using Chatan.Core;
using Chatan.Dev;
using Chatan.UI;
using NUnit.Framework;

namespace Chatan.Tests
{
    /// <summary>Unit tests for <see cref="HudText"/>: the texts and button rules of the HUD.</summary>
    public class HudTextTests
    {
        private const int Human = FakeGameStateFactory.HumanId;

        [Test]
        public void Points_WithoutHiddenPoints_ShowsOnlyTheTotal()
        {
            Assert.AreEqual("4 PV", HudText.Points(4, 0));
        }

        [Test]
        public void Points_WithHiddenPoints_AddsThemAndSaysHowMany()
        {
            Assert.AreEqual("3 PV (1 oculto)", HudText.Points(2, 1));
            Assert.AreEqual("5 PV (2 ocultos)", HudText.Points(3, 2));
        }

        [Test]
        public void Dice_BeforeTheFirstRoll_ShowDashesAndNoTotal()
        {
            Assert.AreEqual(HudText.NoDie, HudText.Die(0));
            Assert.AreEqual(string.Empty, HudText.DiceTotal(0, 0));
            Assert.IsFalse(HudText.IsRobberRoll(0, 0));
        }

        [Test]
        public void Dice_AfterARoll_ShowValuesAndSum()
        {
            Assert.AreEqual("4", HudText.Die(4));
            Assert.AreEqual("9", HudText.DiceTotal(4, 5));
        }

        [Test]
        public void Seven_IsTheOnlyRobberRoll()
        {
            Assert.IsTrue(HudText.IsRobberRoll(3, 4));
            Assert.IsTrue(HudText.IsRobberRoll(6, 1));
            Assert.IsFalse(HudText.IsRobberRoll(3, 3));
            Assert.IsFalse(HudText.IsRobberRoll(6, 2));
        }

        [Test]
        public void Turn_SaysTuTurnoForTheHumanAndTheNameForABot()
        {
            GameState state = FakeGameStateFactory.Create();

            Assert.AreEqual("Tu turno", HudText.Turn(state.Players[0]));
            Assert.AreEqual("Turno de Bot Azul", HudText.Turn(state.Players[1]));
        }

        [Test]
        public void EveryPhase_HasASpanishText()
        {
            foreach (GamePhase phase in System.Enum.GetValues(typeof(GamePhase)))
            {
                Assert.AreNotEqual(phase.ToString(), HudText.Phase(phase), $"No text for {phase}.");
            }
        }

        [Test]
        public void Buttons_OnTheHumanTurn_FollowThePhase()
        {
            GameState state = FakeGameStateFactory.Create();

            state.Phase = GamePhase.Roll;
            Assert.IsTrue(HudText.CanRoll(state, Human));
            Assert.IsFalse(HudText.CanEndTurn(state, Human));

            state.Phase = GamePhase.Main;
            Assert.IsFalse(HudText.CanRoll(state, Human));
            Assert.IsTrue(HudText.CanEndTurn(state, Human));

            state.Phase = GamePhase.MoveRobber;
            Assert.IsFalse(HudText.CanRoll(state, Human));
            Assert.IsFalse(HudText.CanEndTurn(state, Human));
        }

        [Test]
        public void Buttons_OnABotTurn_AreDisabled()
        {
            GameState state = FakeGameStateFactory.Create();
            state.CurrentPlayerIndex = 2;

            state.Phase = GamePhase.Roll;
            Assert.IsFalse(HudText.CanRoll(state, Human));

            state.Phase = GamePhase.Main;
            Assert.IsFalse(HudText.CanEndTurn(state, Human));
        }

        [Test]
        public void Buttons_WithoutAValidState_AreDisabled()
        {
            Assert.IsFalse(HudText.CanRoll(null, Human));
            Assert.IsFalse(HudText.CanRoll(new GameState { Phase = GamePhase.Roll }, Human));
        }
    }
}
