using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Chatan.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chatan.UI
{
    /// <summary>
    /// After moving the robber next to several players: one button per possible victim,
    /// with their name and colour.
    /// </summary>
    public class RobberVictimDialog : MonoBehaviour
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private Transform optionsContainer;

        [Tooltip("Inactive button copied once per victim.")]
        [SerializeField] private Button optionTemplate;

        private readonly List<Button> _options = new List<Button>();
        private TaskCompletionSource<int> _tcs;
        private CancellationTokenRegistration _cancel;

        /// <summary>Whether the dialog is waiting for an answer.</summary>
        public bool IsOpen => _tcs != null && !_tcs.Task.IsCompleted;

        private void Awake()
        {
            if (optionTemplate != null)
            {
                optionTemplate.gameObject.SetActive(false);
            }

            if (_tcs == null)
            {
                gameObject.SetActive(false);
            }
        }

        /// <summary>Shows one button per victim and waits for the click. Returns the chosen player id.</summary>
        public Task<int> Ask(IReadOnlyList<int> victims, GameState state, CancellationToken ct = default)
        {
            _tcs?.TrySetCanceled();
            _cancel.Dispose();
            ClearOptions();

            _tcs = new TaskCompletionSource<int>();
            _cancel = ct.Register(() => { Close(); _tcs.TrySetCanceled(); });
            title.text = "¿A quién le robas una carta?";

            foreach (int victimId in victims)
            {
                PlayerState victim = state.Players[victimId];
                Button option = Instantiate(optionTemplate, optionsContainer);
                option.name = $"Victim_P{victimId}";
                option.gameObject.SetActive(true);
                option.GetComponentInChildren<TMP_Text>().text = victim.Name;
                if (option.targetGraphic != null)
                {
                    option.targetGraphic.color = victim.Color;
                }

                int id = victimId;
                option.onClick.AddListener(() => Pick(id));
                _options.Add(option);
            }

            gameObject.SetActive(true);
            return _tcs.Task;
        }

        private void Pick(int victimId)
        {
            Close();
            _tcs?.TrySetResult(victimId);
        }

        private void Close()
        {
            _cancel.Dispose();
            if (this != null)
            {
                ClearOptions();
                gameObject.SetActive(false);
            }
        }

        private void ClearOptions()
        {
            foreach (Button option in _options)
            {
                if (option != null)
                {
                    Destroy(option.gameObject);
                }
            }

            _options.Clear();
        }
    }
}
