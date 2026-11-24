namespace Chatan.Negotiation
{
    /// <summary>State of a <see cref="NegotiationSession"/>.</summary>
    public enum SessionStatus
    {
        /// <summary>Still talking.</summary>
        Open,

        /// <summary>The human accepted a deal. Whoever owns the session must execute it.</summary>
        DealAccepted,

        /// <summary>The bot ran out of rounds and ended the negotiation for this turn.</summary>
        Closed
    }
}
