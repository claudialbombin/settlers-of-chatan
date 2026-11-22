using Chatan.AI;
using Chatan.Core;

namespace Chatan.UI
{
    /// <summary>
    /// Texts of a bot's info card, kept apart from the view so they can be unit tested.
    /// </summary>
    /// <remarks>
    /// The card shows only public information: visible points (never hidden ones), the
    /// number of resource cards (not which ones) and "?" instead of the personality,
    /// unless <see cref="DebugSettings"/> says otherwise.
    /// </remarks>
    public static class PlayerCardText
    {
        /// <summary>Text shown instead of a secret personality.</summary>
        public const string UnknownPersonality = "?";

        /// <summary>With more cards than this, a 7 makes the player discard half.</summary>
        public const int SafeHandSize = 7;

        /// <summary>Visible victory points only: hidden points are secret.</summary>
        public static string Points(PlayerState player) => $"{player.VisiblePoints} PV";

        /// <summary>"7 cartas", or the exact hand when <paramref name="revealHand"/> is true.</summary>
        public static string Cards(ResourceBundle resources, bool revealHand)
        {
            if (revealHand)
            {
                return resources.ToString();
            }

            int total = resources.Total;
            return total == 1 ? "1 carta" : $"{total} cartas";
        }

        /// <summary>Number of development cards in hand: "1 desarrollo".</summary>
        public static string DevCards(PlayerState player) => $"{player.DevCards?.Count ?? 0} desarrollo";

        /// <summary>Knights played: "1 caballero", "2 caballeros".</summary>
        public static string Knights(PlayerState player) =>
            player.KnightsPlayed == 1 ? "1 caballero" : $"{player.KnightsPlayed} caballeros";

        /// <summary>Whether the hand is at risk of discarding when a 7 is rolled.</summary>
        public static bool HasTooManyCards(ResourceBundle resources) => resources.Total > SafeHandSize;

        /// <summary>The personality name if it may be revealed, otherwise <see cref="UnknownPersonality"/>.</summary>
        public static string Personality(PersonalityProfile personality, bool reveal) =>
            reveal && personality != null ? personality.displayName : UnknownPersonality;

        /// <summary>
        /// Fill of the affinity bar (0..1) for an affinity from -1 (hatred) through
        /// 0 (neutral) to 1 (friendship). Values outside the range are clamped.
        /// </summary>
        public static float AffinityFill(float affinity)
        {
            float clamped = affinity < -1f ? -1f : affinity > 1f ? 1f : affinity;
            return (clamped + 1f) / 2f;
        }
    }
}
