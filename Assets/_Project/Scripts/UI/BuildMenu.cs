using Chatan.Core;
using UnityEngine;

namespace Chatan.UI
{
    /// <summary>
    /// The bar with the four build buttons. Clicking one only announces the intention
    /// through <see cref="UIEvents"/>; the human controller does the rest.
    /// </summary>
    public class BuildMenu : MonoBehaviour
    {
        [SerializeField] private int humanPlayerId;
        [SerializeField] private BuildButton[] buttons = new BuildButton[0];

        private void OnEnable()
        {
            GameEvents.StateChanged += Refresh;
            foreach (BuildButton b in buttons)
            {
                BuildKind kind = b.Kind;
                b.Button.onClick.AddListener(() => Choose(kind));
                b.Button.interactable = false;
            }
        }

        private void OnDisable()
        {
            GameEvents.StateChanged -= Refresh;
            foreach (BuildButton b in buttons)
            {
                b.Button.onClick.RemoveAllListeners();
            }
        }

        /// <summary>What a click on <paramref name="kind"/> asks for.</summary>
        public static void Choose(BuildKind kind)
        {
            switch (kind)
            {
                case BuildKind.Road: UIEvents.SetPlacementMode(PlacementMode.Road); break;
                case BuildKind.Settlement: UIEvents.SetPlacementMode(PlacementMode.Settlement); break;
                case BuildKind.City: UIEvents.SetPlacementMode(PlacementMode.City); break;
                case BuildKind.DevCard: UIEvents.RequestBuyDevCard(); break;
            }
        }

        /// <summary>Enables or greys out each button for the new state.</summary>
        public void Refresh(GameState state)
        {
            foreach (BuildButton b in buttons)
            {
                b.Refresh(state, humanPlayerId);
            }
        }
    }
}
