using System.Collections.Generic;
using Chatan.Core;
using UnityEngine;

namespace Chatan.Dev
{
    /// <summary>
    /// Made-up game state with one human and three bots, to try the UI before the real
    /// rules exist. Reused by the sandbox drivers of the HUD, info cards and chat.
    /// </summary>
    /// <remarks>
    /// Player 0 is the human: 2 wood, 1 brick, 3 wool, 2 grain, 0 ore, 2 visible points and
    /// 1 hidden point. It is their turn, in the roll phase of turn 1.
    /// </remarks>
    public static class FakeGameStateFactory
    {
        /// <summary>Player id of the human in the fake state.</summary>
        public const int HumanId = 0;

        /// <summary>Creates a new fake state. Each call returns an independent object.</summary>
        public static GameState Create()
        {
            var state = new GameState
            {
                Players = new List<PlayerState>
                {
                    MakePlayer(HumanId, "Tú", true, new Color(0.85f, 0.2f, 0.2f), new ResourceBundle(2, 1, 3, 2, 0)),
                    MakePlayer(1, "Bot Azul", false, new Color(0.2f, 0.4f, 0.9f), new ResourceBundle(1, 2, 0, 1, 3)),
                    MakePlayer(2, "Bot Verde", false, new Color(0.2f, 0.7f, 0.3f), new ResourceBundle(0, 0, 2, 4, 1)),
                    MakePlayer(3, "Bot Blanco", false, new Color(0.9f, 0.9f, 0.9f), new ResourceBundle(3, 3, 1, 0, 0)),
                },
                CurrentPlayerIndex = 0,
                Phase = GamePhase.Roll,
                TurnNumber = 1
            };

            state.Players[0].HiddenPoints = 1;
            state.Players[1].VisiblePoints = 3;
            return state;
        }

        private static PlayerState MakePlayer(int id, string name, bool human, Color color, ResourceBundle resources) =>
            new PlayerState
            {
                Id = id,
                Name = name,
                IsHuman = human,
                Color = color,
                Resources = resources,
                VisiblePoints = 2,
                RoadsLeft = 13,
                SettlementsLeft = 3,
                CitiesLeft = 4,
                DevCards = new List<DevCardType>()
            };
    }
}
