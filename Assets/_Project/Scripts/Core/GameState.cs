using System;
using System.Collections.Generic;

namespace Chatan.Core
{
    /// <summary>
    /// A snapshot of the whole match: players, whose turn it is, the phase and the
    /// last dice roll.
    /// </summary>
    /// <remarks>
    /// Like <see cref="PlayerState"/>, it is a plain data container. The rules change
    /// it and then announce the change through <see cref="GameEvents.RaiseStateChanged"/>;
    /// everybody else only reads it.
    /// </remarks>
    [Serializable]
    public class GameState
    {
        /// <summary>Players in seat order. The index in this list matches <see cref="PlayerState.Id"/>.</summary>
        public List<PlayerState> Players = new List<PlayerState>();

        /// <summary>Index in <see cref="Players"/> of the player whose turn it is.</summary>
        public int CurrentPlayerIndex;

        /// <summary>Current phase of the turn.</summary>
        public GamePhase Phase;

        /// <summary>Value of the first die in the last roll, or 0 if nobody has rolled yet this turn.</summary>
        public int LastDiceA;

        /// <summary>Value of the second die in the last roll, or 0 if nobody has rolled yet this turn.</summary>
        public int LastDiceB;

        /// <summary>Turn counter, starting at the first turn after setup.</summary>
        public int TurnNumber;

        /// <summary>The player whose turn it is.</summary>
        public PlayerState CurrentPlayer => Players[CurrentPlayerIndex];

        /// <summary>
        /// Returns a deep copy: every <see cref="PlayerState"/> is cloned too, so bots
        /// can simulate moves on the copy without touching the real game.
        /// </summary>
        /// <remarks>If you add a new reference-type field (e.g. the board), copy it here as well.</remarks>
        public GameState Clone()
        {
            var copy = (GameState)MemberwiseClone();
            copy.Players = new List<PlayerState>(Players.Count);

            foreach (PlayerState player in Players)
            {
                copy.Players.Add(player.Clone());
            }

            return copy;
        }
    }
}
