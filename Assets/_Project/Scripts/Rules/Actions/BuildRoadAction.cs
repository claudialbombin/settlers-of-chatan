using Chatan.Core;

namespace Chatan.Rules
{
    /// <summary>The player builds a road on an edge of the board.</summary>
    /// <remarks>Placeholder until Teresa's version lands in <c>feature/build-actions</c>; keep the constructor signature.</remarks>
    public sealed class BuildRoadAction : IGameAction
    {
        /// <summary>Creates the action.</summary>
        public BuildRoadAction(int playerId, int edgeId)
        {
            PlayerId = playerId;
            EdgeId = edgeId;
        }

        /// <inheritdoc/>
        public int PlayerId { get; }

        /// <summary>Id of the board edge where the road goes.</summary>
        public int EdgeId { get; }

        /// <inheritdoc/>
        public override string ToString() => $"BuildRoad(P{PlayerId}, edge {EdgeId})";
    }
}
