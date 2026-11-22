using Chatan.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Chatan.UI
{
    /// <summary>
    /// One button of the build menu. Greys itself out when it cannot be used and shows
    /// the cost while the mouse is over it.
    /// </summary>
    public class BuildButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private BuildKind kind;
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;

        [Tooltip("Panel shown on hover.")]
        [SerializeField] private GameObject costTooltip;
        [SerializeField] private TMP_Text costText;

        /// <summary>What this button buys.</summary>
        public BuildKind Kind => kind;

        /// <summary>The Unity button, for the menu to listen to.</summary>
        public Button Button => button;

        private void Awake()
        {
            if (label != null)
            {
                label.text = BuildChoices.Label(kind);
            }

            if (costText != null)
            {
                costText.text = BuildChoices.CostText(kind);
            }

            if (costTooltip != null)
            {
                costTooltip.SetActive(false);
            }
        }

        /// <summary>Enables the button only if <see cref="BuildChoices.CanUse"/> says so.</summary>
        public void Refresh(GameState state, int humanPlayerId) =>
            button.interactable = BuildChoices.CanUse(kind, state, humanPlayerId);

        /// <inheritdoc/>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (costTooltip != null)
            {
                costTooltip.SetActive(true);
            }
        }

        /// <inheritdoc/>
        public void OnPointerExit(PointerEventData eventData)
        {
            if (costTooltip != null)
            {
                costTooltip.SetActive(false);
            }
        }
    }
}
