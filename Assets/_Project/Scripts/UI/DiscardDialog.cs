using System.Threading;
using System.Threading.Tasks;
using Chatan.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chatan.UI
{
    /// <summary>
    /// After a 7: lets the human pick which cards to give back with −/+ buttons per
    /// resource. "Confirmar" only works at exactly the required amount.
    /// </summary>
    /// <remarks>The counting rules live in <see cref="DiscardSelection"/>.</remarks>
    public class DiscardDialog : MonoBehaviour
    {
        [SerializeField] private TMP_Text title;

        [Tooltip("In ResourceBundle.AllTypes order: wood, brick, wool, grain, ore.")]
        [SerializeField] private TMP_Text[] amountTexts = new TMP_Text[5];
        [SerializeField] private Button[] minusButtons = new Button[5];
        [SerializeField] private Button[] plusButtons = new Button[5];
        [SerializeField] private Button confirmButton;

        private DiscardSelection _selection;
        private TaskCompletionSource<ResourceBundle> _tcs;
        private CancellationTokenRegistration _cancel;

        /// <summary>Whether the dialog is waiting for an answer.</summary>
        public bool IsOpen => _tcs != null && !_tcs.Task.IsCompleted;

        private void Awake()
        {
            for (int i = 0; i < ResourceBundle.AllTypes.Count; i++)
            {
                ResourceType type = ResourceBundle.AllTypes[i];
                minusButtons[i].onClick.AddListener(() => Change(type, -1));
                plusButtons[i].onClick.AddListener(() => Change(type, +1));
            }

            confirmButton.onClick.AddListener(Confirm);
            if (_tcs == null)
            {
                gameObject.SetActive(false);
            }
        }

        /// <summary>Opens the dialog and waits until exactly <paramref name="required"/> cards are confirmed.</summary>
        public Task<ResourceBundle> Ask(ResourceBundle hand, int required, CancellationToken ct)
        {
            _tcs?.TrySetCanceled();
            _cancel.Dispose();

            _selection = new DiscardSelection(hand, required);
            _tcs = new TaskCompletionSource<ResourceBundle>();
            _cancel = ct.Register(() => Close(cancelled: true));

            gameObject.SetActive(true);
            Redraw();
            return _tcs.Task;
        }

        private void Change(ResourceType type, int delta)
        {
            if (_selection != null && _selection.TryChange(type, delta))
            {
                Redraw();
            }
        }

        private void Confirm()
        {
            if (_selection == null || !_selection.CanConfirm)
            {
                return;
            }

            ResourceBundle chosen = _selection.Chosen;
            Close(cancelled: false);
            _tcs.TrySetResult(chosen);
        }

        private void Close(bool cancelled)
        {
            _cancel.Dispose();
            if (this != null)
            {
                gameObject.SetActive(false);
            }

            if (cancelled)
            {
                _tcs?.TrySetCanceled();
            }
        }

        private void Redraw()
        {
            title.text = _selection.Title;
            for (int i = 0; i < ResourceBundle.AllTypes.Count; i++)
            {
                ResourceType type = ResourceBundle.AllTypes[i];
                amountTexts[i].text = _selection.AmountText(type);
                minusButtons[i].interactable = _selection.Chosen[type] > 0;
                plusButtons[i].interactable = _selection.Chosen[type] < _selection.Hand[type]
                    && _selection.Chosen.Total < _selection.Required;
            }

            confirmButton.interactable = _selection.CanConfirm;
        }
    }
}
