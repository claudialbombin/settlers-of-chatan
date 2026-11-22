using TMPro;
using UnityEngine;

namespace Chatan.UI
{
    /// <summary>
    /// The two dice of the last roll and their sum. A 7 is highlighted because it moves
    /// the robber.
    /// </summary>
    /// <remarks>
    /// It can later be swapped for 3D dice or sprites (F8) keeping <see cref="Show"/>.
    /// </remarks>
    public class DiceView : MonoBehaviour
    {
        [SerializeField] private TMP_Text dieA;
        [SerializeField] private TMP_Text dieB;
        [SerializeField] private TMP_Text total;

        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color robberColor = new Color(0.9f, 0.2f, 0.2f);

        /// <summary>Shows a roll. Use 0 for both dice before the first roll.</summary>
        public void Show(int a, int b)
        {
            dieA.text = HudText.Die(a);
            dieB.text = HudText.Die(b);
            total.text = HudText.DiceTotal(a, b);
            total.color = HudText.IsRobberRoll(a, b) ? robberColor : normalColor;
        }
    }
}
