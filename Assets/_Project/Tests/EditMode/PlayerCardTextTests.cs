using System.Collections.Generic;
using Chatan.AI;
using Chatan.Core;
using Chatan.UI;
using NUnit.Framework;
using UnityEngine;

namespace Chatan.Tests
{
    /// <summary>Unit tests for <see cref="PlayerCardText"/>: what a bot card may and may not show.</summary>
    public class PlayerCardTextTests
    {
        private const float Tolerance = 1e-4f;

        [Test]
        public void Points_ShowOnlyVisiblePoints()
        {
            var bot = new PlayerState { VisiblePoints = 3, HiddenPoints = 2 };

            Assert.AreEqual("3 PV", PlayerCardText.Points(bot));
        }

        [Test]
        public void Cards_Hidden_ShowOnlyTheTotal()
        {
            var hand = new ResourceBundle(wood: 1, brick: 2, ore: 4);

            Assert.AreEqual("7 cartas", PlayerCardText.Cards(hand, revealHand: false));
            Assert.AreEqual("1 carta", PlayerCardText.Cards(new ResourceBundle(wool: 1), revealHand: false));
        }

        [Test]
        public void Cards_Revealed_ShowTheExactHand()
        {
            var hand = new ResourceBundle(wood: 1, ore: 4);

            Assert.AreEqual(hand.ToString(), PlayerCardText.Cards(hand, revealHand: true));
        }

        [Test]
        public void TooManyCards_StartsAboveSeven()
        {
            Assert.IsFalse(PlayerCardText.HasTooManyCards(new ResourceBundle(grain: 7)));
            Assert.IsTrue(PlayerCardText.HasTooManyCards(new ResourceBundle(grain: 7, ore: 1)));
        }

        [Test]
        public void DevCardsAndKnights_CountWithSingularAndPlural()
        {
            var bot = new PlayerState { DevCards = new List<DevCardType> { DevCardType.Knight }, KnightsPlayed = 1 };
            Assert.AreEqual("1 desarrollo", PlayerCardText.DevCards(bot));
            Assert.AreEqual("1 caballero", PlayerCardText.Knights(bot));

            bot.KnightsPlayed = 2;
            bot.DevCards = null;
            Assert.AreEqual("0 desarrollo", PlayerCardText.DevCards(bot));
            Assert.AreEqual("2 caballeros", PlayerCardText.Knights(bot));
        }

        [Test]
        public void Personality_IsSecretUnlessRevealed()
        {
            var profile = ScriptableObject.CreateInstance<PersonalityProfile>();
            profile.displayName = "El Prudente";

            Assert.AreEqual(PlayerCardText.UnknownPersonality, PlayerCardText.Personality(profile, reveal: false));
            Assert.AreEqual("El Prudente", PlayerCardText.Personality(profile, reveal: true));
            Assert.AreEqual(PlayerCardText.UnknownPersonality, PlayerCardText.Personality(null, reveal: true));

            Object.DestroyImmediate(profile);
        }

        [Test]
        public void AffinityFill_MapsMinusOneToOneOntoZeroToOne()
        {
            Assert.AreEqual(0f, PlayerCardText.AffinityFill(-1f), Tolerance);
            Assert.AreEqual(0.5f, PlayerCardText.AffinityFill(0f), Tolerance);
            Assert.AreEqual(1f, PlayerCardText.AffinityFill(1f), Tolerance);
            Assert.AreEqual(1f, PlayerCardText.AffinityFill(3f), Tolerance);
            Assert.AreEqual(0f, PlayerCardText.AffinityFill(-3f), Tolerance);
        }

        [Test]
        public void DebugSettings_Null_RevealsNothing()
        {
            Assert.IsFalse(DebugSettings.RevealPersonalities(null));
            Assert.IsFalse(DebugSettings.RevealBotHands(null));
        }
    }
}
