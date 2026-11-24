using System.Collections.Generic;
using Chatan.AI;
using Chatan.Core;

namespace Chatan.Negotiation
{
    /// <summary>
    /// Everything the reply generator needs to answer with sense: the decision is already
    /// taken, the generator only puts it into words with the bot's voice.
    /// </summary>
    public class ReplyContext
    {
        /// <summary>The bot's personality.</summary>
        public PersonalityProfile Personality;

        /// <summary>What the human just wrote.</summary>
        public string HumanText;

        /// <summary>What the human meant.</summary>
        public IntentType HumanIntent;

        /// <summary>The bot's decision about the human's offer, or null when there was no offer.</summary>
        public TradeDecision Decision;

        /// <summary>The human's offer, or the deal they just accepted.</summary>
        public TradeOffer OfferOnTable;

        /// <summary>The bot's counter-offer, if it made one.</summary>
        public TradeOffer CounterOffer;

        /// <summary>What the bot feels about the human, from <see cref="RelationshipMemory.DescribeFor"/>.</summary>
        public string RelationshipSummary;

        /// <summary>A copy of the conversation so far, ending with the human's last message.</summary>
        public IReadOnlyList<NegotiationTurn> History;

        /// <summary>True on the last answer: the bot ends the negotiation for this turn.</summary>
        public bool SessionClosing;

        /// <summary>Current game state.</summary>
        public GameState State;

        /// <summary>The bot that answers.</summary>
        public int BotId;
    }
}
