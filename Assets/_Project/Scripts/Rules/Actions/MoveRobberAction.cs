using Chatan.Core;

namespace Chatan.Rules
{
    /// <summary>The player moves the robber to a tile and, optionally, steals one card.</summary>
    /// <remarks>Placeholder until Teresa's version lands in <c>feature/build-actions</c>; keep the constructor signature.</remarks>
    public sealed class MoveRobberAction : IGameAction
    {
        /// <summary>Creates the action.</summary>
        public MoveRobberAction(int playerId, int tileId, int victimPlayerId)
        {
            PlayerId = playerId;
            TileId = tileId;
            VictimPlayerId = victimPlayerId;
        }

        /// <inheritdoc/>
        public int PlayerId { get; }

        /// <summary>Value of <see cref="VictimPlayerId"/> when nobody can be robbed.</summary>
        public const int NoVictim = -1;

        /// <summary>Id of the tile the robber moves to.</summary>
        public int TileId { get; }

        /// <summary>Player who loses a random card, or <see cref="NoVictim"/>.</summary>
        public int VictimPlayerId { get; }

        /// <inheritdoc/>
        public override string ToString() => $"MoveRobber(P{PlayerId}, tile {TileId}, victim {VictimPlayerId})";
    }
}
