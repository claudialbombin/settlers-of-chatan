using Chatan.Core;

namespace Chatan.Rules
{
    /// <summary>The player builds a settlement on a vertex of the board.</summary>
    /// <remarks>Placeholder until Teresa's version lands in <c>feature/build-actions</c>; keep the constructor signature.</remarks>
    public sealed class BuildSettlementAction : IGameAction
    {
        /// <summary>Creates the action.</summary>
        public BuildSettlementAction(int playerId, int vertexId)
        {
            PlayerId = playerId;
            VertexId = vertexId;
        }

        /// <inheritdoc/>
        public int PlayerId { get; }

        /// <summary>Id of the board vertex where the settlement goes.</summary>
        public int VertexId { get; }

        /// <inheritdoc/>
        public override string ToString() => $"BuildSettlement(P{PlayerId}, vertex {VertexId})";
    }
}
