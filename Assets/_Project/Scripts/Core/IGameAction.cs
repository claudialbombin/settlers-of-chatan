namespace Chatan.Core
{
    /// <summary>
    /// Something a player wants to do on their turn: roll, build, buy a card, move the
    /// robber, end the turn...
    /// </summary>
    /// <remarks>
    /// Actions are plain requests. Whoever produces them (the human UI or a bot) does not
    /// decide whether they are legal: the rules validate every action again before
    /// applying it.
    /// </remarks>
    public interface IGameAction
    {
        /// <summary>Seat of the player who asks for the action.</summary>
        int PlayerId { get; }
    }
}
