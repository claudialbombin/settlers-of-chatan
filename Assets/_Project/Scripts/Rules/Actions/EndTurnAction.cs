using Chatan.Core;

namespace Chatan.Rules
{
    /// <summary>The current player ends their turn.</summary>
    /// <remarks>Placeholder until Teresa's version lands in <c>feature/build-actions</c>; keep the constructor signature.</remarks>
    public sealed class EndTurnAction : IGameAction
    {
        /// <summary>Creates the action.</summary>
        public EndTurnAction(int playerId)
        {
            PlayerId = playerId;
        }

        /// <inheritdoc/>
        public int PlayerId { get; }

        /// <inheritdoc/>
        public override string ToString() => $"EndTurn(P{PlayerId})";
    }
}
