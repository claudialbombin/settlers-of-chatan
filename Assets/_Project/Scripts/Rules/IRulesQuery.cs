using System.Collections.Generic;
using Chatan.Core;

namespace Chatan.Rules
{
    /// <summary>
    /// Questions the interface asks the rules before letting the human click: which
    /// positions are legal right now.
    /// </summary>
    /// <remarks>
    /// The UI only highlights what this returns; the rules still validate the resulting
    /// action when they apply it. Implemented by Teresa's <c>RuleValidator</c>; the sandbox
    /// uses <c>FakeRulesQuery</c> until then.
    /// </remarks>
    public interface IRulesQuery
    {
        /// <summary>Edges where <paramref name="playerId"/> may build a road.</summary>
        IEnumerable<int> ValidRoadEdges(GameState state, int playerId);

        /// <summary>Vertices where <paramref name="playerId"/> may build a settlement.</summary>
        IEnumerable<int> ValidSettlementVertices(GameState state, int playerId);

        /// <summary>Vertices with a settlement of <paramref name="playerId"/> that can become a city.</summary>
        IEnumerable<int> ValidCityVertices(GameState state, int playerId);

        /// <summary>Tiles the robber may move to: every tile except the one it is on.</summary>
        IEnumerable<int> ValidRobberTiles(GameState state);

        /// <summary>Players with a building next to <paramref name="tileId"/> who can be robbed by <paramref name="thiefId"/>.</summary>
        IEnumerable<int> RobbablePlayersAt(GameState state, int tileId, int thiefId);

        /// <summary>True during the setup rounds, when building costs nothing.</summary>
        bool IsFreePlacement(GameState state);
    }
}
