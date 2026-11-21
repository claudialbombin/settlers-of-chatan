using Chatan.Core;

namespace Chatan.Rules
{
    /// <summary>The player buys the top development card from the deck.</summary>
    /// <remarks>Placeholder until Teresa's version lands in <c>feature/build-actions</c>; keep the constructor signature.</remarks>
    public sealed class BuyDevCardAction : IGameAction
    {
        /// <summary>Creates the action.</summary>
        public BuyDevCardAction(int playerId)
        {
            PlayerId = playerId;
        }

        /// <inheritdoc/>
        public int PlayerId { get; }

        /// <inheritdoc/>
        public override string ToString() => $"BuyDevCard(P{PlayerId})";
    }
}
