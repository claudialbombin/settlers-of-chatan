using Chatan.Core;

namespace Chatan.Negotiation
{
    /// <summary>
    /// Carries out an accepted trade on the real game state.
    /// </summary>
    /// <remarks>
    /// The negotiation code decides <i>whether</i> to trade; the rules decide <i>how</i>
    /// resources move. This interface is the only bridge between them, so negotiation
    /// never edits <see cref="GameState"/> directly (Dependency Inversion). The real
    /// implementation lives in Rules/ (player trade action); a fake one in Dev/ lets the
    /// chat be tested without the rules.
    /// </remarks>
    public interface ITradeExecutor
    {
        /// <summary>
        /// Checks that the trade can happen right now: both players own what they promise
        /// and the current phase allows trading.
        /// </summary>
        /// <param name="offer">The offer to check.</param>
        /// <param name="state">The current game state. It is not modified.</param>
        /// <returns>True if <see cref="Execute"/> would succeed.</returns>
        bool CanExecute(TradeOffer offer, GameState state);

        /// <summary>
        /// Moves the resources between both players and raises
        /// <see cref="GameEvents.StateChanged"/>. Call <see cref="CanExecute"/> first.
        /// </summary>
        /// <param name="offer">The accepted offer.</param>
        /// <param name="state">The game state to modify.</param>
        void Execute(TradeOffer offer, GameState state);
    }
}
