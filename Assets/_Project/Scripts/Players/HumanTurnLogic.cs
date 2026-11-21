using System;
using System.Collections.Generic;
using System.Linq;
using Chatan.Board;
using Chatan.Core;
using Chatan.Rules;
using Chatan.UI;

namespace Chatan.Players
{
    /// <summary>
    /// The decisions behind <see cref="HumanPlayerController"/> that do not need Unity:
    /// which pieces to light up, which action a click produces, which mode a phase forces.
    /// </summary>
    public static class HumanTurnLogic
    {
        /// <summary>Road pieces each player starts with.</summary>
        public const int MaxRoads = 15;

        /// <summary>Kind of board piece that a mode clicks on.</summary>
        public static BoardTargetKind KindFor(PlacementMode mode) => mode switch
        {
            PlacementMode.Road => BoardTargetKind.Edge,
            PlacementMode.Robber => BoardTargetKind.Tile,
            _ => BoardTargetKind.Vertex
        };

        /// <summary>Ids the rules accept for <paramref name="mode"/> right now. Empty for <see cref="PlacementMode.None"/>.</summary>
        public static HashSet<int> ValidIds(PlacementMode mode, IRulesQuery rules, GameState state, int playerId)
        {
            if (rules == null || state == null)
            {
                return new HashSet<int>();
            }

            IEnumerable<int> ids = mode switch
            {
                PlacementMode.Road => rules.ValidRoadEdges(state, playerId),
                PlacementMode.Settlement => rules.ValidSettlementVertices(state, playerId),
                PlacementMode.City => rules.ValidCityVertices(state, playerId),
                PlacementMode.Robber => rules.ValidRobberTiles(state),
                _ => Enumerable.Empty<int>()
            };
            return new HashSet<int>(ids ?? Enumerable.Empty<int>());
        }

        /// <summary>
        /// Action produced by clicking <paramref name="targetId"/> in a build mode. The
        /// robber is not covered here because it may need to ask for a victim first.
        /// </summary>
        /// <exception cref="ArgumentException">For <see cref="PlacementMode.None"/> or <see cref="PlacementMode.Robber"/>.</exception>
        public static IGameAction BuildActionFor(PlacementMode mode, int playerId, int targetId) => mode switch
        {
            PlacementMode.Road => new BuildRoadAction(playerId, targetId),
            PlacementMode.Settlement => new BuildSettlementAction(playerId, targetId),
            PlacementMode.City => new BuildCityAction(playerId, targetId),
            _ => throw new ArgumentException($"{mode} does not build anything.", nameof(mode))
        };

        /// <summary>
        /// Mode a phase imposes on the human: the robber after a 7, and settlement-then-road
        /// during setup. <see cref="PlacementMode.None"/> when they are free to choose.
        /// </summary>
        public static PlacementMode ForcedMode(GameState state, IRulesQuery rules, PlayerState me)
        {
            if (state == null)
            {
                return PlacementMode.None;
            }

            if (state.Phase == GamePhase.MoveRobber)
            {
                return PlacementMode.Robber;
            }

            if (rules != null && rules.IsFreePlacement(state))
            {
                return NeedsSettlementInSetup(me) ? PlacementMode.Settlement : PlacementMode.Road;
            }

            return PlacementMode.None;
        }

        /// <summary>Whether right-click may leave the current mode (not while a phase forces it).</summary>
        public static bool CanCancel(GameState state, IRulesQuery rules, PlayerState me) =>
            ForcedMode(state, rules, me) == PlacementMode.None;

        /// <summary>
        /// In setup each settlement is followed by its road, so a settlement is due when
        /// the player has placed as many settlements as roads.
        /// </summary>
        /// <remarks>Deduced from the pieces left; switch to the rules' answer if Teresa exposes one.</remarks>
        public static bool NeedsSettlementInSetup(PlayerState me)
        {
            int settlements = BuildChoices.MaxSettlements - me.SettlementsLeft;
            int roads = MaxRoads - me.RoadsLeft;
            return settlements <= roads;
        }

        /// <summary>
        /// Victim for a robber move when no question is needed: <see cref="MoveRobberAction.NoVictim"/>
        /// with nobody around, the only one if there is one. Null when the human must choose.
        /// </summary>
        public static int? AutomaticVictim(IReadOnlyList<int> victims)
        {
            if (victims == null || victims.Count == 0)
            {
                return MoveRobberAction.NoVictim;
            }

            return victims.Count == 1 ? victims[0] : (int?)null;
        }
    }
}
