using System;
using System.Collections.Generic;
using UnityEngine;

namespace Chatan.Core
{
    /// <summary>
    /// Everything the game knows about one player at a given moment.
    /// </summary>
    /// <remarks>
    /// Plain data container: it holds no game logic. The rules (Rules/) are the only
    /// code that changes it; the UI and the bots only read it. Fields are public so
    /// Unity can serialize them and they can be inspected while debugging.
    /// </remarks>
    [Serializable]
    public class PlayerState
    {
        /// <summary>Seat index. 0 is the human player, 1..3 are the bots.</summary>
        public int Id;

        /// <summary>Name shown in the UI and used by the chat.</summary>
        public string Name;

        /// <summary>True for the human seat, false for bots.</summary>
        public bool IsHuman;

        /// <summary>Resource cards currently in the player's hand.</summary>
        public ResourceBundle Resources;

        /// <summary>Points every player can see (buildings, longest road, largest army).</summary>
        public int VisiblePoints;

        /// <summary>Points from victory-point cards. Only the owner sees them until the end.</summary>
        public int HiddenPoints;

        /// <summary>Road pieces still available to place.</summary>
        public int RoadsLeft;

        /// <summary>Settlement pieces still available to place.</summary>
        public int SettlementsLeft;

        /// <summary>City pieces still available to place.</summary>
        public int CitiesLeft;

        /// <summary>Development cards held and not yet played.</summary>
        public List<DevCardType> DevCards = new List<DevCardType>();

        /// <summary>Knight cards already played (used for the largest army).</summary>
        public int KnightsPlayed;

        /// <summary>True while this player holds the "longest road" bonus.</summary>
        public bool HasLongestRoad;

        /// <summary>True while this player holds the "largest army" bonus.</summary>
        public bool HasLargestArmy;

        /// <summary>Colour of the player's pieces and UI accents.</summary>
        public Color Color = Color.white;

        /// <summary>Real score: visible plus hidden points. Used to check victory.</summary>
        public int TotalPoints => VisiblePoints + HiddenPoints;

        /// <summary>
        /// Returns an independent copy. Value fields are copied by
        /// <see cref="object.MemberwiseClone"/>; the card list is duplicated so that
        /// changing the copy never changes the original.
        /// </summary>
        /// <remarks>If you add a new reference-type field, copy it here as well.</remarks>
        public PlayerState Clone()
        {
            var copy = (PlayerState)MemberwiseClone();
            copy.DevCards = DevCards == null ? new List<DevCardType>() : new List<DevCardType>(DevCards);
            return copy;
        }
    }
}
