namespace Chatan.Negotiation
{
    /// <summary>One message of a <see cref="NegotiationSession"/>'s history.</summary>
    public class NegotiationTurn
    {
        /// <summary>Who wrote it.</summary>
        public Speaker Speaker;

        /// <summary>What was said.</summary>
        public string Text;

        /// <summary>The offer waiting for the human's answer after this message, or null.</summary>
        public TradeOffer OfferOnTable;

        /// <summary>The bot's decision. Only set on bot turns that answered an offer.</summary>
        public TradeDecision Decision;

        /// <summary>Seconds since the game started (<c>Time.realtimeSinceStartup</c>) when it was created.</summary>
        public float Time;

        /// <summary>Debug text, e.g. "Bot: Trato hecho".</summary>
        public override string ToString() => $"{Speaker}: {Text}";
    }
}
