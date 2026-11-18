using Chatan.Core;

namespace Chatan.AI
{
    /// <summary>
    /// Estimates how useful a resource is to a given player at this point of the match.
    /// </summary>
    /// <remarks>
    /// The trade evaluator depends on this abstraction instead of a concrete valuation
    /// strategy, so the heuristic can be replaced or tuned without touching the
    /// negotiation code (Dependency Inversion / Open-Closed).
    /// </remarks>
    public interface IResourceValuator
    {
        /// <summary>
        /// Value of ONE unit of <paramref name="type"/> for <paramref name="playerId"/> right now.
        /// </summary>
        /// <param name="type">The resource to value.</param>
        /// <param name="playerId">The player from whose point of view it is valued.</param>
        /// <param name="state">The current game state. It is not modified.</param>
        /// <returns>A positive score, roughly between 0.5 (barely needed) and 3 (badly needed).</returns>
        float ValueOf(ResourceType type, int playerId, GameState state);
    }
}
