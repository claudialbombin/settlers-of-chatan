using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Chatan.AI;
using Chatan.Core;
using Chatan.Dev;
using Chatan.Negotiation;
using NUnit.Framework;
using UnityEngine;

namespace Chatan.Tests
{
    /// <summary>
    /// Unit tests for <see cref="NegotiationSession"/> and <see cref="NegotiationManager"/>
    /// with a fake parser (returns the intents the test queues) and a fake reply generator
    /// (records every context). The human (0) talks to Bot Azul (1).
    /// </summary>
    public class NegotiationSessionTests
    {
        private const int Human = FakeGameStateFactory.HumanId;
        private const int Bot = 1;

        private sealed class FakeParser : IIntentParser
        {
            public readonly Queue<ParsedIntent> Next = new Queue<ParsedIntent>();

            public Task<ParsedIntent> ParseAsync(string humanText, int humanId, int botId, GameState s, CancellationToken ct) =>
                Task.FromResult(Next.Count > 0 ? Next.Dequeue() : new ParsedIntent { Type = IntentType.SmallTalk });
        }

        private sealed class FakeReplies : IReplyGenerator
        {
            public readonly List<ReplyContext> Contexts = new List<ReplyContext>();

            public Task<string> GenerateAsync(ReplyContext context, CancellationToken ct)
            {
                Contexts.Add(context);
                return Task.FromResult("respuesta " + Contexts.Count);
            }
        }

        private GameState _state;
        private PersonalityProfile _personality;
        private RelationshipMemory _memory;
        private FakeParser _parser;
        private FakeReplies _replies;
        private NegotiationSession _session;

        [SetUp]
        public void SetUp()
        {
            _state = FakeGameStateFactory.Create();
            _personality = ScriptableObject.CreateInstance<PersonalityProfile>();
            _personality.randomness = 0f;
            _personality.maxNegotiationRounds = 2;
            _memory = new RelationshipMemory(Bot, _personality);
            _parser = new FakeParser();
            _replies = new FakeReplies();
            _session = NewSession(Bot);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_personality);

        private NegotiationSession NewSession(int botId)
        {
            var evaluator = new TradeEvaluator(new SimpleResourceValuator());
            return new NegotiationSession(Human, botId, _personality, _memory, _parser, evaluator,
                                          new CounterOfferGenerator(evaluator), _replies);
        }

        private void Queue(IntentType type, ResourceBundle give = default, ResourceBundle receive = default)
        {
            TradeOffer offer = type is IntentType.Propose or IntentType.Counter
                ? new TradeOffer { FromPlayerId = Human, ToPlayerId = Bot, Give = give, Receive = receive }
                : null;
            _parser.Next.Enqueue(new ParsedIntent { Type = type, Offer = offer });
        }

        // The fakes finish synchronously, so waiting here never blocks.
        private NegotiationTurn Say(string text) => _session.HandleHumanMessageAsync(text, _state).GetAwaiter().GetResult();

        // 2 grain (2.2) for 1 brick (1.0): a neutral bot accepts.
        private void QueueGoodOffer() =>
            Queue(IntentType.Propose, ResourceBundle.Of(ResourceType.Grain, 2), ResourceBundle.Of(ResourceType.Brick, 1));

        // 1 wood for 1 brick: a neutral bot wants a little more and counters.
        private void QueueAlmostOffer() =>
            Queue(IntentType.Propose, ResourceBundle.Of(ResourceType.Wood, 1), ResourceBundle.Of(ResourceType.Brick, 1));

        // 1 wool for 3 ore: far too little.
        private void QueueBadOffer() =>
            Queue(IntentType.Propose, ResourceBundle.Of(ResourceType.Wool, 1), ResourceBundle.Of(ResourceType.Ore, 3));

        [Test]
        public void AcceptableProposal_BotAccepts_AndWaitsForConfirmation()
        {
            QueueGoodOffer();

            NegotiationTurn turn = Say("te doy 2 trigo por 1 ladrillo");

            Assert.AreEqual(Speaker.Bot, turn.Speaker);
            Assert.AreEqual(TradeDecisionType.Accept, turn.Decision.Type);
            Assert.IsNotNull(_session.PendingBotOffer);
            Assert.AreSame(_session.PendingBotOffer, turn.OfferOnTable);
            Assert.AreEqual(SessionStatus.Open, _session.Status, "the human has the last word");
        }

        [Test]
        public void RejectedUntilMaxRounds_ClosesTheSession()
        {
            QueueBadOffer();
            QueueBadOffer();

            Say("1 lana por 3 minerales");
            Assert.IsFalse(_replies.Contexts[0].SessionClosing);
            Say("venga, 1 lana por 3 minerales");

            Assert.AreEqual(SessionStatus.Closed, _session.Status);
            Assert.IsTrue(_replies.Contexts[1].SessionClosing);
            Assert.AreEqual(2, _session.RoundsUsed);

            NegotiationTurn after = Say("¿y ahora?");
            Assert.AreEqual(Speaker.System, after.Speaker);
            Assert.AreEqual(NegotiationSession.ClosedMessage, after.Text);
            Assert.AreEqual(2, _replies.Contexts.Count, "a closed session does not ask for more replies");
        }

        [Test]
        public void CounterOffer_ThenHumanAccepts_DealAccepted()
        {
            QueueAlmostOffer();
            NegotiationTurn counterTurn = Say("1 madera por 1 ladrillo");
            Assume.That(counterTurn.Decision.Type, Is.EqualTo(TradeDecisionType.Counter));
            TradeOffer counter = _session.PendingBotOffer;
            Assert.AreSame(counter, _replies.Contexts[0].CounterOffer);

            Queue(IntentType.Accept);
            NegotiationTurn turn = Say("vale, acepto");

            Assert.AreEqual(SessionStatus.DealAccepted, _session.Status);
            Assert.AreSame(counter, turn.OfferOnTable);
            Assert.AreEqual(TradeStatus.Accepted, counter.Status);
            Assert.AreSame(counter, _replies.Contexts[1].OfferOnTable);
        }

        [Test]
        public void RejectingACounterOffer_LowersTheAffinity()
        {
            QueueAlmostOffer();
            Say("1 madera por 1 ladrillo");
            Assume.That(_session.PendingBotOffer, Is.Not.Null);

            Queue(IntentType.Reject);
            Say("ni de broma");

            Assert.Less(_memory.Get(Human), 0f);
            Assert.IsNull(_session.PendingBotOffer);
        }

        [Test]
        public void SmallTalk_DoesNotUseUpRounds()
        {
            for (int i = 0; i < 5; i++)
            {
                Say("¿qué tal?");
            }

            Assert.AreEqual(0, _session.RoundsUsed);
            Assert.AreEqual(SessionStatus.Open, _session.Status);
            Assert.AreEqual(10, _session.History.Count, "a human and a bot turn per message");
        }

        [Test]
        public void LastRound_AlmostBecomesAReject()
        {
            QueueBadOffer();
            QueueAlmostOffer();

            Say("1 lana por 3 minerales");
            NegotiationTurn last = Say("1 madera por 1 ladrillo");

            Assert.AreEqual(TradeDecisionType.Reject, last.Decision.Type);
            Assert.AreEqual(SessionStatus.Closed, _session.Status);
        }

        [Test]
        public void AcceptOnTheLastRound_LeavesTheDealWaiting()
        {
            QueueBadOffer();
            QueueGoodOffer();

            Say("1 lana por 3 minerales");
            Say("2 trigo por 1 ladrillo");
            Say("genial");

            Assert.AreEqual(SessionStatus.Open, _session.Status);
            Assert.IsNotNull(_session.AcceptPendingFromButton());
            Assert.AreEqual(SessionStatus.DealAccepted, _session.Status);
        }

        [Test]
        public void ButtonReject_DropsTheOfferAndLowersTheAffinity()
        {
            QueueGoodOffer();
            Say("2 trigo por 1 ladrillo");

            _session.RejectPendingFromButton();

            Assert.IsNull(_session.PendingBotOffer);
            Assert.IsNull(_session.AcceptPendingFromButton());
            Assert.Less(_memory.Get(Human), 0f);
        }

        [Test]
        public void ReplyContext_CarriesTheRelationshipAndTheHistory()
        {
            _memory.Record(Human, RelationshipEventType.RobbedMe);
            QueueGoodOffer();

            Say("2 trigo por 1 ladrillo");

            ReplyContext context = _replies.Contexts[0];
            Assert.AreEqual(_memory.DescribeFor(Human), context.RelationshipSummary);
            Assert.AreEqual(Bot, context.BotId);
            Assert.AreEqual(IntentType.Propose, context.HumanIntent);
            Assert.AreEqual(Speaker.Human, context.History[context.History.Count - 1].Speaker);
        }

        [Test]
        public void Manager_ReusesOpenSessions_AndReplacesFinishedOnes()
        {
            int created = 0;
            var manager = new NegotiationManager(id => { created++; return created == 1 ? _session : NewSession(id); });

            NegotiationSession first = manager.Get(Bot);
            Assert.AreSame(first, manager.Get(Bot));
            Assert.AreEqual(1, created);

            QueueGoodOffer();
            Say("2 trigo por 1 ladrillo");
            first.AcceptPendingFromButton();
            manager.Get(Bot);
            Assert.AreEqual(2, created, "a finished session is replaced");

            manager.Get(2);
            Assert.AreEqual(2, manager.Count);
            manager.ResetAll();
            Assert.AreEqual(0, manager.Count);
            Assert.IsNull(manager.Peek(Bot));
        }
    }
}
