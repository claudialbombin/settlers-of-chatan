using System;
using Chatan.Core;
using UnityEngine;

namespace Chatan.AI
{
    /// <summary>
    /// The personality sheet of a bot, stored as a Unity asset and edited with sliders
    /// in the Inspector. Each of the ten personalities is one asset of this class.
    /// </summary>
    /// <remarks>
    /// <para>Three systems read it:</para>
    /// <list type="bullet">
    /// <item><description>The bot brain (action planner, robber strategy): what to build, whom to block.</description></item>
    /// <item><description>The trade evaluator: whether to accept, reject or counter a trade.</description></item>
    /// <item><description>The response generator: how the bot talks in the chat.</description></item>
    /// </list>
    /// <para>
    /// It is pure data plus two small lookups; behaviour lives in those systems (Single
    /// Responsibility). Fields are public, the usual Unity convention for data assets, so
    /// the Inspector shows them and tests can set them directly.
    /// </para>
    /// <para>
    /// Do not rename a field once assets exist: Unity would lose the saved value. If a
    /// rename is unavoidable, add <c>[FormerlySerializedAs("oldName")]</c> above the new field.
    /// </para>
    /// </remarks>
    [CreateAssetMenu(menuName = "Chatan/Personality Profile", fileName = "Personality_")]
    public class PersonalityProfile : ScriptableObject
    {
        /// <summary>Multiplier that leaves a value unchanged (used as the default weight and preference).</summary>
        public const float NeutralMultiplier = 1f;

        /// <summary>Stable identifier of this personality.</summary>
        [Header("Identity")]
        public PersonalityId id;

        /// <summary>Name shown in the UI.</summary>
        [Tooltip("Name shown in the UI.")]
        public string displayName = "Sin nombre";

        /// <summary>One or two sentences describing the personality, for docs and debugging.</summary>
        [Tooltip("One or two sentences describing the personality, for docs and debugging.")]
        [TextArea(2, 4)]
        public string shortDescription;

        /// <summary>Optional portrait for the player info card.</summary>
        [Tooltip("Optional portrait for the player info card.")]
        public Sprite portrait;

        /// <summary>Character prompt for the language model (Data/Prompts/&lt;id&gt;.txt).</summary>
        [Tooltip("Character prompt for the language model (Data/Prompts/<id>.txt).")]
        public TextAsset promptFile;

        /// <summary>Tendency to block or attack the leader (robber, cutting roads).</summary>
        [Header("Negotiation traits (0 = none, 1 = maximum)")]
        [Tooltip("Tendency to block or attack the leader (robber, cutting roads).")]
        [Range(0f, 1f)]
        public float aggression = 0.5f;

        /// <summary>Margin demanded in a trade: 0 accepts fair deals, 1 asks for much more than it gives.</summary>
        [Tooltip("Margin demanded in a trade: 0 accepts fair deals, 1 asks for much more than it gives.")]
        [Range(0f, 1f)]
        public float greed = 0.5f;

        /// <summary>General willingness to trade. Lowers the acceptance threshold.</summary>
        [Tooltip("General willingness to trade. Lowers the acceptance threshold.")]
        [Range(0f, 1f)]
        public float cooperation = 0.5f;

        /// <summary>How much past behaviour of the other player (affinity) matters.</summary>
        [Tooltip("How much past behaviour of the other player (affinity) matters.")]
        [Range(0f, 1f)]
        public float grudge = 0.3f;

        /// <summary>Probability of lying about its own resources in the chat.</summary>
        [Tooltip("Probability of lying about its own resources in the chat.")]
        [Range(0f, 1f)]
        public float bluffing = 0.1f;

        /// <summary>Willingness to hold more than 7 cards and to make risky plays.</summary>
        [Tooltip("Willingness to hold more than 7 cards and to make risky plays.")]
        [Range(0f, 1f)]
        public float riskTolerance = 0.5f;

        /// <summary>Noise in its decisions. 0 always picks the best move.</summary>
        [Tooltip("Noise in its decisions. 0 always picks the best move.")]
        [Range(0f, 1f)]
        public float randomness = 0.1f;

        /// <summary>Priority multiplier for building roads.</summary>
        [Header("Building priorities (multipliers, 1 = normal)")]
        [Tooltip("Priority multiplier for building roads.")]
        [Range(0f, 3f)]
        public float roadWeight = NeutralMultiplier;

        /// <summary>Priority multiplier for building settlements.</summary>
        [Tooltip("Priority multiplier for building settlements.")]
        [Range(0f, 3f)]
        public float settlementWeight = NeutralMultiplier;

        /// <summary>Priority multiplier for upgrading to cities.</summary>
        [Tooltip("Priority multiplier for upgrading to cities.")]
        [Range(0f, 3f)]
        public float cityWeight = NeutralMultiplier;

        /// <summary>Priority multiplier for buying development cards.</summary>
        [Tooltip("Priority multiplier for buying development cards.")]
        [Range(0f, 3f)]
        public float devCardWeight = NeutralMultiplier;

        /// <summary>Priority multiplier for settling on ports.</summary>
        [Tooltip("Priority multiplier for settling on ports.")]
        [Range(0f, 3f)]
        public float portWeight = NeutralMultiplier;

        /// <summary>Multiplier applied to the value of wood.</summary>
        [Header("Resource preference (multiplies its value, 1 = normal)")]
        [Range(0.5f, 2f)]
        public float woodPreference = NeutralMultiplier;

        /// <summary>Multiplier applied to the value of brick.</summary>
        [Range(0.5f, 2f)]
        public float brickPreference = NeutralMultiplier;

        /// <summary>Multiplier applied to the value of wool.</summary>
        [Range(0.5f, 2f)]
        public float woolPreference = NeutralMultiplier;

        /// <summary>Multiplier applied to the value of grain.</summary>
        [Range(0.5f, 2f)]
        public float grainPreference = NeutralMultiplier;

        /// <summary>Multiplier applied to the value of ore.</summary>
        [Range(0.5f, 2f)]
        public float orePreference = NeutralMultiplier;

        /// <summary>Maximum haggling rounds before the bot ends the conversation.</summary>
        [Header("Chat")]
        [Tooltip("Maximum haggling rounds before the bot ends the conversation.")]
        [Range(1, 6)]
        public int maxNegotiationRounds = 3;

        /// <summary>Language model temperature for this voice (higher = more creative).</summary>
        [Tooltip("Language model temperature for this voice (higher = more creative).")]
        [Range(0f, 1.2f)]
        public float chatTemperature = 0.8f;

        /// <summary>
        /// Character prompt for the language model: the content of <see cref="promptFile"/>,
        /// or a generic Spanish prompt built from the name and description if it is missing.
        /// </summary>
        public string PromptText => promptFile != null
            ? promptFile.text
            : $"Eres {displayName}, un jugador de Catan. {shortDescription}".TrimEnd();

        /// <summary>
        /// Preference multiplier for one resource. Used by the resource valuator and the
        /// trade evaluator so they can loop over resources instead of naming each field.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">If <paramref name="type"/> is not a defined value.</exception>
        public float PreferenceFor(ResourceType type) => type switch
        {
            ResourceType.Wood => woodPreference,
            ResourceType.Brick => brickPreference,
            ResourceType.Wool => woolPreference,
            ResourceType.Grain => grainPreference,
            ResourceType.Ore => orePreference,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown resource type.")
        };

#if UNITY_EDITOR
        /// <summary>
        /// Called by the editor whenever a value changes in the Inspector. Warns about
        /// common set-up mistakes; it never changes any value.
        /// </summary>
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(displayName))
            {
                Debug.LogWarning($"{name}: displayName is empty.", this);
            }

            if (promptFile == null)
            {
                Debug.LogWarning($"{name}: promptFile is missing, a generic prompt will be used.", this);
            }
        }
#endif
    }
}
