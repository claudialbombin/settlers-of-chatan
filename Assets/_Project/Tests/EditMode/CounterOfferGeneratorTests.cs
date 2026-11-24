using System.Collections.Generic;
using Chatan.AI;
using Chatan.Core;
using Chatan.Dev;
using Chatan.Negotiation;
using NUnit.Framework;
using UnityEngine;

namespace Chatan.Tests
{
    /// <summary>
    /// Unit tests for <see cref="CounterOfferGenerator"/>, with an evaluator without noise.
    /// The human (0) holds 2 wood, 1 brick, 3 wool, 2 grain, 0 ore; Bot Azul (1) holds
    /// 1 wood, 2 brick, 0 wool, 1 grain, 3 ore.
    /// </summary>
    public class CounterOfferGeneratorTests
    {
        private const int Human = FakeGameStateFactory.HumanId;
        private const int Bot = 1;

        private readonly List<PersonalityProfile> _profiles = new List<PersonalityProfile>();
        private GameState _state;
        private TradeEvaluator _evaluator;
        private CounterOfferGenerator _generator;

        [SetUp]
        public void SetUp()
        {
            _state = FakeGameStateFactory.Create();
            _evaluator = new TradeEvaluator(new SimpleResourceValuator());
            _generator = new CounterOfferGenerator(_evaluator);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (PersonalityProfile profile in _profiles)
            {
                Object.DestroyImmediate(profile);
            }

            _profiles.Clear();
        }

        private PersonalityProfile Personality(float greed = 0.5f, float cooperation = 0.5f)
        {
            var profile = ScriptableObject.CreateInstance<PersonalityProfile>();
            profile.greed = greed;
            profile.cooperation = cooperation;
            profile.randomness = 0f;
            _profiles.Add(profile);
            return profile;
        }

        private static TradeOffer Offer(ResourceBundle give, ResourceBundle receive) =>
            new TradeOffer { Id = 7, FromPlayerId = Human, ToPlayerId = Bot, Give = give, Receive = receive };

        [Test]
        public void AcceptableOriginal_IsReturnedAlmostUnchanged()
        {
            PersonalityProfile neutral = Personality();
            var original = Offer(ResourceBundle.Of(ResourceType.Grain, 2), ResourceBundle.Of(ResourceType.Brick, 1));
            Assume.That(_evaluator.Evaluate(original, Bot, _state, neutral, null, false).IsAccepted);

            TradeOffer counter = _generator.Generate(original, Bot, _state, neutral, null);

            Assert.IsNotNull(counter);
            Assert.LessOrEqual(CounterOfferGenerator.Distance(original, counter), 1);
        }

        [Test]
        public void GreedyBot_OneWoolForOneOre_AsksTheHumanForMore()
        {
            PersonalityProfile greedy = Personality(greed: 1f, cooperation: 0f);
            var original = Offer(ResourceBundle.Of(ResourceType.Wool, 1), ResourceBundle.Of(ResourceType.Ore, 1));

            TradeOffer counter = _generator.Generate(original, Bot, _state, greedy, null);

            Assert.IsNotNull(counter);
            Assert.Greater(counter.Give.Total, original.Give.Total, counter.ToString());
            Assert.IsTrue(_state.Players[Human].Resources.Contains(counter.Give), "the human must be able to pay it");
        }

        [Test]
        public void HumanHasNothingElseToGive_ReturnsNull()
        {
            _state.Players[Human].Resources = ResourceBundle.Of(ResourceType.Wool, 1);
            var original = Offer(ResourceBundle.Of(ResourceType.Wool, 1), ResourceBundle.Of(ResourceType.Ore, 3));

            TradeOffer counter = _generator.Generate(original, Bot, _state, Personality(greed: 1f, cooperation: 0f), null);

            Assert.IsNull(counter);
        }

        [Test]
        public void Counter_IsAcceptedByTheBot_AndMarkedCountered()
        {
            PersonalityProfile neutral = Personality();
            var original = Offer(ResourceBundle.Of(ResourceType.Wood, 1), ResourceBundle.Of(ResourceType.Brick, 1));
            Assume.That(_evaluator.Evaluate(original, Bot, _state, neutral, null, true).Type, Is.EqualTo(TradeDecisionType.Counter));

            TradeOffer counter = _generator.Generate(original, Bot, _state, neutral, null);

            Assert.IsNotNull(counter);
            Assert.AreEqual(TradeStatus.Countered, counter.Status);
            Assert.AreEqual(Human, counter.FromPlayerId, "written as if the human proposed it");
            Assert.IsTrue(_evaluator.Evaluate(counter, Bot, _state, neutral, null, false).IsAccepted);
            Assert.AreEqual(1, CounterOfferGenerator.Distance(original, counter), "one card more is enough");
        }

        [Test]
        public void Generate_DoesNotModifyTheOriginal()
        {
            var original = Offer(ResourceBundle.Of(ResourceType.Wood, 1), ResourceBundle.Of(ResourceType.Brick, 1));

            _generator.Generate(original, Bot, _state, Personality(), null);

            Assert.AreEqual(ResourceBundle.Of(ResourceType.Wood, 1), original.Give);
            Assert.AreEqual(ResourceBundle.Of(ResourceType.Brick, 1), original.Receive);
            Assert.AreEqual(TradeStatus.Proposed, original.Status);
        }

        [Test]
        public void Counter_NeverAsksForWhatTheHumanGives()
        {
            var original = Offer(ResourceBundle.Of(ResourceType.Wood, 1), ResourceBundle.Of(ResourceType.Brick, 1));

            TradeOffer counter = _generator.Generate(original, Bot, _state, Personality(greed: 1f, cooperation: 0f), null);

            if (counter != null)
            {
                foreach (ResourceType type in ResourceBundle.AllTypes)
                {
                    Assert.IsFalse(counter.Give[type] > 0 && counter.Receive[type] > 0, counter.ToString());
                }
            }
        }
    }
}
