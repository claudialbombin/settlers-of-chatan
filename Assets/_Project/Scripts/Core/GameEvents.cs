using System;

namespace Chatan.Core
{
    /// <summary>
    /// Game-wide notice board. The rules publish what happened; the UI, the bots and the
    /// chat subscribe and react.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Publishers and subscribers never reference each other, only this class, so the
    /// UI can be built and tested without the real rules (Dependency Inversion).
    /// Nobody needs to poll the game state every frame.
    /// </para>
    /// <para>
    /// Subscribe in <c>OnEnable</c> and unsubscribe in <c>OnDisable</c>. Because the
    /// events are static they outlive scenes: call <see cref="ClearAll"/> when a new
    /// match starts and in test set-up so stale listeners do not leak between runs.
    /// </para>
    /// </remarks>
    public static class GameEvents
    {
        /// <summary>The game state changed in any way. Receives the updated state.</summary>
        public static event Action<GameState> StateChanged;

        /// <summary>The dice were rolled. Receives (die A, die B).</summary>
        public static event Action<int, int> DiceRolled;

        /// <summary>A new turn started. Receives the player whose turn it is.</summary>
        public static event Action<PlayerState> TurnStarted;

        /// <summary>A player stole a card with the robber. Receives (thief id, victim id).</summary>
        public static event Action<int, int> PlayerRobbed;

        /// <summary>Notifies <see cref="StateChanged"/> subscribers.</summary>
        public static void RaiseStateChanged(GameState state) => StateChanged?.Invoke(state);

        /// <summary>Notifies <see cref="DiceRolled"/> subscribers.</summary>
        public static void RaiseDiceRolled(int dieA, int dieB) => DiceRolled?.Invoke(dieA, dieB);

        /// <summary>Notifies <see cref="TurnStarted"/> subscribers.</summary>
        public static void RaiseTurnStarted(PlayerState player) => TurnStarted?.Invoke(player);

        /// <summary>Notifies <see cref="PlayerRobbed"/> subscribers.</summary>
        public static void RaisePlayerRobbed(int thiefId, int victimId) => PlayerRobbed?.Invoke(thiefId, victimId);

        /// <summary>
        /// Removes every subscriber. Call it when a match starts and in test set-up.
        /// </summary>
        /// <remarks>When you add a new event above, remember to clear it here too.</remarks>
        public static void ClearAll()
        {
            StateChanged = null;
            DiceRolled = null;
            TurnStarted = null;
            PlayerRobbed = null;
        }
    }
}
