using System;
using Chatan.Core;

namespace Chatan.Negotiation
{
    /// <summary>
    /// A structured trade proposal between two players: "player A gives X to player B
    /// in exchange for Y".
    /// </summary>
    /// <remarks>
    /// This is what the chat turns free text into and what the trade evaluator reasons
    /// about. It only describes the deal; it never moves resources itself
    /// (see <see cref="ITradeExecutor"/>).
    /// </remarks>
    [Serializable]
    public class TradeOffer
    {
        /// <summary>Unique identifier within the match.</summary>
        public int Id;

        /// <summary>Id of the player who makes the proposal.</summary>
        public int FromPlayerId;

        /// <summary>Id of the player the proposal is addressed to.</summary>
        public int ToPlayerId;

        /// <summary>What the proposer GIVES away.</summary>
        public ResourceBundle Give;

        /// <summary>What the proposer ASKS for in return.</summary>
        public ResourceBundle Receive;

        /// <summary>Where the offer is in its life cycle.</summary>
        public TradeStatus Status = TradeStatus.Proposed;

        /// <summary>
        /// True when the offer is well formed: both sides give something and the two
        /// players are different. It does not check whether they own those cards;
        /// that is <see cref="ITradeExecutor.CanExecute"/>'s job.
        /// </summary>
        public bool IsValidShape => !Give.IsEmpty && !Receive.IsEmpty && FromPlayerId != ToPlayerId;

        /// <summary>
        /// The same deal seen from the other side: players swap places and what one
        /// gives the other receives. Useful when a bot evaluates an offer made to it.
        /// </summary>
        /// <returns>A new offer; this instance is not modified.</returns>
        public TradeOffer Reversed() => new TradeOffer
        {
            Id = Id,
            FromPlayerId = ToPlayerId,
            ToPlayerId = FromPlayerId,
            Give = Receive,
            Receive = Give,
            Status = Status
        };

        /// <summary>Short description for logs, e.g. "#3 J0→J2: da [2 trigo] por [1 mineral]".</summary>
        public override string ToString() => $"#{Id} J{FromPlayerId}→J{ToPlayerId}: da [{Give}] por [{Receive}]";
    }
}
