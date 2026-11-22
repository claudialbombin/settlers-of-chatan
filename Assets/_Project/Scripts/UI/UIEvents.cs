using System;

namespace Chatan.UI
{
    /// <summary>
    /// What the interface asks the game to do. The other half of <see cref="Chatan.Core.GameEvents"/>.
    /// </summary>
    /// <remarks>
    /// The HUD and the build menu never call the turn manager directly: their buttons raise these events and
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

        /// <summary>
        /// The human picked a build button (or cancelled with <see cref="PlacementMode.None"/>):
        /// the board should highlight where that piece can go.
        /// </summary>
        public static event Action<PlacementMode> PlacementModeChanged;

        /// <summary>The human pressed "Carta de desarrollo".</summary>
        public static event Action BuyDevCardRequested;

        /// <summary>Raises <see cref="RollDiceRequested"/>.</summary>
        public static void RequestRollDice() => RollDiceRequested?.Invoke();

        /// <summary>Raises <see cref="EndTurnRequested"/>.</summary>
        public static void RequestEndTurn() => EndTurnRequested?.Invoke();

        /// <summary>Raises <see cref="PlacementModeChanged"/>.</summary>
        public static void SetPlacementMode(PlacementMode mode) => PlacementModeChanged?.Invoke(mode);

        /// <summary>Raises <see cref="BuyDevCardRequested"/>.</summary>
        public static void RequestBuyDevCard() => BuyDevCardRequested?.Invoke();

        /// <summary>
        /// Removes every subscriber. Use it in tests and when the game scene is unloaded,
        /// so listeners from a previous game do not survive.
        /// </summary>
        public static void ClearAll()
        {
            RollDiceRequested = null;
            EndTurnRequested = null;
            PlacementModeChanged = null;
            BuyDevCardRequested = null;
        }
    }
}
