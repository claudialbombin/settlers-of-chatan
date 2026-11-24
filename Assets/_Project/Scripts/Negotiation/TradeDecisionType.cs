namespace Chatan.Negotiation
{
    /// <summary>What a bot answers to a trade offer.</summary>
    public enum TradeDecisionType
    {
        /// <summary>The bot takes the deal as it is.</summary>
        Accept,

        /// <summary>The bot says no.</summary>
        Reject,

        /// <summary>The deal is close: the bot will answer with a slightly better offer for itself.</summary>
        Counter
    }
}
