using System.Linq;
using Chatan.AI;
using Chatan.Core;
using Chatan.LLM;
using Chatan.Negotiation;
using NUnit.Framework;
using UnityEngine;

namespace Chatan.Tests
{
    /// <summary>Unit tests for <see cref="PromptBuilder"/>.</summary>
    public class PromptBuilderTests
    {
        private PersonalityProfile _personality;

        [SetUp]
        public void SetUp()
        {
            _personality = ScriptableObject.CreateInstance<PersonalityProfile>();
            _personality.chatTemperature = 0.4f;
            _personality.bluffing = 0f;
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_personality);

        private static TradeOffer Offer(ResourceBundle give, ResourceBundle receive) =>
            new TradeOffer { FromPlayerId = 0, ToPlayerId = 1, Give = give, Receive = receive };

        private ReplyContext Context(TradeDecisionType? type, TradeReason reason = TradeReason.GoodDeal) => new ReplyContext
        {
            Personality = _personality,
            HumanText = "te doy 2 trigo por 1 mineral",
            HumanIntent = IntentType.Propose,
            Decision = type.HasValue ? new TradeDecision { Type = type.Value, Reason = reason } : null,
            OfferOnTable = Offer(ResourceBundle.Of(ResourceType.Grain, 2), ResourceBundle.Of(ResourceType.Ore, 1)),
            RelationshipSummary = "te cae bien",
            BotId = 1
        };

        [Test]
        public void ParseRequest_AsksForJson_WithTheHumanTextLast()
        {
            LlmRequest r = PromptBuilder.BuildParseRequest("te doy 2 trigo por 1 mineral", null);

            Assert.AreEqual(LlmPurpose.ParseIntent, r.Purpose);
            Assert.IsTrue(r.ExpectJson);
            Assert.AreEqual(0f, r.Temperature);
            Assert.AreEqual(2, r.Messages.Count);
            Assert.AreEqual(LlmRole.System, r.Messages[0].Role);
            Assert.AreEqual(PromptBuilder.PlayerPrefix + "te doy 2 trigo por 1 mineral", r.Messages[1].Content);
        }

        [Test]
        public void ParseRequest_WithLastBotOffer_AddsContext()
        {
            LlmRequest r = PromptBuilder.BuildParseRequest("mejor 3", "me das 2 trigos y te doy 1 mineral");

            Assert.AreEqual(3, r.Messages.Count);
            StringAssert.Contains("me das 2 trigos y te doy 1 mineral", r.Messages[1].Content);
        }

        [TestCase(TradeDecisionType.Accept, "decision: accept")]
        [TestCase(TradeDecisionType.Reject, "decision: reject")]
        [TestCase(TradeDecisionType.Counter, "decision: counter")]
        public void Facts_ContainTheMarkerTheMockReads(TradeDecisionType type, string marker)
        {
            string facts = PromptBuilder.DescribeFacts(Context(type));

            StringAssert.Contains(marker, facts);
            StringAssert.Contains(PromptBuilder.DecisionToSpanish(type), facts, "said in Spanish too");
        }

        [Test]
        public void MockMarkers_MatchTheBuilder()
        {
            StringAssert.Contains(MockLanguageModel.AcceptMarker, PromptBuilder.DescribeFacts(Context(TradeDecisionType.Accept)));
            StringAssert.Contains(MockLanguageModel.RejectMarker, PromptBuilder.DescribeFacts(Context(TradeDecisionType.Reject)));
            StringAssert.Contains(MockLanguageModel.CounterMarker, PromptBuilder.DescribeFacts(Context(TradeDecisionType.Counter)));
        }

        [Test]
        public void Facts_IncludeOfferCounterReasonAndFeeling()
        {
            ReplyContext c = Context(TradeDecisionType.Counter, TradeReason.AlmostThere);
            c.CounterOffer = Offer(ResourceBundle.Of(ResourceType.Grain, 3), ResourceBundle.Of(ResourceType.Ore, 1));

            string facts = PromptBuilder.DescribeFacts(c);

            StringAssert.Contains("Te ofrece 2 trigos a cambio de 1 mineral", facts);
            StringAssert.Contains("que te dé 3 trigos y tú le das 1 mineral", facts);
            StringAssert.Contains(PromptBuilder.ReasonToSpanish(TradeReason.AlmostThere), facts);
            StringAssert.Contains("te cae bien", facts);
        }

        [Test]
        public void Facts_WithoutDecision_DependOnTheIntent()
        {
            ReplyContext c = Context(null);
            c.HumanIntent = IntentType.AskNeeds;
            StringAssert.Contains("qué necesitas", PromptBuilder.DescribeFacts(c));

            c.HumanIntent = IntentType.Accept;
            StringAssert.Contains("decision: confirmed", PromptBuilder.DescribeFacts(c));

            c.HumanIntent = IntentType.Accept;
            c.OfferOnTable = null;
            StringAssert.DoesNotContain("confirmed", PromptBuilder.DescribeFacts(c), "nothing to confirm");
        }

        [Test]
        public void Facts_OnClosing_TellTheBotToEndIt()
        {
            ReplyContext c = Context(TradeDecisionType.Reject);
            c.SessionClosing = true;

            StringAssert.Contains("última respuesta", PromptBuilder.DescribeFacts(c));
        }

        [Test]
        public void ReplyRequest_UsesThePersonalityTemperature_AndEndsWithTheFacts()
        {
            ReplyContext c = Context(TradeDecisionType.Accept);

            LlmRequest r = PromptBuilder.BuildReplyRequest(c);

            Assert.AreEqual(LlmPurpose.GenerateReply, r.Purpose);
            Assert.IsFalse(r.ExpectJson);
            Assert.AreEqual(0.4f, r.Temperature);
            Assert.AreEqual(LlmRole.System, r.Messages[0].Role);
            StringAssert.Contains(PromptBuilder.ReplyRules, r.Messages[0].Content);
            Assert.AreEqual(PromptBuilder.DescribeFacts(c), r.Messages.Last().Content);
        }

        [Test]
        public void ReplyRequest_OnlyBluffersMayLie()
        {
            Assert.IsFalse(PromptBuilder.BuildReplyRequest(Context(TradeDecisionType.Accept)).Messages[0].Content.Contains(PromptBuilder.BluffRule));

            _personality.bluffing = 0.9f;
            StringAssert.Contains(PromptBuilder.BluffRule, PromptBuilder.BuildReplyRequest(Context(TradeDecisionType.Accept)).Messages[0].Content);
        }

        [Test]
        public void ReplyRequest_AddsTheLastMessages_WithoutRepeatingTheCurrentOne()
        {
            ReplyContext c = Context(TradeDecisionType.Accept);
            c.History = new[]
            {
                new NegotiationTurn { Speaker = Speaker.Human, Text = "hola" },
                new NegotiationTurn { Speaker = Speaker.Bot, Text = "¿qué quieres?" },
                new NegotiationTurn { Speaker = Speaker.System, Text = "sistema" },
                new NegotiationTurn { Speaker = Speaker.Human, Text = "uno" },
                new NegotiationTurn { Speaker = Speaker.Bot, Text = "dos" },
                new NegotiationTurn { Speaker = Speaker.Human, Text = c.HumanText }
            };

            LlmRequest r = PromptBuilder.BuildReplyRequest(c);

            // system + last 4 before the current (minus the system turn) + facts
            string[] middle = r.Messages.Skip(1).Take(r.Messages.Count - 2).Select(m => m.Content).ToArray();
            CollectionAssert.AreEqual(new[] { "¿qué quieres?", "uno", "dos" }, middle);
            Assert.AreEqual(LlmRole.Assistant, r.Messages[1].Role);
        }

        [Test]
        public void ReplyRequest_WithoutPersonality_StillWorks()
        {
            ReplyContext c = Context(TradeDecisionType.Reject);
            c.Personality = null;

            LlmRequest r = PromptBuilder.BuildReplyRequest(c);

            Assert.AreEqual(PromptBuilder.DefaultReplyTemperature, r.Temperature);
            StringAssert.StartsWith("Eres un jugador de Catan.", r.Messages[0].Content);
        }

        [Test]
        public void Spanish_UsesPluralsAndNada()
        {
            Assert.AreEqual("nada", PromptBuilder.Spanish(ResourceBundle.Empty));
            Assert.AreEqual("1 madera, 2 lanas, 3 minerales", PromptBuilder.Spanish(new ResourceBundle(wood: 1, wool: 2, ore: 3)));
            Assert.AreEqual("me das 2 trigos y te doy 1 ladrillo",
                PromptBuilder.DescribeOffer(Offer(ResourceBundle.Of(ResourceType.Grain, 2), ResourceBundle.Of(ResourceType.Brick, 1))));
            Assert.IsNull(PromptBuilder.DescribeOffer(null));
        }

        [Test]
        public void EveryReason_HasAText()
        {
            foreach (TradeReason reason in System.Enum.GetValues(typeof(TradeReason)))
            {
                Assert.IsFalse(string.IsNullOrEmpty(PromptBuilder.ReasonToSpanish(reason)), reason.ToString());
            }
        }
    }
}
