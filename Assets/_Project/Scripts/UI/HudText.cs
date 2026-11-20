using Chatan.Core;

namespace Chatan.UI
{
    /// <summary>
    /// Texts and button rules of the HUD, kept apart from the views so they can be unit
    /// tested without building any UI.
    /// </summary>
    public static class HudText
    {
        /// <summary>Text shown when a die has not been rolled yet.</summary>
        public const string NoDie = "-";

        /// <summary>Spanish instruction for each phase of the turn.</summary>
        public static string Phase(GamePhase phase) => phase switch
        {
            GamePhase.SetupRound1 => "Colocación inicial (1ª ronda)",
            GamePhase.SetupRound2 => "Colocación inicial (2ª ronda)",
            GamePhase.Roll => "Tira los dados",
            GamePhase.Main => "Construye, comercia o termina el turno",
            GamePhase.Discard => "Descarta la mitad de tus cartas",
            GamePhase.MoveRobber => "Mueve el ladrón",
            GamePhase.GameOver => "Fin de la partida",
            _ => phase.ToString()
        };

        /// <summary>"Tu turno" for the human, "Turno de &lt;name&gt;" for a bot.</summary>
        public static string Turn(PlayerState current) =>
            current.IsHuman ? "Tu turno" : $"Turno de {current.Name}";

        /// <summary>
        /// Victory points of the human. Hidden points (victory point cards) are included
        /// because they are the human's own: "5 PV" or "5 PV (1 oculto)".
        /// </summary>
        public static string Points(int visible, int hidden)
        {
            int total = visible + hidden;
            if (hidden <= 0)
            {
                return $"{total} PV";
            }

            return hidden == 1 ? $"{total} PV (1 oculto)" : $"{total} PV ({hidden} ocultos)";
        }

        /// <summary>Value of one die, or <see cref="NoDie"/> before the first roll.</summary>
        public static string Die(int value) => value > 0 ? value.ToString() : NoDie;

        /// <summary>Sum of the dice, or an empty text before the first roll.</summary>
        public static string DiceTotal(int dieA, int dieB) =>
            dieA > 0 && dieB > 0 ? (dieA + dieB).ToString() : string.Empty;

        /// <summary>Whether the roll is a 7, which moves the robber (shown highlighted).</summary>
        public static bool IsRobberRoll(int dieA, int dieB) => dieA > 0 && dieB > 0 && dieA + dieB == 7;

        /// <summary>"Tirar dados" is enabled only on the human's turn, in the roll phase.</summary>
        public static bool CanRoll(GameState state, int humanPlayerId) =>
            IsTurnOf(state, humanPlayerId) && state.Phase == GamePhase.Roll;

        /// <summary>"Terminar turno" is enabled only on the human's turn, in the main phase.</summary>
        public static bool CanEndTurn(GameState state, int humanPlayerId) =>
            IsTurnOf(state, humanPlayerId) && state.Phase == GamePhase.Main;

        private static bool IsTurnOf(GameState state, int playerId) =>
            state != null
            && state.CurrentPlayerIndex >= 0
            && state.CurrentPlayerIndex < state.Players.Count
            && state.CurrentPlayer.Id == playerId;
    }
}
