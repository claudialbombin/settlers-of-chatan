using Chatan.Core;
using Chatan.UI;
using UnityEngine;

namespace Chatan.Dev
{
    /// <summary>
    /// Fake turn manager for <c>Sandbox_Claudia</c> only: answers the HUD buttons with a
    /// made-up state so the HUD can be tried without the real rules.
    /// </summary>
    /// <remarks>
    /// Rolling gives the human one grain and moves to the main phase. Ending the turn
    /// passes it to the next bot and, after <see cref="botTurnSeconds"/>, back to the human.
    /// </remarks>
    public class HudSandboxDriver : MonoBehaviour
    {
        [Tooltip("Seconds a bot turn lasts before the turn comes back to the human.")]
        [SerializeField] private float botTurnSeconds = 1.5f;

        private readonly System.Random _rng = new System.Random();
        private GameState _state;

        private void Start()
        {
            _state = FakeGameStateFactory.Create();
            UIEvents.RollDiceRequested += Roll;
            UIEvents.EndTurnRequested += EndTurn;
            GameEvents.RaiseStateChanged(_state);
        }

        private void OnDestroy()
        {
            UIEvents.RollDiceRequested -= Roll;
            UIEvents.EndTurnRequested -= EndTurn;
        }

        private void Roll()
        {
            _state.LastDiceA = _rng.Next(1, 7);
            _state.LastDiceB = _rng.Next(1, 7);

            // Resources is a struct field of a class, so it can be changed in place.
            _state.Players[0].Resources.Grain += 1;
            _state.Phase = GamePhase.Main;

            GameEvents.RaiseDiceRolled(_state.LastDiceA, _state.LastDiceB);
            GameEvents.RaiseStateChanged(_state);
        }

        private void EndTurn()
        {
            _state.CurrentPlayerIndex = (_state.CurrentPlayerIndex + 1) % _state.Players.Count;
            _state.Phase = GamePhase.Roll;
            _state.TurnNumber++;
            GameEvents.RaiseStateChanged(_state);

            if (!_state.CurrentPlayer.IsHuman)
            {
                Invoke(nameof(BackToHuman), botTurnSeconds);
            }
        }

        private void BackToHuman()
        {
            _state.CurrentPlayerIndex = 0;
            _state.Phase = GamePhase.Roll;
            GameEvents.RaiseStateChanged(_state);
        }
    }
}
