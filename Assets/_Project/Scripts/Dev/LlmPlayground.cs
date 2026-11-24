using System;
using System.Collections.Generic;
using System.Threading;
using Chatan.AI;
using Chatan.Core;
using Chatan.LLM;
using Chatan.Negotiation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Chatan.Dev
{
    /// <summary>
    /// Test bench for the prompts: type a message, choose Parse or Reply and see what the
    /// model answers and how long it took, without playing a match.
    /// </summary>
    /// <remarks>
    /// Lives in <c>Sandbox_LLM</c> (built by Chatan → LLM → Build LLM Playground). Reply
    /// uses a sample context (the human offers 2 grain for 1 ore) with the decision chosen in
    /// the second dropdown and the personality dragged into <see cref="personality"/>. The
    /// model is created again on every send, so <c>useMock</c> can be switched in Play mode.
    /// </remarks>
    public class LlmPlayground : MonoBehaviour
    {
        /// <summary>Options of the mode dropdown, in order.</summary>
        public static readonly IReadOnlyList<string> ModeOptions = new[] { "Parse", "Reply" };

        /// <summary>Options of the decision dropdown, in order.</summary>
        public static readonly IReadOnlyList<string> DecisionOptions = new[] { "accept", "reject", "counter", "sin oferta" };

        [SerializeField] private LlmSettings settings;
        [SerializeField] private PersonalityProfile personality;
        [SerializeField] private TMP_InputField input;
        [SerializeField] private TMP_Dropdown mode;
        [SerializeField] private TMP_Dropdown decision;
        [SerializeField] private Button send;
        [SerializeField] private TMP_Text output;

        private CancellationTokenSource _cts;

        private void Awake()
        {
            Fill(mode, ModeOptions);
            Fill(decision, DecisionOptions);
            if (send != null)
            {
                send.onClick.AddListener(OnSend);
            }
        }

        private void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
        }

        /// <summary>The request the playground would send for <paramref name="text"/>.</summary>
        /// <param name="text">The human's message.</param>
        /// <param name="reply">True for Reply, false for Parse.</param>
        /// <param name="decisionIndex">Index in <see cref="DecisionOptions"/>.</param>
        /// <param name="profile">The bot's personality, or null.</param>
        public static LlmRequest BuildRequest(string text, bool reply, int decisionIndex, PersonalityProfile profile)
        {
            if (!reply)
            {
                return PromptBuilder.BuildParseRequest(text, null);
            }

            var offer = new TradeOffer
            {
                FromPlayerId = FakeGameStateFactory.HumanId,
                ToPlayerId = 1,
                Give = ResourceBundle.Of(ResourceType.Grain, 2),
                Receive = ResourceBundle.Of(ResourceType.Ore, 1)
            };
            var context = new ReplyContext
            {
                Personality = profile,
                HumanText = text,
                HumanIntent = IntentType.Propose,
                OfferOnTable = offer,
                RelationshipSummary = NegotiationSession.NeutralRelationship,
                History = Array.Empty<NegotiationTurn>(),
                BotId = 1
            };

            switch (decisionIndex)
            {
                case 0:
                    context.Decision = new TradeDecision { Type = TradeDecisionType.Accept, Reason = TradeReason.GoodDeal };
                    break;
                case 1:
                    context.Decision = new TradeDecision { Type = TradeDecisionType.Reject, Reason = TradeReason.TooLittle };
                    break;
                case 2:
                    context.Decision = new TradeDecision { Type = TradeDecisionType.Counter, Reason = TradeReason.AlmostThere };
                    context.CounterOffer = new TradeOffer
                    {
                        FromPlayerId = offer.FromPlayerId,
                        ToPlayerId = offer.ToPlayerId,
                        Give = ResourceBundle.Of(ResourceType.Grain, 3),
                        Receive = offer.Receive,
                        Status = TradeStatus.Countered
                    };
                    break;
                default:
                    context.HumanIntent = IntentType.SmallTalk;
                    context.OfferOnTable = null;
                    break;
            }

            return PromptBuilder.BuildReplyRequest(context);
        }

        // async void is fine for a UI event handler: everything is caught inside.
        private async void OnSend()
        {
            string text = input != null ? input.text : string.Empty;
            if (string.IsNullOrWhiteSpace(text))
            {
                Show("Escribe algo primero.");
                return;
            }

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();

            bool reply = mode != null && mode.value == 1;
            LlmRequest request = BuildRequest(text, reply, decision != null ? decision.value : 0, personality);
            ILanguageModel model = LanguageModelFactory.Create(settings);

            send.interactable = false;
            Show($"{model.DisplayName} pensando…");
            try
            {
                LlmResponse response = await model.CompleteAsync(request, _cts.Token);
                if (this == null)
                {
                    return;   // the scene was closed while waiting
                }

                Show(response.Success
                    ? $"<b>{model.DisplayName}</b> ({response.LatencySeconds:0.0} s)\n{response.Text}"
                    : $"<color=#ff8080><b>Error</b> ({response.LatencySeconds:0.0} s)\n{response.Error}</color>");
                Debug.Log($"[LLM Playground] {(reply ? "Reply" : "Parse")} \"{text}\" → {response}");
            }
            catch (OperationCanceledException)
            {
                // A newer send replaced this one.
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Show($"<color=#ff8080>{e.Message}</color>");
            }
            finally
            {
                if (this != null && send != null)
                {
                    send.interactable = true;
                }
            }
        }

        private void Show(string text)
        {
            if (output != null)
            {
                output.text = text;
            }
        }

        private static void Fill(TMP_Dropdown dropdown, IReadOnlyList<string> options)
        {
            if (dropdown == null)
            {
                return;
            }

            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string>(options));
        }
    }
}
