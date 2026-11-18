namespace Chatan.Core
{
    /// <summary>
    /// The kinds of development card a player can buy and hold.
    /// </summary>
    /// <remarks>
    /// Keep the order stable: Unity serializes enums by their numeric value.
    /// </remarks>
    public enum DevCardType
    {
        /// <summary>Moves the robber. Counts towards the largest army.</summary>
        Knight,

        /// <summary>Hidden victory point, only revealed to the owner until the game ends.</summary>
        VictoryPoint,

        /// <summary>Builds two roads for free.</summary>
        RoadBuilding,

        /// <summary>Takes any two resources from the bank.</summary>
        YearOfPlenty,

        /// <summary>Takes every card of one resource type from all other players.</summary>
        Monopoly
    }
}
