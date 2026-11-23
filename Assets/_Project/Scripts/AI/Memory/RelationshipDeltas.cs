using System;

namespace Chatan.AI
{
    /// <summary>
    /// How much each <see cref="RelationshipEventType"/> moves the affinity before the
    /// personality is taken into account, and how to say it in a prompt.
    /// </summary>
    /// <remarks>Positive means "I like them more". Tune the numbers here, not in the memory.</remarks>
    public static class RelationshipDeltas
    {
        /// <summary>Base change of affinity for an event.</summary>
        public static float Base(RelationshipEventType type) => type switch
        {
            RelationshipEventType.TradeCompleted => +0.15f,
            RelationshipEventType.TradeRejectedByThem => -0.05f,
            RelationshipEventType.RobbedMe => -0.35f,
            RelationshipEventType.BlockedMyRoad => -0.20f,
            RelationshipEventType.HelpedAgainstLeader => +0.20f,
            RelationshipEventType.InsultedInChat => -0.10f,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown relationship event.")
        };

        /// <summary>Spanish description from the bot's point of view, e.g. "te robó con el ladrón".</summary>
        public static string Describe(RelationshipEventType type) => type switch
        {
            RelationshipEventType.TradeCompleted => "cerrasteis un trato",
            RelationshipEventType.TradeRejectedByThem => "te rechazó una oferta",
            RelationshipEventType.RobbedMe => "te robó con el ladrón",
            RelationshipEventType.BlockedMyRoad => "te bloqueó el camino",
            RelationshipEventType.HelpedAgainstLeader => "te ayudó contra el que va ganando",
            RelationshipEventType.InsultedInChat => "te insultó en el chat",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown relationship event.")
        };
    }
}
