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
    /// Unit tests for <see cref="TradeEvaluator"/>. The human (0) proposes to the bots of
    /// the fake state: Bot Azul (1) holds 1 wood, 2 brick, 0 wool, 1 grain, 3 ore and
    /// Bot Verde (2) holds 0 wood, 0 brick, 2 wool, 4 grain, 1 ore.
    /// </summary>
    public class TradeEvaluatorTests
    {
        private const int Human = FakeGameStateFactory.HumanId;

        private readonly List<PersonalityProfile> _profiles = new List<PersonalityProfile>();
        private GameState _state;
        private TradeEvaluator _evaluator;

        [SetUp]
        public void SetUp()
        {
            _state = FakeGameStateFactory.Create();
            _evaluator = new TradeEvaluator(new SimpleResourceValuator());
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

        private PersonalityProfile Personality(float greed = 0.5f, float cooperation = 0.5f, float aggression = 0.5f, float grudge = 0.3f, float randomness = 0f)
        {
            var profile = ScriptableObject.CreateInstance<PersonalityProfile>();
            profile.greed = greed;
            profile.cooperation = cooperation;
            profile.aggression = aggression;
            profile.grudge = grudge;
            profile.randomness = randomness;
            _profiles.Add(profile);
            return profile;
        }

        private static TradeOffer Offer(int to, ResourceBundle give, ResourceBundle receive) =>
            new TradeOffer { FromPlayerId = Human, ToPlayerId = to, Give = give, Receive = receive };

        // 1 wood (worth 1.0 to Bot Azul) for 1 brick (1.0): fair, but a neutral bot wants a little more.
        private static TradeOffer FairOffer() =>
            Offer(1, ResourceBundle.Of(ResourceType.Wood, 1), ResourceBundle.Of(ResourceType.Brick, 1));

        private RelationshipMemory MemoryWith(float affinityTowardsHuman, PersonalityProfile personality)
        {
            var memory = new RelationshipMemory(1, personality);
            var type = affinityTowardsHuman >= 0 ? RelationshipEventType.TradeCompleted : RelationshipEventType.RobbedMe;
            while (affinityTowardsHuman >= 0 ? memory.Get(Human) < affinityTowardsHuman : memory.Get(Human) > affinityTowardsHuman)
            {
                memory.Record(Human, type);
            }

            return memory;
        }

        [Test]
        public void NeutralBot_TwoGrainForOneOre_IsNotRejected()
        {
            var offer = Offer(1, ResourceBundle.Of(ResourceType.Grain, 2), ResourceBundle.Of(ResourceType.Ore, 1));

            TradeDecision d = _evaluator.Evaluate(offer, 1, _state, Personality(), null, true);

            Assert.AreNotEqual(TradeDecisionType.Reject, d.Type, d.ToString());
        }

        [Test]
        public void BotWithoutTheResource_RejectsDontHaveIt()
        {
            var offer = Offer(2, ResourceBundle.Of(ResourceType.Wool, 1), ResourceBundle.Of(ResourceType.Wood, 1));

            TradeDecision d = _evaluator.Evaluate(offer, 2, _state, Personality(), null, true);

            Assert.AreEqual(TradeDecisionType.Reject, d.Type);
            Assert.AreEqual(TradeReason.DontHaveIt, d.Reason);
        }

        [Test]
        public void ProposerWithoutTheResource_RejectsYouDontHaveIt()
        {
            var ok = Offer(1, ResourceBundle.Of(ResourceType.Wool, 3), ResourceBundle.Of(ResourceType.Ore, 1));
            var tooMuch = Offer(1, ResourceBundle.Of(ResourceType.Wool, 5), ResourceBundle.Of(ResourceType.Ore, 1));

            Assert.AreNotEqual(TradeReason.YouDontHaveIt, _evaluator.Evaluate(ok, 1, _state, Personality(), null, true).Reason);
            TradeDecision d = _evaluator.Evaluate(tooMuch, 1, _state, Personality(), null, true);
            Assert.AreEqual(TradeDecisionType.Reject, d.Type);
            Assert.AreEqual(TradeReason.YouDontHaveIt, d.Reason);
        }

        [Test]
        public void GreedyBot_HasALowerMargin()
        {
            float greedy = _evaluator.Evaluate(FairOffer(), 1, _state, Personality(greed: 0.9f), null, true).Margin;
            float generous = _evaluator.Evaluate(FairOffer(), 1, _state, Personality(greed: 0.1f), null, true).Margin;

            Assert.Less(greedy, generous);
        }

        [Test]
        public void HigherAffinity_GivesAHigherMargin()
        {
            PersonalityProfile p = Personality();

            float friend = _evaluator.Evaluate(FairOffer(), 1, _state, p, MemoryWith(0.8f, p), true).Margin;
            float enemy = _evaluator.Evaluate(FairOffer(), 1, _state, p, MemoryWith(-0.5f, p), true).Margin;

            Assert.Greater(friend, enemy);
        }

        [Test]
        public void HatedProposer_RejectsGrudge()
        {
            PersonalityProfile p = Personality(grudge: 1f);

            TradeDecision d = _evaluator.Evaluate(FairOffer(), 1, _state, p, MemoryWith(-0.9f, p), true);

            Assert.AreEqual(TradeDecisionType.Reject, d.Type);
            Assert.AreEqual(TradeReason.Grudge, d.Reason);
        }

        [Test]
        public void AggressiveBot_PunishesTheLeader()
        {
            _state.Players[Human].VisiblePoints = 8;

            float aggressive = _evaluator.Evaluate(FairOffer(), 1, _state, Personality(aggression: 1f), null, true).Margin;
            float peaceful = _evaluator.Evaluate(FairOffer(), 1, _state, Personality(aggression: 0f), null, true).Margin;

            Assert.Less(aggressive, peaceful);
        }

        [Test]
        public void AggressiveBot_SaysItWillNotHelpTheLeader()
        {
            _state.Players[Human].VisiblePoints = 8;

            TradeDecision d = _evaluator.Evaluate(FairOffer(), 1, _state, Personality(aggression: 1f), null, true);

            Assert.AreEqual(TradeDecisionType.Reject, d.Type);
            Assert.AreEqual(TradeReason.HelpsTheLeader, d.Reason);
        }

        [Test]
        public void Gift_IsAcceptedAsAGoodDeal()
        {
            // The human GIVES 3 wool (what the bot gets) and ASKS for 1 grain (what the bot pays).
            var offer = Offer(1, ResourceBundle.Of(ResourceType.Wool, 3), ResourceBundle.Of(ResourceType.Grain, 1));

            TradeDecision d = _evaluator.Evaluate(offer, 1, _state, Personality(), null, true);

            Assert.AreEqual(TradeDecisionType.Accept, d.Type);
            Assert.AreEqual(TradeReason.GoodDeal, d.Reason);
            Assert.Greater(d.Gain, d.Cost, "Give is what the bot receives");
        }

        [Test]
        public void AlmostThere_CountersOnlyIfRoundsAreLeft()
        {
            PersonalityProfile p = Personality();

            TradeDecision withRounds = _evaluator.Evaluate(FairOffer(), 1, _state, p, null, true);
            TradeDecision lastRound = _evaluator.Evaluate(FairOffer(), 1, _state, p, null, false);

            Assert.Less(withRounds.Margin, 0f);
            Assert.AreEqual(TradeDecisionType.Counter, withRounds.Type);
            Assert.AreEqual(TradeReason.AlmostThere, withRounds.Reason);
            Assert.AreEqual(TradeDecisionType.Reject, lastRound.Type);
        }

        [Test]
        public void SameSeed_SameDecision()
        {
            PersonalityProfile p = Personality(randomness: 0.5f);

            TradeDecision a = new TradeEvaluator(new SimpleResourceValuator(), 99).Evaluate(FairOffer(), 1, _state, p, null, true);
            TradeDecision b = new TradeEvaluator(new SimpleResourceValuator(), 99).Evaluate(FairOffer(), 1, _state, p, null, true);

            Assert.AreEqual(a.Type, b.Type);
            Assert.AreEqual(a.Margin, b.Margin);
        }

        [Test]
        public void Randomness_ChangesTheMarginBetweenSeeds()
        {
            PersonalityProfile p = Personality(randomness: 0.5f);
            var margins = new HashSet<float>();

            for (int seed = 0; seed < 5; seed++)
            {
                margins.Add(new TradeEvaluator(new SimpleResourceValuator(), seed).Evaluate(FairOffer(), 1, _state, p, null, true).Margin);
            }

            Assert.Greater(margins.Count, 1);
        }

        [Test]
        public void SmallFriendlyBoost_AcceptsAsFriendlyDiscount()
        {
            PersonalityProfile p = Personality();

            TradeDecision d = _evaluator.Evaluate(FairOffer(), 1, _state, p, MemoryWith(0.5f, p), true);

            Assert.AreEqual(TradeDecisionType.Accept, d.Type);
            Assert.AreEqual(TradeReason.FriendlyDiscount, d.Reason);
        }

        [Test]
        public void Malformed_Offers_AreRejected()
        {
            PersonalityProfile p = Personality();
            var empty = Offer(1, ResourceBundle.Empty, ResourceBundle.Of(ResourceType.Ore, 1));
            var wrongBot = FairOffer();
            var unknown = new TradeOffer { FromPlayerId = 9, ToPlayerId = 1, Give = ResourceBundle.Of(ResourceType.Wood, 1), Receive = ResourceBundle.Of(ResourceType.Ore, 1) };

            Assert.AreEqual(TradeReason.Malformed, _evaluator.Evaluate(null, 1, _state, p, null, true).Reason);
            Assert.AreEqual(TradeReason.Malformed, _evaluator.Evaluate(empty, 1, _state, p, null, true).Reason);
            Assert.AreEqual(TradeReason.Malformed, _evaluator.Evaluate(wrongBot, 2, _state, p, null, true).Reason);
            Assert.AreEqual(TradeReason.Malformed, _evaluator.Evaluate(unknown, 1, _state, p, null, true).Reason);
        }

        [Test]
        public void Evaluate_DoesNotChangeTheState()
        {
            ResourceBundle before = _state.Players[1].Resources;

            _evaluator.Evaluate(FairOffer(), 1, _state, Personality(), null, true);

            Assert.AreEqual(before, _state.Players[1].Resources);
        }

        [Test]
        public void Value_UsesThePersonalityPreference()
        {
            PersonalityProfile neutral = Personality();
            PersonalityProfile loves = Personality();
            loves.orePreference = 1.5f;
            var ore = ResourceBundle.Of(ResourceType.Ore, 2);

            Assert.AreEqual(1.5f * _evaluator.Value(ore, 1, _state, neutral), _evaluator.Value(ore, 1, _state, loves), 1e-5f);
        }
    }
}
