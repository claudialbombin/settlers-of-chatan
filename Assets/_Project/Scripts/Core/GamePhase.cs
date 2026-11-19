namespace Chatan.Core
{
    /// <summary>
    /// The phase the game is currently in. It decides which actions are legal
    /// and which UI controls are enabled.
    /// </summary>
    /// <remarks>
    /// Keep the order stable: Unity serializes enums by their numeric value.
    /// </remarks>
    public enum GamePhase
    {
        /// <summary>First placement round: one settlement and one road per player, in seat order.</summary>
        SetupRound1,

        /// <summary>Second placement round, in reverse seat order. Grants starting resources.</summary>
        SetupRound2,

        /// <summary>The current player must roll the dice before doing anything else.</summary>
        Roll,

        /// <summary>The current player may build, trade and play development cards.</summary>
        Main,

        /// <summary>A 7 was rolled: every player holding more than 7 cards discards half of them.</summary>
        Discard,

        /// <summary>The current player must move the robber and may steal one card.</summary>
        MoveRobber,

        /// <summary>A player reached the victory threshold. No more actions are accepted.</summary>
        GameOver
    }
}
