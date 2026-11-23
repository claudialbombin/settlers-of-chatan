using Chatan.AI;
using Chatan.Core;

namespace Chatan.Negotiation
{
    /// <summary>
    /// Placeholder valuation until Teresa's <c>ResourceValuator</c> (F4) exists: a fixed
    /// base value per resource, adjusted by how many of it the player already holds.
    /// </summary>
    /// <remarks>
    /// Ore and grain are worth a bit more (cities and cards need them), wool a bit less.
    /// Having none makes a resource worth ×1.4; having 4 or more, ×0.7. Swapping it for
    /// the real one is a single line where the <see cref="TradeEvaluator"/> is created.
    /// </remarks>
    public class SimpleResourceValuator : IResourceValuator
    {
        /// <summary>Multiplier when the player has none of the resource.</summary>
        public const float MissingMultiplier = 1.4f;

        /// <summary>Multiplier when the player has <see cref="PlentyThreshold"/> or more.</summary>
        public const float PlentyMultiplier = 0.7f;

        /// <summary>Amount from which a resource counts as plentiful.</summary>
        public const int PlentyThreshold = 4;

        /// <inheritdoc/>
        public float ValueOf(ResourceType type, int playerId, GameState state)
        {
            PlayerState player = state?.Players.Find(p => p.Id == playerId);
            float scarcity = 1f;
            if (player != null)
            {
                int have = player.Resources[type];
                scarcity = have == 0 ? MissingMultiplier : have >= PlentyThreshold ? PlentyMultiplier : 1f;
            }

            return BaseValue(type) * scarcity;
        }

        /// <summary>Value of one unit before looking at the player's hand.</summary>
        public static float BaseValue(ResourceType type) => type switch
        {
            ResourceType.Ore => 1.2f,
            ResourceType.Grain => 1.1f,
            ResourceType.Wool => 0.8f,
            _ => 1f
        };
    }
}
