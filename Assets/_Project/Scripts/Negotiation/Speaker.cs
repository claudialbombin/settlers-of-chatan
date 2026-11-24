namespace Chatan.Negotiation
{
    /// <summary>Who wrote a message of a negotiation.</summary>
    public enum Speaker
    {
        /// <summary>The human player.</summary>
        Human,

        /// <summary>The bot being negotiated with.</summary>
        Bot,

        /// <summary>The game itself, e.g. "La negociación está cerrada."</summary>
        System
    }
}
