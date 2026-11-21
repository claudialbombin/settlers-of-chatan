using System.Collections.Generic;
using System.Linq;
using Chatan.Core;
using Chatan.Rules;

namespace Chatan.Dev
{
    /// <summary>
    /// Stand-in for Teresa's <c>RuleValidator</c> in the sandbox: every free spot is valid,
    /// with no distance or connection rules.
    /// </summary>
    /// <remarks>
    /// It remembers what has been built (the sandbox driver tells it) so a vertex cannot
    /// get two settlements and only your own settlements can become cities. Anybody near
    /// a tile can be robbed: it always offers players 1 and 2, minus the thief.
    /// </remarks>
    public class FakeRulesQuery : IRulesQuery
    {
        private readonly HashSet<int> _roads = new HashSet<int>();
        private readonly Dictionary<int, int> _settlementOwner = new Dictionary<int, int>();
        private readonly HashSet<int> _cities = new HashSet<int>();

        /// <summary>Creates fake rules for a board with these many pieces of each kind.</summary>
        public FakeRulesQuery(int vertexCount, int edgeCount, int tileCount)
        {
            VertexCount = vertexCount;
            EdgeCount = edgeCount;
            TileCount = tileCount;
        }

        /// <summary>Number of vertices (ids 0..VertexCount-1).</summary>
        public int VertexCount { get; }

        /// <summary>Number of edges (ids 0..EdgeCount-1).</summary>
        public int EdgeCount { get; }

        /// <summary>Number of tiles (ids 0..TileCount-1).</summary>
        public int TileCount { get; }

        /// <summary>Tile the robber is on.</summary>
        public int RobberTile { get; private set; }

        /// <summary>What <see cref="IsFreePlacement"/> answers. The sandbox turns it on for the setup scenario.</summary>
        public bool FreePlacement { get; set; }

        /// <summary>Records a road so the edge is no longer offered.</summary>
        public void AddRoad(int edgeId) => _roads.Add(edgeId);

        /// <summary>Records a settlement of <paramref name="playerId"/>.</summary>
        public void AddSettlement(int playerId, int vertexId) => _settlementOwner[vertexId] = playerId;

        /// <summary>Records that the settlement on <paramref name="vertexId"/> is now a city.</summary>
        public void UpgradeToCity(int vertexId) => _cities.Add(vertexId);

        /// <summary>Records the new robber position.</summary>
        public void MoveRobber(int tileId) => RobberTile = tileId;

        /// <inheritdoc/>
        public IEnumerable<int> ValidRoadEdges(GameState state, int playerId) =>
            Enumerable.Range(0, EdgeCount).Where(e => !_roads.Contains(e));

        /// <inheritdoc/>
        public IEnumerable<int> ValidSettlementVertices(GameState state, int playerId) =>
            Enumerable.Range(0, VertexCount).Where(v => !_settlementOwner.ContainsKey(v));

        /// <inheritdoc/>
        public IEnumerable<int> ValidCityVertices(GameState state, int playerId) =>
            _settlementOwner.Where(kv => kv.Value == playerId && !_cities.Contains(kv.Key)).Select(kv => kv.Key).OrderBy(v => v);

        /// <inheritdoc/>
        public IEnumerable<int> ValidRobberTiles(GameState state) =>
            Enumerable.Range(0, TileCount).Where(t => t != RobberTile);

        /// <inheritdoc/>
        public IEnumerable<int> RobbablePlayersAt(GameState state, int tileId, int thiefId) =>
            new[] { 1, 2 }.Where(p => p != thiefId);

        /// <inheritdoc/>
        public bool IsFreePlacement(GameState state) => FreePlacement;
    }
}
