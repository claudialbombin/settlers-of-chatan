using System.Threading;
using System.Threading.Tasks;
using Chatan.Core;

namespace Chatan.Players
{
    /// <summary>
    /// Whoever decides the moves of one seat: the human through the UI, or a bot.
    /// </summary>
    /// <remarks>
    /// The turn manager does not care which one it is talking to (Liskov): it awaits the
    /// next action, applies it and asks again until it receives an <c>EndTurnAction</c>.
    /// </remarks>
    public interface IPlayerController
    {
        /// <summary>Seat this controller plays.</summary>
        int PlayerId { get; }

        /// <summary>Waits for the next action of this player. Called in a loop during their turn.</summary>
        Task<IGameAction> ChooseActionAsync(GameState state, CancellationToken ct);

        /// <summary>
        /// After a 7 with more than 7 cards in hand: returns exactly <paramref name="count"/>
        /// cards to discard.
        /// </summary>
        Task<ResourceBundle> ChooseDiscardAsync(GameState state, int count, CancellationToken ct);
    }
}
