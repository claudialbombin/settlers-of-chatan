using System;
using Chatan.AI;
using Chatan.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Chatan.UI
{
    /// <summary>
    /// Card with the public information of one bot: name and colour, visible points,
    /// number of cards, development cards, knights, badges, affinity and, in debug mode
    /// only, its personality.
    /// </summary>
    /// <remarks>
    /// Clicking it raises <see cref="Clicked"/>, which the chat panel will use to open the
    /// conversation with that bot. The background image must be a raycast target to
    /// receive the click.
    /// </remarks>
    public class PlayerInfoCard : MonoBehaviour, IPointerClickHandler
    {
        [Header("References")]
        [SerializeField] private Image background;
        [SerializeField] private Image colorStripe;
        [SerializeField] private Image portrait;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text pointsText;
        [SerializeField] private TMP_Text cardsText;
        [SerializeField] private TMP_Text devCardsText;
        [SerializeField] private TMP_Text knightsText;
        [SerializeField] private GameObject longestRoadBadge;
        [SerializeField] private GameObject largestArmyBadge;
        [SerializeField] private TMP_Text personalityText;

        [Tooltip("Image with Image Type = Filled, Fill Method = Horizontal.")]
        [SerializeField] private Image affinityBar;

        [Tooltip("Shown when the bot holds more than 7 cards (it would discard on a 7).")]
        [SerializeField] private GameObject tooManyCardsWarning;

        [Header("Style")]
        [SerializeField] private Color normalBackground = new Color(0f, 0f, 0f, 0.55f);
        [SerializeField] private Color activeBackground = new Color(1f, 0.85f, 0.2f, 0.75f);
        [SerializeField] private Color hatredColor = new Color(0.85f, 0.2f, 0.2f);
        [SerializeField] private Color friendshipColor = new Color(0.25f, 0.75f, 0.3f);
        [SerializeField] private Sprite unknownPortrait;

        /// <summary>Player id of the bot shown by this card.</summary>
        public int PlayerId { get; private set; }

        /// <summary>Whether the card is highlighted because it is this bot's turn.</summary>
        public bool IsHighlighted { get; private set; }

        /// <summary>Raised with <see cref="PlayerId"/> when the card is clicked.</summary>
        public event Action<int> Clicked;

        /// <summary>Links the card to a player. Call it once, right after creating the card.</summary>
        public void Bind(int playerId) => PlayerId = playerId;

        /// <summary>Repaints the card.</summary>
        /// <param name="player">State of the bot.</param>
        /// <param name="isTheirTurn">Highlights the card.</param>
        /// <param name="personality">Personality of the bot, or null if not assigned yet.</param>
        /// <param name="debug">Debug switches; null means nothing secret is shown.</param>
        public void Show(PlayerState player, bool isTheirTurn, PersonalityProfile personality, DebugSettings debug)
        {
            nameText.text = player.Name;
            colorStripe.color = player.Color;
            pointsText.text = PlayerCardText.Points(player);
            cardsText.text = PlayerCardText.Cards(player.Resources, DebugSettings.RevealBotHands(debug));
            devCardsText.text = PlayerCardText.DevCards(player);
            knightsText.text = PlayerCardText.Knights(player);

            SetActive(tooManyCardsWarning, PlayerCardText.HasTooManyCards(player.Resources));
            SetActive(longestRoadBadge, player.HasLongestRoad);
            SetActive(largestArmyBadge, player.HasLargestArmy);

            bool reveal = DebugSettings.RevealPersonalities(debug) && personality != null;
            personalityText.text = PlayerCardText.Personality(personality, reveal);
            if (portrait != null)
            {
                portrait.sprite = reveal && personality.portrait != null ? personality.portrait : unknownPortrait;
            }

            IsHighlighted = isTheirTurn;
            background.color = isTheirTurn ? activeBackground : normalBackground;
        }

        /// <summary>
        /// Shows the affinity with the human, from -1 (hatred) through 0 (neutral) to
        /// 1 (friendship). RelationshipMemory will call it.
        /// </summary>
        public void ShowAffinity(float affinity)
        {
            if (affinityBar == null)
            {
                return;
            }

            float fill = PlayerCardText.AffinityFill(affinity);
            affinityBar.fillAmount = fill;
            affinityBar.color = Color.Lerp(hatredColor, friendshipColor, fill);
        }

        /// <inheritdoc />
        public void OnPointerClick(PointerEventData eventData) => Clicked?.Invoke(PlayerId);

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }
    }
}
