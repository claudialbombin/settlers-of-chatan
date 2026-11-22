using System;
using Chatan.Core;

namespace Chatan.UI
{
    /// <summary>
    /// What each build button costs, says and needs, kept apart from the Unity components
    /// so it can be unit tested.
    /// </summary>
    /// <remarks>
    /// This only decides whether a button is <i>enabled</i>. Whether the move is legal
    /// (a free spot, a connected road...) is still up to the rules.
    /// </remarks>
    public static class BuildChoices
    {
        /// <summary>Resources the option costs.</summary>
        public static ResourceBundle Cost(BuildKind kind) => kind switch
        {
            BuildKind.Road => ResourceBundle.RoadCost,
            BuildKind.Settlement => ResourceBundle.SettlementCost,
            BuildKind.City => ResourceBundle.CityCost,
            BuildKind.DevCard => ResourceBundle.DevCardCost,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown build option.")
        };

        /// <summary>Button text, e.g. "Carretera".</summary>
        public static string Label(BuildKind kind) => kind switch
        {
            BuildKind.Road => "Carretera",
            BuildKind.Settlement => "Poblado",
            BuildKind.City => "Ciudad",
            BuildKind.DevCard => "Carta de desarrollo",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown build option.")
        };

        /// <summary>Tooltip text, e.g. "Coste: 1 madera, 1 ladrillo".</summary>
        public static string CostText(BuildKind kind) => $"Coste: {Cost(kind)}";

        /// <summary>
        /// True when the player still has a piece of that kind. A city also needs a
        /// settlement on the board to upgrade (5 settlement pieces minus those left).
        /// </summary>
        public static bool HasPiecesLeft(BuildKind kind, PlayerState player) => kind switch
        {
            BuildKind.Road => player.RoadsLeft > 0,
            BuildKind.Settlement => player.SettlementsLeft > 0,
            BuildKind.City => player.CitiesLeft > 0 && player.SettlementsLeft < MaxSettlements,
            _ => true
        };

        /// <summary>
        /// Whether the button for <paramref name="kind"/> should be clickable: it is the
        /// human's main phase, they have the piece and they can pay for it.
        /// </summary>
        public static bool CanUse(BuildKind kind, GameState state, int humanPlayerId)
        {
            if (state == null || humanPlayerId < 0 || humanPlayerId >= state.Players.Count)
            {
                return false;
            }

            PlayerState me = state.Players[humanPlayerId];
            return state.Phase == GamePhase.Main
                && state.CurrentPlayer.Id == humanPlayerId
                && HasPiecesLeft(kind, me)
                && me.Resources.Contains(Cost(kind));
        }

        /// <summary>Settlement pieces each player starts with.</summary>
        public const int MaxSettlements = 5;
    }
}
