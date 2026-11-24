using System.Collections.Generic;
using System.Linq;
using System.Text;
using Chatan.AI;
using Chatan.Core;
using Chatan.Dev;
using Chatan.Negotiation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Chatan.Tests
{
    /// <summary>
    /// Runs four typical offers against the ten shipped personalities, logs the table and
    /// checks that each personality behaves like its description.
    /// </summary>
    /// <remarks>
    /// The bot (seat 1) and the human hold 2 and 4 of everything, so wood and brick are
    /// both worth 1. Offers, as "what the bot gets × what it pays": fair 1×1, in favour
    /// 2×1, against 1×2 and gift 3×1. Copy the logged table to Docs/personalidades.md
    /// after tuning the coefficients of <see cref="TradeEvaluator"/>.
    /// </remarks>
    public class TradePersonalityTableTests
    {
        private const string CatalogPath = "Assets/_Project/Data/Personalities/PersonalityCatalog.asset";
        private const int Bot = 1;

        private static readonly (string Name, int Gets, int Pays)[] Offers =
        {
            ("justa 1×1", 1, 1),
            ("2×1 a favor", 2, 1),
            ("1×2 en contra", 1, 2),
            ("regalo 3×1", 3, 1),
        };

        private PersonalityCatalog _catalog;

        [SetUp]
        public void SetUp()
        {
            _catalog = AssetDatabase.LoadAssetAtPath<PersonalityCatalog>(CatalogPath);
            Assert.IsNotNull(_catalog, $"No catalog at {CatalogPath}.");
        }

        private static GameState State(bool humanLeads)
        {
            GameState state = FakeGameStateFactory.Create();
            state.Players[FakeGameStateFactory.HumanId].Resources = new ResourceBundle(4, 4, 4, 4, 4);
            state.Players[Bot].Resources = new ResourceBundle(2, 2, 2, 2, 2);
            state.Players[FakeGameStateFactory.HumanId].VisiblePoints = humanLeads ? 8 : 2;
            return state;
        }

        private static TradeOffer Offer(int gets, int pays) => new TradeOffer
        {
            FromPlayerId = FakeGameStateFactory.HumanId,
            ToPlayerId = Bot,
            Give = ResourceBundle.Of(ResourceType.Wood, gets),
            Receive = ResourceBundle.Of(ResourceType.Brick, pays)
        };

        private static TradeDecision Decide(PersonalityProfile p, int offerIndex, bool humanLeads, int seed = 0) =>
            new TradeEvaluator(new SimpleResourceValuator(), seed)
                .Evaluate(Offer(Offers[offerIndex].Gets, Offers[offerIndex].Pays), Bot, State(humanLeads), p, null, true);

        private PersonalityProfile Get(PersonalityId id) => _catalog.Get(id);

        [Test]
        public void LogTheTable()
        {
            var text = new StringBuilder("Decisions (A accept, C counter, R reject) — normal | human leading with 8 PV\n");
            text.AppendLine($"{"",-18}{string.Join("  ", Offers.Select(o => o.Name))}");
            foreach (PersonalityProfile p in _catalog.Profiles)
            {
                string Row(bool leads) => string.Join(" ", Enumerable.Range(0, Offers.Length).Select(i => Decide(p, i, leads).Type.ToString()[0]));
                text.AppendLine($"{p.displayName,-18}{Row(false)}  |  {Row(true)}");
            }

            Debug.Log(text.ToString());
            Assert.AreEqual(10, _catalog.Profiles.Count);
        }

        [TestCase(PersonalityId.Conciliator)]
        [TestCase(PersonalityId.Merchant)]
        public void Cooperative_AcceptTheFairOffer(PersonalityId id)
        {
            Assert.AreEqual(TradeDecisionType.Accept, Decide(Get(id), 0, false).Type);
        }

        [Test]
        public void Greedy_OnlyAcceptsDealsInItsFavour()
        {
            PersonalityProfile greedy = Get(PersonalityId.Greedy);

            Assert.AreNotEqual(TradeDecisionType.Accept, Decide(greedy, 0, false).Type, "fair");
            Assert.AreEqual(TradeDecisionType.Accept, Decide(greedy, 1, false).Type, "in favour");
            Assert.AreNotEqual(TradeDecisionType.Accept, Decide(greedy, 2, false).Type, "against");
            Assert.AreEqual(TradeDecisionType.Accept, Decide(greedy, 3, false).Type, "gift");
        }

        [Test]
        public void Aggressive_RejectsTheLeaderUnlessItIsABargain()
        {
            PersonalityProfile aggressive = Get(PersonalityId.Aggressive);

            Assert.AreEqual(TradeDecisionType.Reject, Decide(aggressive, 0, true).Type, "fair");
            Assert.AreEqual(TradeDecisionType.Reject, Decide(aggressive, 2, true).Type, "against");
            Assert.AreEqual(TradeReason.HelpsTheLeader, Decide(aggressive, 0, true).Reason);
        }

        [Test]
        public void Trickster_DoesNotAlwaysAnswerTheSame()
        {
            PersonalityProfile trickster = Get(PersonalityId.Trickster);
            var answers = new HashSet<TradeDecisionType>();

            for (int seed = 0; seed < 30; seed++)
            {
                answers.Add(Decide(trickster, 0, false, seed).Type);
            }

            Assert.Greater(answers.Count, 1);
        }

        [Test]
        public void Everybody_AcceptsTheGift()
        {
            foreach (PersonalityProfile p in _catalog.Profiles)
            {
                Assert.AreEqual(TradeDecisionType.Accept, Decide(p, 3, false).Type, p.displayName);
            }
        }

        [Test]
        public void Nobody_AcceptsTheOfferAgainstThem()
        {
            foreach (PersonalityProfile p in _catalog.Profiles)
            {
                Assert.AreNotEqual(TradeDecisionType.Accept, Decide(p, 2, false).Type, p.displayName);
            }
        }
    }
}
