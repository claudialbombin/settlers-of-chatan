using System;
using System.Collections.Generic;
using UnityEngine;

namespace Chatan.AI
{
    /// <summary>
    /// What one bot remembers about every other player: an affinity from −1 (enemy)
    /// through 0 (indifferent) to +1 (ally).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The personality's <see cref="PersonalityProfile.grudge"/> shapes it: a spiteful bot
    /// feels offences more, is harder to win over and forgets more slowly.
    /// </para>
    /// <para>
    /// Read by the trade evaluator, the chat (<see cref="DescribeFor"/>), the robber
    /// strategy and the bot info card (through <see cref="Changed"/>).
    /// </para>
    /// </remarks>
    public class RelationshipMemory
    {
        /// <summary>Grudge used when no personality is given.</summary>
        public const float DefaultGrudge = 0.3f;

        /// <summary>Values closer to 0 than this are rounded to 0 when decaying.</summary>
        public const float ForgetThreshold = 0.01f;

        private readonly PersonalityProfile _personality;
        private readonly Dictionary<int, float> _affinity = new Dictionary<int, float>();
        private readonly Dictionary<int, RelationshipEventType> _lastEvent = new Dictionary<int, RelationshipEventType>();
        private readonly List<string> _log = new List<string>();

        /// <summary>Creates an empty memory.</summary>
        /// <param name="ownerId">Seat of the bot that remembers.</param>
        /// <param name="personality">Its personality; null uses <see cref="DefaultGrudge"/>.</param>
        public RelationshipMemory(int ownerId, PersonalityProfile personality)
        {
            OwnerId = ownerId;
            _personality = personality;
        }

        /// <summary>The affinity with <c>otherId</c> changed. Receives (other player, new affinity).</summary>
        public event Action<int, float> Changed;

        /// <summary>Seat of the bot that remembers.</summary>
        public int OwnerId { get; }

        /// <summary>One line per recorded event, oldest first. For the debug log.</summary>
        public IReadOnlyList<string> History => _log;

        private float Grudge => _personality != null ? _personality.grudge : DefaultGrudge;

        /// <summary>Affinity with <paramref name="otherId"/>; 0 if nothing happened yet.</summary>
        public float Get(int otherId) => _affinity.TryGetValue(otherId, out float value) ? value : 0f;

        /// <summary>
        /// Applies an event caused by <paramref name="otherId"/>. Events about the owner itself
        /// are ignored. The result is clamped to [−1, 1].
        /// </summary>
        public void Record(int otherId, RelationshipEventType type)
        {
            if (otherId == OwnerId)
            {
                return;
            }

            float delta = ScaledDelta(type, Grudge);
            float next = Mathf.Clamp(Get(otherId) + delta, -1f, 1f);
            _affinity[otherId] = next;
            _lastEvent[otherId] = type;
            _log.Add($"{type} con J{otherId}: {delta:+0.00;-0.00} → {next:0.00}");
            Changed?.Invoke(otherId, next);
        }

        /// <summary>
        /// Moves every affinity a bit towards 0. Call it once per round, not once per turn,
        /// or the bots forget four times too fast.
        /// </summary>
        public void DecayOneRound()
        {
            float keep = KeptPerRound(Grudge);

            // Copy the keys: the dictionary cannot change while it is being enumerated.
            foreach (int other in new List<int>(_affinity.Keys))
            {
                float old = _affinity[other];
                float next = old * keep;
                if (Mathf.Abs(next) < ForgetThreshold)
                {
                    next = 0f;
                }

                if (!Mathf.Approximately(next, old))
                {
                    _affinity[other] = next;
                    Changed?.Invoke(other, next);
                }
            }
        }

        /// <summary>
        /// Short Spanish sentence for the LLM prompt, e.g. "te cae mal (lo último: te robó con
        /// el ladrón)".
        /// </summary>
        public string DescribeFor(int otherId)
        {
            string feeling = Feeling(Get(otherId));
            return _lastEvent.TryGetValue(otherId, out RelationshipEventType last)
                ? $"{feeling} (lo último: {RelationshipDeltas.Describe(last)})"
                : feeling;
        }

        /// <summary>Spanish word for an affinity value.</summary>
        public static string Feeling(float affinity) => affinity switch
        {
            <= -0.6f => "le odias",
            <= -0.2f => "te cae mal",
            < 0.2f => "te es indiferente",
            < 0.6f => "te cae bien",
            _ => "es tu aliado"
        };

        /// <summary>
        /// Change an event causes with this grudge. Offences weigh ×0.6 (no grudge) to ×1.6
        /// (full grudge); good deeds ×1.2 down to ×0.8.
        /// </summary>
        public static float ScaledDelta(RelationshipEventType type, float grudge)
        {
            float delta = RelationshipDeltas.Base(type);
            return delta < 0f
                ? delta * Mathf.Lerp(0.6f, 1.6f, grudge)
                : delta * Mathf.Lerp(1.2f, 0.8f, grudge);
        }

        /// <summary>Fraction of the affinity kept each round: 0.85 with no grudge, 0.99 with full grudge.</summary>
        public static float KeptPerRound(float grudge) => Mathf.Lerp(0.85f, 0.99f, grudge);
    }
}
