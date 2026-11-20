using System.Collections;
using Chatan.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chatan.UI
{
    /// <summary>One box of the resource panel: icon, name and amount of one resource.</summary>
    public class ResourceSlotView : MonoBehaviour
    {
        private const float PulseScale = 1.2f;
        private const float PulseSeconds = 0.25f;

        [SerializeField] private ResourceType type;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text amountText;

        [Tooltip("Optional. Shows the Spanish name of the resource.")]
        [SerializeField] private TMP_Text nameText;

        [Tooltip("Briefly enlarges the box when the amount changes.")]
        [SerializeField] private bool pulseOnChange = true;

        private int _lastAmount = -1;
        private Coroutine _pulse;

        /// <summary>Resource shown by this box.</summary>
        public ResourceType Type => type;

        /// <summary>Icon image, so the editor tools can assign the sprite.</summary>
        public Image Icon => icon;

        /// <summary>Shows a new amount, pulsing if it changed since the last call.</summary>
        public void SetAmount(int amount)
        {
            amountText.text = amount.ToString();

            bool changed = _lastAmount >= 0 && amount != _lastAmount;
            if (changed && pulseOnChange && isActiveAndEnabled)
            {
                if (_pulse != null)
                {
                    StopCoroutine(_pulse);
                }

                _pulse = StartCoroutine(Pulse());
            }

            _lastAmount = amount;
        }

        private void Awake()
        {
            if (nameText != null)
            {
                nameText.text = ResourceNames.ToSpanish(type);
            }
        }

        private void OnDisable()
        {
            _pulse = null;
            transform.localScale = Vector3.one;
        }

        private IEnumerator Pulse()
        {
            for (float t = 0f; t < PulseSeconds; t += Time.unscaledDeltaTime)
            {
                // Up and back down: sin goes 0 → 1 → 0 over the pulse.
                float k = Mathf.Sin(t / PulseSeconds * Mathf.PI);
                transform.localScale = Vector3.one * Mathf.Lerp(1f, PulseScale, k);
                yield return null;
            }

            transform.localScale = Vector3.one;
            _pulse = null;
        }
    }
}
