namespace Chatan.Negotiation
{
    /// <summary>The result of parsing one message of the human.</summary>
    public class ParsedIntent
    {
        /// <summary>What the human meant.</summary>
        public IntentType Type = IntentType.Unknown;

        /// <summary>The offer, filled in when <see cref="Type"/> is Propose or Counter.</summary>
        public TradeOffer Offer;
    }
}
