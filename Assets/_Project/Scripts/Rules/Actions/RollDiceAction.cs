using Chatan.Core;

namespace Chatan.Rules
{
    /// <summary>The current player rolls the two dice.</summary>
    /// <remarks>Placeholder until Teresa's version lands in <c>feature/build-actions</c>; keep the constructor signature.</remarks>
    public sealed class RollDiceAction : IGameAction
    {
        /// <summary>Creates the action.</summary>
        public RollDiceAction(int playerId)
        {
            PlayerId = playerId;
        }

        /// <inheritdoc/>
        public int PlayerId { get; }

        /// <inheritdoc/>
        public override string ToString() => $"RollDice(P{PlayerId})";
    }
}
