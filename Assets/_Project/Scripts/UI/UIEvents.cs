using System;

namespace Chatan.UI
{
    /// <summary>
    /// What the interface asks the game to do. The other half of <see cref="Chatan.Core.GameEvents"/>.
    /// </summary>
    /// <remarks>
    /// The HUD never calls the turn manager directly: its buttons raise these events and
    /// whoever runs the game (the TurnManager, or <c>HudSandboxDriver</c> in the sandbox)
    /// subscribes to them. This keeps the UI independent of the rules code. Subscribers
    /// must unsubscribe when they are disabled or destroyed.
    /// </remarks>
    public static class UIEvents
    {
        /// <summary>The human pressed "Tirar dados".</summary>
        public static event Action RollDiceRequested;

        /// <summary>The human pressed "Terminar turno".</summary>
        public static event Action EndTurnRequested;

        /// <summary>Raises <see cref="RollDiceRequested"/>.</summary>
        public static void RequestRollDice() => RollDiceRequested?.Invoke();

        /// <summary>Raises <see cref="EndTurnRequested"/>.</summary>
        public static void RequestEndTurn() => EndTurnRequested?.Invoke();

        /// <summary>
        /// Removes every subscriber. Use it in tests and when the game scene is unloaded,
        /// so listeners from a previous game do not survive.
        /// </summary>
        public static void ClearAll()
        {
            RollDiceRequested = null;
            EndTurnRequested = null;
        }
    }
}
