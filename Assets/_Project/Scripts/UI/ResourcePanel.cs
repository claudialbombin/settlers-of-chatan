using Chatan.Core;
using UnityEngine;

namespace Chatan.UI
{
    /// <summary>The five resource boxes of the human player.</summary>
    public class ResourcePanel : MonoBehaviour
    {
        [Tooltip("One box per resource, in any order.")]
        [SerializeField] private ResourceSlotView[] slots;

        /// <summary>Shows the amounts of a resource bundle.</summary>
        public void Show(ResourceBundle resources)
        {
            foreach (ResourceSlotView slot in slots)
            {
                if (slot != null)
                {
                    slot.SetAmount(resources[slot.Type]);
                }
            }
        }
    }
}
