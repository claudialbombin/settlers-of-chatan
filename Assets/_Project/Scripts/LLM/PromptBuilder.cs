using System.Collections.Generic;
using System.Text;
using Chatan.Core;
using Chatan.Negotiation;

namespace Chatan.LLM
{
    /// <summary>
    /// The only place where prompts are written. It builds the messages for the two jobs of
    /// the language model: understanding the human (JSON) and answering with the voice of
    /// the bot's personality.
    /// </summary>
    /// <remarks>
    /// Texts are in Spanish; JSON keys in English. The reply prompt always contains a
    /// <c>decision: accept|reject|counter|confirmed|none</c> line, which
    /// <see cref="MockLanguageModel"/> reads too: if that format changes here, change it there.
    /// </remarks>
    public static class PromptBuilder
    {
        /// <summary>How many earlier chat messages go into a reply prompt.</summary>
        public const int HistoryMessages = 4;

        /// <summary>Token limit of a parse answer (a small JSON object).</summary>
        public const int ParseMaxTokens = 120;

        /// <summary>Token limit of a chat answer (one or two short sentences).</summary>
        public const int ReplyMaxTokens = 90;

        // ======================= 1. UNDERSTANDING THE HUMAN =======================

        /// <summary>System prompt of the intent parser.</summary>
        public const string ParseSystem =
@"Eres un analizador de mensajes de un juego de Catan. El jugador escribe a un bot para comerciar.
Devuelve SOLO un objeto JSON, sin texto antes ni después, con esta forma exacta:
{""intent"": ""propose|accept|reject|counter|ask_needs|smalltalk|unknown"",
 ""give"":    {""wood"":0,""brick"":0,""wool"":0,""grain"":0,""ore"":0},
 ""receive"": {""wood"":0,""brick"":0,""wool"":0,""grain"":0,""ore"":0}}

- ""give"" es lo que el JUGADOR ofrece. ""receive"" es lo que el JUGADOR pide al bot.
- Recursos: madera/leña=wood, ladrillo/arcilla=brick, lana/oveja=wool, trigo/cereal/grano=grain, mineral/piedra/roca=ore.
- Si no dice cantidad, es 1. ""un par"" = 2.
- ""acepto"", ""vale"", ""trato hecho"" = accept. ""no"", ""paso"", ""ni de broma"" = reject.
- Si cambia una oferta anterior del bot, es counter.
- ""¿qué necesitas?"" = ask_needs. Saludos o charla = smalltalk.

Ejemplos:
Jugador: te doy 2 trigo por 1 mineral
{""intent"":""propose"",""give"":{""wood"":0,""brick"":0,""wool"":0,""grain"":2,""ore"":0},""receive"":{""wood"":0,""brick"":0,""wool"":0,""grain"":0,""ore"":1}}
Jugador: vale, trato hecho
{""intent"":""accept"",""give"":{""wood"":0,""brick"":0,""wool"":0,""grain"":0,""ore"":0},""receive"":{""wood"":0,""brick"":0,""wool"":0,""grain"":0,""ore"":0}}
Jugador: necesito madera, te doy una oveja
{""intent"":""propose"",""give"":{""wood"":0,""brick"":0,""wool"":1,""grain"":0,""ore"":0},""receive"":{""wood"":1,""brick"":0,""wool"":0,""grain"":0,""ore"":0}}";

        /// <summary>Prefix of the human's message in the parse prompt.</summary>
        public const string PlayerPrefix = "Jugador: ";

        /// <summary>Builds the request that turns <paramref name="humanText"/> into intent JSON.</summary>
        /// <param name="humanText">What the human wrote.</param>
        /// <param name="lastBotOfferText">The bot's last offer in words (see <see cref="DescribeOffer"/>), or null.</param>
        public static LlmRequest BuildParseRequest(string humanText, string lastBotOfferText)
        {
            var request = new LlmRequest
            {
                Purpose = LlmPurpose.ParseIntent,
                ExpectJson = true,
                Temperature = 0f,
                MaxTokens = ParseMaxTokens
            };
            request.Messages.Add(LlmMessage.System(ParseSystem));
            if (!string.IsNullOrEmpty(lastBotOfferText))
            {
                request.Messages.Add(LlmMessage.User($"(Contexto: la última oferta del bot fue: {lastBotOfferText})"));
            }

            request.Messages.Add(LlmMessage.User(PlayerPrefix + humanText));
            return request;
        }

        // ======================= 2. ANSWERING WITH PERSONALITY =======================

        /// <summary>Style rules added after the personality's character prompt.</summary>
        public const string ReplyRules =
@"Reglas de estilo:
- Responde en español, en 1 o 2 frases cortas, como en un chat de partida.
- Mantén siempre tu personalidad.
- La DECISIÓN ya está tomada: exprésala, no la cambies. No aceptes si la decisión es rechazar ni al revés.
- Si hay contraoferta, menciónala con sus cantidades exactas.
- No inventes recursos ni cantidades que no aparezcan en los datos.
- No hables de reglas del juego ni de que eres una IA.";

        /// <summary>Extra rule for personalities that bluff.</summary>
        public const string BluffRule = "- Puedes exagerar o mentir sobre lo que tienes, pero no sobre la decisión.";

        /// <summary>Bluffing above which the bot may lie about its hand.</summary>
        public const float BluffThreshold = 0.5f;

        /// <summary>Temperature when the context has no personality.</summary>
        public const float DefaultReplyTemperature = 0.8f;

        /// <summary>Builds the request that writes the bot's answer for an already taken decision.</summary>
        public static LlmRequest BuildReplyRequest(ReplyContext context)
        {
            var request = new LlmRequest
            {
                Purpose = LlmPurpose.GenerateReply,
                ExpectJson = false,
                Temperature = context.Personality != null ? context.Personality.chatTemperature : DefaultReplyTemperature,
                MaxTokens = ReplyMaxTokens
            };

            string persona = context.Personality != null ? context.Personality.PromptText : string.Empty;
            if (string.IsNullOrWhiteSpace(persona))
            {
                persona = "Eres un jugador de Catan.";
            }

            bool mayBluff = context.Personality != null && context.Personality.bluffing > BluffThreshold;
            request.Messages.Add(LlmMessage.System(persona + "\n\n" + ReplyRules + (mayBluff ? "\n" + BluffRule : "")));

            // The last messages before this one give the answer some continuity. The last
            // turn of the history is the human's current message, which goes in the facts.
            if (context.History != null)
            {
                int end = context.History.Count;
                if (end > 0 && context.History[end - 1].Speaker == Speaker.Human)
                {
                    end--;
                }

                for (int i = System.Math.Max(0, end - HistoryMessages); i < end; i++)
                {
                    NegotiationTurn turn = context.History[i];
                    if (turn.Speaker == Speaker.Human)
                    {
                        request.Messages.Add(LlmMessage.User(turn.Text));
                    }
                    else if (turn.Speaker == Speaker.Bot)
                    {
                        request.Messages.Add(LlmMessage.Assistant(turn.Text));
                    }
                }
            }

            request.Messages.Add(LlmMessage.User(DescribeFacts(context)));
            return request;
        }

        /// <summary>The facts the model has to put into words, ending with the <c>decision:</c> line.</summary>
        public static string DescribeFacts(ReplyContext context)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"El jugador dice: \"{context.HumanText}\"");
            sb.AppendLine($"Lo que sientes por este jugador: {context.RelationshipSummary}.");

            if (context.Decision != null)
            {
                sb.AppendLine($"decision: {context.Decision.Type.ToString().ToLowerInvariant()}");
                sb.AppendLine(DecisionToSpanish(context.Decision.Type));
                sb.AppendLine($"motivo: {ReasonToSpanish(context.Decision.Reason)}");
                if (context.OfferOnTable != null)
                {
                    sb.AppendLine($"Te ofrece {Spanish(context.OfferOnTable.Give)} a cambio de {Spanish(context.OfferOnTable.Receive)}.");
                }

                if (context.CounterOffer != null)
                {
                    sb.AppendLine($"Tu contraoferta: que te dé {Spanish(context.CounterOffer.Give)} y tú le das {Spanish(context.CounterOffer.Receive)}.");
                }
            }
            else
            {
                sb.AppendLine(context.HumanIntent switch
                {
                    IntentType.AskNeeds => "decision: none — te pregunta qué necesitas; dilo de forma vaga, sin cantidades.",
                    IntentType.Accept when context.OfferOnTable != null => "decision: confirmed — el jugador acepta tu oferta; confírmalo.",
                    IntentType.Reject => "decision: none — el jugador rechaza tu oferta; reacciona según tu carácter.",
                    _ => "decision: none — no hay oferta concreta; responde y anímale a proponer algo."
                });
            }

            if (context.SessionClosing)
            {
                sb.AppendLine("Esta es tu última respuesta: corta la negociación por hoy.");
            }

            sb.Append("Escribe solo tu respuesta.");
            return sb.ToString();
        }

        /// <summary>A bot's offer in words for the parse context, e.g. "me das 2 trigos y te doy 1 mineral".</summary>
        public static string DescribeOffer(TradeOffer offer) =>
            offer == null ? null : $"me das {Spanish(offer.Give)} y te doy {Spanish(offer.Receive)}";

        /// <summary>A bundle in Spanish with plurals, e.g. "2 trigos, 1 mineral". "nada" when empty.</summary>
        public static string Spanish(ResourceBundle bundle)
        {
            var parts = new List<string>();

            void Add(int amount, string one, string many)
            {
                if (amount > 0)
                {
                    parts.Add($"{amount} {(amount == 1 ? one : many)}");
                }
            }

            Add(bundle.Wood, "madera", "maderas");
            Add(bundle.Brick, "ladrillo", "ladrillos");
            Add(bundle.Wool, "lana", "lanas");
            Add(bundle.Grain, "trigo", "trigos");
            Add(bundle.Ore, "mineral", "minerales");
            return parts.Count == 0 ? "nada" : string.Join(", ", parts);
        }

        /// <summary>
        /// The decision spelled out in Spanish. Small models sometimes ignore the English
        /// <c>decision:</c> marker and accept when they should reject, so it is said twice.
        /// </summary>
        public static string DecisionToSpanish(TradeDecisionType type) => type switch
        {
            TradeDecisionType.Accept => "Tu decisión: ACEPTAS el trato.",
            TradeDecisionType.Counter => "Tu decisión: NO aceptas así, pero propones tu contraoferta.",
            _ => "Tu decisión: RECHAZAS el trato. No aceptes nada."
        };

        /// <summary>The reason of a decision in words for the model.</summary>
        public static string ReasonToSpanish(TradeReason reason) => reason switch
        {
            TradeReason.GoodDeal => "te conviene",
            TradeReason.FriendlyDiscount => "no es gran cosa, pero te cae bien",
            TradeReason.AlmostThere => "le falta poco para convencerte",
            TradeReason.TooLittle => "te ofrece demasiado poco",
            TradeReason.DontHaveIt => "no tienes lo que te pide",
            TradeReason.YouDontHaveIt => "él no tiene lo que ofrece",
            TradeReason.HelpsTheLeader => "va ganando y no quieres ayudarle",
            TradeReason.Grudge => "no quieres tratar con él por lo que te hizo",
            _ => "la oferta no tiene sentido"
        };
    }
}
