using Chatan.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chatan.UI
{
    /// <summary>Whose turn it is (name and colour) and what to do in the current phase.</summary>
    public class TurnIndicatorView : MonoBehaviour
    {
        [SerializeField] private TMP_Text playerName;
        [SerializeField] private TMP_Text phaseText;
        [SerializeField] private Image colorStripe;

        /// <summary>Shows the current player and phase.</summary>
        public void Show(PlayerState current, GamePhase phase)
        {
            playerName.text = HudText.Turn(current);
            phaseText.text = HudText.Phase(phase);
            if (colorStripe != null)
            {
                colorStripe.color = current.Color;
            }
        }
    }
}
