namespace Chatan.Negotiation
{
    /// <summary>
    /// The answer of <see cref="TradeEvaluator"/> to one offer, with the numbers behind it.
    /// </summary>
    /// <remarks>
    /// <see cref="Gain"/>, <see cref="Cost"/> and <see cref="Margin"/> are in "value points"
    /// from the bot's point of view. They are 0 when the offer was rejected before the
    /// formula (a hard check failed). The counter-offer generator uses the margin to know
    /// how much to ask for.
    /// </remarks>
    public class TradeDecision
    {
        /// <summary>Accept, reject or counter.</summary>
        public TradeDecisionType Type;

        /// <summary>Why.</summary>
        public TradeReason Reason;

        /// <summary>Value of what the bot would receive.</summary>
        public float Gain;

        /// <summary>Value of what the bot would give away.</summary>
        public float Cost;

        /// <summary>How good the deal is once personality and affinity are applied. Accept at 0 or more.</summary>
        public float Margin;

        /// <summary>True for <see cref="TradeDecisionType.Accept"/>.</summary>
        public bool IsAccepted => Type == TradeDecisionType.Accept;

        /// <summary>Debug text, e.g. "Counter (AlmostThere) gana=1.00 cuesta=1.00 margen=-0.04".</summary>
        public override string ToString() =>
            $"{Type} ({Reason}) gana={Gain:0.00} cuesta={Cost:0.00} margen={Margin:0.00}";
    }
}
