using System;
using System.Collections.Generic;
using Chatan.AI;
using Chatan.Core;

namespace Chatan.Negotiation
{
    /// <summary>
    /// When a bot says "almost", finds the smallest change to the human's offer that the
    /// bot would accept.
    /// </summary>
    /// <remarks>
    /// <para>
    /// It tries small edits of the original offer and evaluates each one with the same
    /// <see cref="TradeEvaluator"/>, as if the human had proposed it. It keeps the one the
    /// bot accepts that is closest to the original, so the counter-offer is reasonable and
    /// not abusive. The edits are:
    /// </para>
    /// <list type="number">
    /// <item>the human gives one more card of a resource they own;</item>
    /// <item>the bot gives one card less (when it gives more than one);</item>
    /// <item>one card the human asks for is swapped for a different resource;</item>
    /// <item>only if none of those work: the human gives one more of two different resources.</item>
    /// </list>
    /// <para>
    /// The result is written as if the human proposed it (<c>FromPlayerId</c> = human), so
    /// it is easy to compare with the original and to show ("¿y si me das 3 trigo en vez de 2?").
    /// </para>
    /// </remarks>
    public class CounterOfferGenerator
    {
        private readonly TradeEvaluator _evaluator;

        /// <summary>Creates a generator that asks <paramref name="evaluator"/> about each candidate.</summary>
        public CounterOfferGenerator(TradeEvaluator evaluator)
        {
            _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
        }

        /// <summary>Finds the counter-offer closest to <paramref name="original"/> that the bot accepts.</summary>
        /// <param name="original">The human's offer (From = human, To = bot).</param>
        /// <param name="botId">The bot that counters.</param>
        /// <param name="state">Current game state. It is not modified.</param>
        /// <param name="personality">The bot's personality.</param>
        /// <param name="memory">The bot's relationship memory, or null.</param>
        /// <returns>
        /// A new offer with status <see cref="TradeStatus.Countered"/>, or null when no small
        /// change works (for instance, the human has nothing else to give).
        /// </returns>
        public TradeOffer Generate(TradeOffer original, int botId, GameState state,
                                   PersonalityProfile personality, RelationshipMemory memory)
        {
            if (original == null || state == null || personality == null)
            {
                return null;
            }

            PlayerState human = state.Players.Find(p => p.Id == original.FromPlayerId);
            if (human == null)
            {
                return null;
            }

            TradeOffer best = null;
            int bestChange = int.MaxValue;
            foreach (TradeOffer candidate in SimpleEdits(original))
            {
                int change = Distance(original, candidate);
                if (change < bestChange && IsAcceptable(candidate, human, botId, state, personality, memory))
                {
                    best = candidate;
                    bestChange = change;
                }
            }

            if (best == null)
            {
                best = FirstAcceptableDoubleAsk(original, human, botId, state, personality, memory);
            }

            if (best != null)
            {
                best.Status = TradeStatus.Countered;
            }

            return best;
        }

        /// <summary>Number of cards that differ between two offers, counting both sides.</summary>
        public static int Distance(TradeOffer a, TradeOffer b)
        {
            int distance = 0;
            foreach (ResourceType type in ResourceBundle.AllTypes)
            {
                distance += Math.Abs(a.Give[type] - b.Give[type]) + Math.Abs(a.Receive[type] - b.Receive[type]);
            }

            return distance;
        }

        // The original comes first, so a deal the bot already likes is returned unchanged.
        private static IEnumerable<TradeOffer> SimpleEdits(TradeOffer original)
        {
            yield return Clone(original);

            foreach (ResourceType type in ResourceBundle.AllTypes)
            {
                // Giving and asking for the same resource makes no sense.
                if (original.Receive[type] == 0)
                {
                    TradeOffer more = Clone(original);
                    more.Give[type] += 1;
                    yield return more;
                }

                if (original.Receive[type] > 0 && original.Receive.Total > 1)
                {
                    TradeOffer less = Clone(original);
                    less.Receive[type] -= 1;
                    yield return less;
                }
            }

            foreach (ResourceType from in ResourceBundle.AllTypes)
            {
                if (original.Receive[from] == 0)
                {
                    continue;
                }

                foreach (ResourceType to in ResourceBundle.AllTypes)
                {
                    if (to == from || original.Give[to] > 0)
                    {
                        continue;
                    }

                    TradeOffer swap = Clone(original);
                    swap.Receive[from] -= 1;
                    swap.Receive[to] += 1;
                    yield return swap;
                }
            }
        }

        private TradeOffer FirstAcceptableDoubleAsk(TradeOffer original, PlayerState human, int botId, GameState state,
                                                    PersonalityProfile personality, RelationshipMemory memory)
        {
            IReadOnlyList<ResourceType> all = ResourceBundle.AllTypes;
            for (int i = 0; i < all.Count; i++)
            {
                for (int j = i + 1; j < all.Count; j++)
                {
                    if (original.Receive[all[i]] > 0 || original.Receive[all[j]] > 0)
                    {
                        continue;
                    }

                    TradeOffer candidate = Clone(original);
                    candidate.Give[all[i]] += 1;
                    candidate.Give[all[j]] += 1;
                    if (IsAcceptable(candidate, human, botId, state, personality, memory))
                    {
                        return candidate;
                    }
                }
            }

            return null;
        }

        private bool IsAcceptable(TradeOffer candidate, PlayerState human, int botId, GameState state,
                                  PersonalityProfile personality, RelationshipMemory memory)
        {
            // The human must be able to pay it; the evaluator checks the bot's side.
            if (!human.Resources.Contains(candidate.Give))
            {
                return false;
            }

            TradeDecision decision = _evaluator.Evaluate(candidate, botId, state, personality, memory, canStillCounter: false);
            return decision.Type == TradeDecisionType.Accept;
        }

        // ResourceBundle is a struct, so editing the copy's bundles never touches the original.
        private static TradeOffer Clone(TradeOffer offer) => new TradeOffer
        {
            Id = offer.Id,
            FromPlayerId = offer.FromPlayerId,
            ToPlayerId = offer.ToPlayerId,
            Give = offer.Give,
            Receive = offer.Receive,
            Status = offer.Status
        };
    }
}
