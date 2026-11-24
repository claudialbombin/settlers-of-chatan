namespace Chatan.AI
{
    /// <summary>Things another player can do that change how a bot feels about them.</summary>
    /// <remarks>Keep the order stable: Unity serializes enums by their numeric value.</remarks>
    public enum RelationshipEventType
    {
        /// <summary>We closed a trade.</summary>
        TradeCompleted,

        /// <summary>They turned down one of my offers.</summary>
        TradeRejectedByThem,

        /// <summary>They put the robber on me and stole a card.</summary>
        RobbedMe,

        /// <summary>They built where I was about to expand (raised by the rules if they detect it).</summary>
        BlockedMyRoad,

        /// <summary>They traded with me while somebody else was winning.</summary>
        HelpedAgainstLeader,

        /// <summary>They insulted me in the chat (optional, if the parser detects it).</summary>
        InsultedInChat
    }
}
