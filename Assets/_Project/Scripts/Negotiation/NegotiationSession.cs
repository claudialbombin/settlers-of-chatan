using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Chatan.AI;
using Chatan.Core;

namespace Chatan.Negotiation
{
    /// <summary>
    /// One chat negotiation between the human and one bot: the history, the offer on the
    /// table and how many rounds have been used.
    /// </summary>
    /// <remarks>
    /// <para>
    /// It coordinates the parser, the evaluator, the counter-offer generator and the reply
    /// generator, received by constructor, and knows nothing about LLMs or UI.
    /// </para>
    /// <para>
    /// Only <see cref="IntentType.Propose"/> and <see cref="IntentType.Counter"/> use up a
    /// round, so chatting never ends the negotiation. On the last round the bot cannot
    /// counter any more; once the rounds are used up and no deal is waiting, its answer closes the session.
    /// </para>
    /// <para>
    /// The bot never closes a deal by itself: when it accepts, the offer stays in
    /// <see cref="PendingBotOffer"/> and the human confirms it (typing "acepto" or with the
    /// popup button). The session then only marks <see cref="SessionStatus.DealAccepted"/>;
    /// executing the trade is the chat panel's job, through <see cref="ITradeExecutor"/>.
    /// </para>
    /// </remarks>
    public class NegotiationSession
    {
        /// <summary>Text of the system turn returned when the session is no longer open.</summary>
        public const string ClosedMessage = "La negociación está cerrada.";

        /// <summary>Summary used when the bot has no relationship memory.</summary>
        public const string NeutralRelationship = "te es indiferente";

        private static int _nextOfferId = 1;

        private readonly List<NegotiationTurn> _history = new List<NegotiationTurn>();
        private readonly PersonalityProfile _personality;
        private readonly RelationshipMemory _memory;
        private readonly IIntentParser _parser;
        private readonly TradeEvaluator _evaluator;
        private readonly CounterOfferGenerator _counter;
        private readonly IReplyGenerator _replies;

        /// <summary>Creates a session.</summary>
        /// <param name="humanId">The human player.</param>
        /// <param name="botId">The bot the human talks to.</param>
        /// <param name="personality">The bot's personality (its <c>maxNegotiationRounds</c> limits the session).</param>
        /// <param name="memory">The bot's relationship memory, or null.</param>
        /// <param name="parser">Turns the human's text into an intent.</param>
        /// <param name="evaluator">Decides about each offer.</param>
        /// <param name="counter">Builds the counter-offers.</param>
        /// <param name="replies">Writes the bot's answers.</param>
        public NegotiationSession(int humanId, int botId, PersonalityProfile personality, RelationshipMemory memory,
            IIntentParser parser, TradeEvaluator evaluator, CounterOfferGenerator counter, IReplyGenerator replies)
        {
            HumanId = humanId;
            BotId = botId;
            _personality = personality ?? throw new ArgumentNullException(nameof(personality));
            _memory = memory;
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
            _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
            _counter = counter ?? throw new ArgumentNullException(nameof(counter));
            _replies = replies ?? throw new ArgumentNullException(nameof(replies));
        }

        /// <summary>The human player.</summary>
        public int HumanId { get; }

        /// <summary>The bot.</summary>
        public int BotId { get; }

        /// <summary>Open, deal accepted or closed.</summary>
        public SessionStatus Status { get; private set; } = SessionStatus.Open;

        /// <summary>
        /// The deal waiting for the human's confirmation: the human's offer that the bot
        /// accepted, or the bot's counter-offer. Null when there is none.
        /// </summary>
        public TradeOffer PendingBotOffer { get; private set; }

        /// <summary>Every message so far, in order.</summary>
        public IReadOnlyList<NegotiationTurn> History => _history;

        /// <summary>Offers and counter-offers made by the human so far.</summary>
        public int RoundsUsed { get; private set; }

        /// <summary>Rounds the bot allows before ending the negotiation.</summary>
        public int MaxRounds => _personality.maxNegotiationRounds;

        /// <summary>Handles one message of the human and returns the bot's answer.</summary>
        /// <param name="text">What the human wrote.</param>
        /// <param name="state">Current game state. It is not modified.</param>
        /// <param name="ct">Cancels the parser and the reply generator.</param>
        /// <returns>The bot's turn (text, decision and the offer now waiting, if any), or a system turn when the session is not open.</returns>
        public async Task<NegotiationTurn> HandleHumanMessageAsync(string text, GameState state, CancellationToken ct = default)
        {
            if (Status != SessionStatus.Open)
            {
                return Add(Speaker.System, ClosedMessage, null, null);
            }

            Add(Speaker.Human, text, null, null);
            ParsedIntent intent = await _parser.ParseAsync(text, HumanId, BotId, state, ct) ?? new ParsedIntent();

            TradeDecision decision = null;
            TradeOffer onTable = null;
            TradeOffer counter = null;

            switch (intent.Type)
            {
                case IntentType.Propose:
                case IntentType.Counter:
                    if (intent.Offer == null)
                    {
                        break;
                    }

                    RoundsUsed++;
                    onTable = intent.Offer;
                    onTable.Id = _nextOfferId++;
                    onTable.FromPlayerId = HumanId;
                    onTable.ToPlayerId = BotId;

                    bool canCounter = RoundsUsed < MaxRounds;
                    decision = _evaluator.Evaluate(onTable, BotId, state, _personality, _memory, canCounter);
                    if (decision.Type == TradeDecisionType.Counter)
                    {
                        counter = _counter.Generate(onTable, BotId, state, _personality, _memory);
                        if (counter == null)
                        {
                            // Nothing reasonable to propose: it is a no after all.
                            decision.Type = TradeDecisionType.Reject;
                            decision.Reason = TradeReason.TooLittle;
                        }
                        else
                        {
                            counter.Id = _nextOfferId++;
                        }
                    }

                    onTable.Status = decision.Type switch
                    {
                        TradeDecisionType.Accept => TradeStatus.Accepted,
                        TradeDecisionType.Counter => TradeStatus.Countered,
                        _ => TradeStatus.Rejected
                    };
                    PendingBotOffer = decision.Type switch
                    {
                        TradeDecisionType.Accept => onTable,
                        TradeDecisionType.Counter => counter,
                        _ => null
                    };
                    break;

                case IntentType.Accept:
                    if (PendingBotOffer != null)
                    {
                        onTable = ConfirmPending();
                    }

                    break;

                case IntentType.Reject:
                    DropPending();
                    break;
            }

            bool closing = Status == SessionStatus.Open
                           && RoundsUsed >= MaxRounds
                           && PendingBotOffer == null;

            var context = new ReplyContext
            {
                Personality = _personality,
                HumanText = text,
                HumanIntent = intent.Type,
                Decision = decision,
                OfferOnTable = onTable,
                CounterOffer = counter,
                RelationshipSummary = _memory?.DescribeFor(HumanId) ?? NeutralRelationship,
                History = _history.ToArray(),
                SessionClosing = closing,
                State = state,
                BotId = BotId
            };
            string reply = await _replies.GenerateAsync(context, ct);

            if (closing)
            {
                Status = SessionStatus.Closed;
            }

            return Add(Speaker.Bot, reply, PendingBotOffer, decision);
        }

        /// <summary>The human pressed "Aceptar" on the popup, without typing.</summary>
        /// <returns>The deal to execute, or null if nothing was waiting.</returns>
        public TradeOffer AcceptPendingFromButton() =>
            Status == SessionStatus.Open && PendingBotOffer != null ? ConfirmPending() : null;

        /// <summary>The human pressed "Rechazar" on the popup. The bot takes it personally.</summary>
        public void RejectPendingFromButton() => DropPending();

        private TradeOffer ConfirmPending()
        {
            TradeOffer deal = PendingBotOffer;
            deal.Status = TradeStatus.Accepted;
            Status = SessionStatus.DealAccepted;
            return deal;
        }

        private void DropPending()
        {
            if (PendingBotOffer == null)
            {
                return;
            }

            PendingBotOffer.Status = TradeStatus.Rejected;
            _memory?.Record(HumanId, RelationshipEventType.TradeRejectedByThem);
            PendingBotOffer = null;
        }

        private NegotiationTurn Add(Speaker who, string text, TradeOffer offer, TradeDecision decision)
        {
            var turn = new NegotiationTurn
            {
                Speaker = who,
                Text = text,
                OfferOnTable = offer,
                Decision = decision,
                Time = UnityEngine.Time.realtimeSinceStartup
            };
            _history.Add(turn);
            return turn;
        }
    }
}
