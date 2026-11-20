using UnityEngine;

namespace Chatan.UI
{
    /// <summary>
    /// Debug switches, kept in an asset so they can be toggled in the Inspector without
    /// touching code.
    /// </summary>
    /// <remarks>
    /// Both switches reveal secret information. Keep them off in the asset committed to the
    /// repository, or the demo would show the bots' hidden data.
    /// </remarks>
    [CreateAssetMenu(menuName = "Chatan/Debug Settings", fileName = "DebugSettings")]
    public class DebugSettings : ScriptableObject
    {
        /// <summary>Shows the real personality of each bot on its info card.</summary>
        [Tooltip("Shows the real personality of each bot on its info card.")]
        public bool revealPersonalities;

        /// <summary>Shows the exact resource cards of each bot (to debug trades).</summary>
        [Tooltip("Shows the exact resource cards of each bot (to debug trades).")]
        public bool revealBotHands;

        /// <summary>Whether personalities may be shown. False when <paramref name="settings"/> is null.</summary>
        public static bool RevealPersonalities(DebugSettings settings) => settings != null && settings.revealPersonalities;

        /// <summary>Whether bot hands may be shown. False when <paramref name="settings"/> is null.</summary>
        public static bool RevealBotHands(DebugSettings settings) => settings != null && settings.revealBotHands;
    }
}
