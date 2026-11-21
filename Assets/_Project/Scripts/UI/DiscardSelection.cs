using Chatan.Core;

namespace Chatan.UI
{
    /// <summary>
    /// The choice being made in the discard dialog: how many cards of each resource the
    /// player gives back after a 7.
    /// </summary>
    /// <remarks>
    /// Plain class so the counting rules can be tested without Unity: never more than you
    /// hold of a type, never more than the required total, and confirm only at exactly
    /// the required amount.
    /// </remarks>
    public class DiscardSelection
    {
        private ResourceBundle _chosen;

        /// <summary>Starts an empty selection.</summary>
        /// <param name="hand">Cards the player holds.</param>
        /// <param name="required">Cards they must discard (half their hand, rounded down).</param>
        public DiscardSelection(ResourceBundle hand, int required)
        {
            Hand = hand;
            Required = required;
        }

        /// <summary>Cards the player holds.</summary>
        public ResourceBundle Hand { get; }

        /// <summary>Cards that must be discarded.</summary>
        public int Required { get; }

        /// <summary>Cards picked so far.</summary>
        public ResourceBundle Chosen => _chosen;

        /// <summary>True when exactly <see cref="Required"/> cards are picked.</summary>
        public bool CanConfirm => _chosen.Total == Required;

        /// <summary>Dialog title, e.g. "Ha salido un 7: descarta 4 cartas (llevas 2)".</summary>
        public string Title => $"Ha salido un 7: descarta {Required} cartas (llevas {_chosen.Total})";

        /// <summary>Counter for one resource, e.g. "1 / 3".</summary>
        public string AmountText(ResourceType type) => $"{_chosen[type]} / {Hand[type]}";

        /// <summary>
        /// Adds (+1) or removes (−1) one card of <paramref name="type"/>. Returns false and
        /// changes nothing if the step would break a limit.
        /// </summary>
        public bool TryChange(ResourceType type, int delta)
        {
            int next = _chosen[type] + delta;
            if (next < 0 || next > Hand[type])
            {
                return false;
            }

            if (delta > 0 && _chosen.Total + delta > Required)
            {
                return false;
            }

            _chosen[type] = next;
            return true;
        }
    }
}
