namespace Chatan.Negotiation
{
    /// <summary>
    /// Life cycle of a <see cref="TradeOffer"/> between two players.
    /// </summary>
    public enum TradeStatus
    {
        /// <summary>Sent and waiting for an answer.</summary>
        Proposed,

        /// <summary>The receiver agreed. It still has to be executed by an <see cref="ITradeExecutor"/>.</summary>
        Accepted,

        /// <summary>The receiver said no.</summary>
        Rejected,

        /// <summary>The receiver answered with a different offer, which replaces this one.</summary>
        Countered,

        /// <summary>No longer valid (the turn ended or a player no longer has the cards).</summary>
        Expired
    }
}
