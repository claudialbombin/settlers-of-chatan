using Chatan.Core;

namespace Chatan.Rules
{
    /// <summary>After a 7, the player returns half of their cards to the bank.</summary>
    /// <remarks>Placeholder until Teresa's version lands in <c>feature/build-actions</c>; keep the constructor signature.</remarks>
    public sealed class DiscardAction : IGameAction
    {
        /// <summary>Creates the action.</summary>
        public DiscardAction(int playerId, ResourceBundle cards)
        {
            PlayerId = playerId;
            Cards = cards;
        }

        /// <inheritdoc/>
        public int PlayerId { get; }

        /// <summary>The cards to discard.</summary>
        public ResourceBundle Cards { get; }

        /// <inheritdoc/>
        public override string ToString() => $"Discard(P{PlayerId}, {Cards})";
    }
}
