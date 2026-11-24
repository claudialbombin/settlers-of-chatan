namespace Chatan.Negotiation
{
    /// <summary>
    /// Why a bot decided what it decided. The response generator turns it into words, so
    /// the bot's answer in the chat makes sense.
    /// </summary>
    /// <remarks>Keep the order stable: Unity serializes enums by their numeric value.</remarks>
    public enum TradeReason
    {
        /// <summary>Accepts: the deal is good for it.</summary>
        GoodDeal,

        /// <summary>Accepts only because it likes the proposer.</summary>
        FriendlyDiscount,

        /// <summary>Counters: it is not far from a deal.</summary>
        AlmostThere,

        /// <summary>Rejects: the offer gives too little for what it asks.</summary>
        TooLittle,

        /// <summary>Rejects: the bot does not have what is asked for.</summary>
        DontHaveIt,

        /// <summary>Rejects: the proposer does not have what they offer.</summary>
        YouDontHaveIt,

        /// <summary>Rejects: the proposer is winning and the bot will not help them.</summary>
        HelpsTheLeader,

        /// <summary>Rejects: the bot refuses to deal with somebody it hates.</summary>
        Grudge,

        /// <summary>Rejects: the offer makes no sense (empty side, to itself, not for this bot).</summary>
        Malformed
    }
}
