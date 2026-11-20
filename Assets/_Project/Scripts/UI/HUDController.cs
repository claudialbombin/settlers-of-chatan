using Chatan.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chatan.UI
{
    /// <summary>
    /// The fixed on-screen interface: resources and points of the human, dice, turn
    /// indicator and the "Tirar dados" / "Terminar turno" buttons.
    /// </summary>
    /// <remarks>
    /// It has no game logic and no <c>Update</c>: it listens to <see cref="GameEvents"/>
    /// and repaints, and its buttons only raise <see cref="UIEvents"/>.
    /// </remarks>
    public class HUDController : MonoBehaviour
    {
        [Tooltip("Player id of the human.")]
        [SerializeField] private int humanPlayerId;

        [Header("Views")]
        [SerializeField] private ResourcePanel resourcePanel;
        [SerializeField] private DiceView diceView;
        [SerializeField] private TurnIndicatorView turnIndicator;
        [SerializeField] private TMP_Text pointsText;

        [Header("Buttons")]
        [SerializeField] private Button rollButton;
        [SerializeField] private Button endTurnButton;

        private void OnEnable()
        {
            GameEvents.StateChanged += OnStateChanged;
            GameEvents.DiceRolled += OnDiceRolled;
            rollButton.onClick.AddListener(UIEvents.RequestRollDice);
            endTurnButton.onClick.AddListener(UIEvents.RequestEndTurn);

            // Nothing to show until the first state arrives.
            rollButton.interactable = false;
            endTurnButton.interactable = false;
        }

        // Always unsubscribe: static events would otherwise keep calling a destroyed HUD
        // (MissingReferenceException when leaving Play mode or changing scene).
        private void OnDisable()
        {
            GameEvents.StateChanged -= OnStateChanged;
            GameEvents.DiceRolled -= OnDiceRolled;
            rollButton.onClick.RemoveListener(UIEvents.RequestRollDice);
            endTurnButton.onClick.RemoveListener(UIEvents.RequestEndTurn);
        }

        private void OnStateChanged(GameState state)
        {
            if (state == null || state.Players.Count == 0)
            {
                return;
            }

            PlayerState me = state.Players.Find(p => p.Id == humanPlayerId);
            if (me != null)
            {
                resourcePanel.Show(me.Resources);
                pointsText.text = HudText.Points(me.VisiblePoints, me.HiddenPoints);
            }

            turnIndicator.Show(state.CurrentPlayer, state.Phase);
            diceView.Show(state.LastDiceA, state.LastDiceB);

            rollButton.interactable = HudText.CanRoll(state, humanPlayerId);
            endTurnButton.interactable = HudText.CanEndTurn(state, humanPlayerId);
        }

        private void OnDiceRolled(int dieA, int dieB) => diceView.Show(dieA, dieB);
    }
}
