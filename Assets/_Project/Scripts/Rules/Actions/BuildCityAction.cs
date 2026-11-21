using Chatan.Core;

namespace Chatan.Rules
{
    /// <summary>The player upgrades one of their settlements to a city.</summary>
    /// <remarks>Placeholder until Teresa's version lands in <c>feature/build-actions</c>; keep the constructor signature.</remarks>
    public sealed class BuildCityAction : IGameAction
    {
        /// <summary>Creates the action.</summary>
        public BuildCityAction(int playerId, int vertexId)
        {
            PlayerId = playerId;
            VertexId = vertexId;
        }

        /// <inheritdoc/>
        public int PlayerId { get; }

        /// <summary>Id of the vertex holding the settlement to upgrade.</summary>
        public int VertexId { get; }

        /// <inheritdoc/>
        public override string ToString() => $"BuildCity(P{PlayerId}, vertex {VertexId})";
    }
}
