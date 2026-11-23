using System;
using System.Linq;
using Chatan.AI;
using Chatan.Core;

namespace Chatan.Negotiation
{
    /// <summary>
    /// The brain of the negotiation: decides whether a bot accepts, rejects or counters an
    /// offer, and why.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Seen from the bot that receives the offer, <c>offer.Give</c> is what it GETS and
    /// <c>offer.Receive</c> is what it PAYS. Each resource is worth
    /// <see cref="IResourceValuator.ValueOf"/> × the personality's preference.
    /// </para>
    /// <code>
    /// required    = cost · (1 + 0.6·greed) · (1 − 0.4·cooperation)
    /// friendBonus = cost · affinity · (0.15 + 0.35·grudge)
    /// leaderMalus = cost · 0.8·aggression        if the proposer leads with ≥ 7 points
    /// noise       = cost · randomness · N(0, 0.15)
    /// margin      = gain + friendBonus − leaderMalus + noise − required
    /// </code>
    /// <para>
    /// Accept when margin ≥ 0; counter when margin ≥ −cost · (0.3 + 0.4·cooperation) and
    /// rounds are left; otherwise reject. The coefficients were tuned so the ten
    /// personalities behave differently (see Docs/personalidades.md).
    /// </para>
    /// <para>
    /// Plain C# and deterministic for a given seed, so Teresa's headless simulator can use
    /// it for bot-to-bot trades.
    /// </para>
    /// </remarks>
    public class TradeEvaluator
    {
        /// <summary>How much greed raises the price: required × (1 + GreedFactor·greed).</summary>
        public const float GreedFactor = 0.6f;

        /// <summary>How much cooperation lowers the price: required × (1 − CooperationFactor·cooperation).</summary>
        public const float CooperationFactor = 0.4f;

        /// <summary>Weight of the affinity with no grudge.</summary>
        public const float AffinityBase = 0.15f;

        /// <summary>Extra weight of the affinity per point of grudge.</summary>
        public const float AffinityGrudgeFactor = 0.35f;

        /// <summary>Penalty for helping the leader, per point of aggression.</summary>
        public const float LeaderFactor = 0.8f;

        /// <summary>Visible points from which a leading proposer is punished.</summary>
        public const int LeaderPoints = 7;

        /// <summary>Standard deviation of the noise, scaled by the personality's randomness.</summary>
        public const float NoiseDeviation = 0.15f;

        /// <summary>Counter tolerance with no cooperation, as a fraction of the cost.</summary>
        public const float CounterToleranceBase = 0.3f;

        /// <summary>Extra counter tolerance per point of cooperation.</summary>
        public const float CounterToleranceCooperation = 0.4f;

        /// <summary>Affinity at or below which the bot refuses to talk.</summary>
        public const float GrudgeLimit = -0.8f;

        private readonly IResourceValuator _valuator;
        private readonly Random _rng;

        /// <summary>Creates an evaluator.</summary>
        /// <param name="valuator">How much each resource is worth to each player.</param>
        /// <param name="seed">Seed of the noise. Same seed and same inputs give the same decisions.</param>
        public TradeEvaluator(IResourceValuator valuator, int seed = 0)
        {
            _valuator = valuator ?? throw new ArgumentNullException(nameof(valuator));
            _rng = new Random(seed);
        }

        /// <summary>Decides what bot <paramref name="botId"/> answers to <paramref name="offer"/>.</summary>
        /// <param name="offer">The offer; its <c>ToPlayerId</c> must be <paramref name="botId"/>.</param>
        /// <param name="botId">The bot that decides.</param>
        /// <param name="state">Current game state. It is not modified.</param>
        /// <param name="personality">The bot's personality.</param>
        /// <param name="memory">The bot's relationship memory, or null for a neutral affinity.</param>
        /// <param name="canStillCounter">False on the last negotiation round: "almost" becomes a rejection.</param>
        public TradeDecision Evaluate(
            TradeOffer offer,
            int botId,
            GameState state,
            PersonalityProfile personality,
            RelationshipMemory memory,
            bool canStillCounter)
        {
            if (personality == null)
            {
                throw new ArgumentNullException(nameof(personality));
            }

            if (offer == null || state == null || !offer.IsValidShape || offer.ToPlayerId != botId)
            {
                return Reject(TradeReason.Malformed);
            }

            PlayerState bot = state.Players.Find(p => p.Id == botId);
            PlayerState proposer = state.Players.Find(p => p.Id == offer.FromPlayerId);
            if (bot == null || proposer == null)
            {
                return Reject(TradeReason.Malformed);
            }

            // The bot pays what the proposer asks for and gets what the proposer gives.
            if (!bot.Resources.Contains(offer.Receive))
            {
                return Reject(TradeReason.DontHaveIt);
            }

            if (!proposer.Resources.Contains(offer.Give))
            {
                return Reject(TradeReason.YouDontHaveIt);
            }

            float affinity = memory?.Get(proposer.Id) ?? 0f;
            if (affinity <= GrudgeLimit)
            {
                return Reject(TradeReason.Grudge);
            }

            float cost = Value(offer.Receive, botId, state, personality);
            float gain = Value(offer.Give, botId, state, personality);

            float required = cost * (1f + GreedFactor * personality.greed) * (1f - CooperationFactor * personality.cooperation);
            float friendBonus = cost * affinity * (AffinityBase + AffinityGrudgeFactor * personality.grudge);
            bool proposerLeads = IsLeader(proposer, state);
            float leaderMalus = proposerLeads ? cost * LeaderFactor * personality.aggression : 0f;
            float noise = cost * personality.randomness * Gaussian(NoiseDeviation);

            float margin = gain + friendBonus - leaderMalus + noise - required;
            var decision = new TradeDecision { Gain = gain, Cost = cost, Margin = margin };

            if (margin >= 0f)
            {
                decision.Type = TradeDecisionType.Accept;
                decision.Reason = friendBonus > 0f && margin - friendBonus < 0f
                    ? TradeReason.FriendlyDiscount
                    : TradeReason.GoodDeal;
                return decision;
            }

            float tolerance = cost * (CounterToleranceBase + CounterToleranceCooperation * personality.cooperation);
            if (canStillCounter && margin >= -tolerance)
            {
                decision.Type = TradeDecisionType.Counter;
                decision.Reason = TradeReason.AlmostThere;
                return decision;
            }

            decision.Type = TradeDecisionType.Reject;
            decision.Reason = leaderMalus > 0f && margin + leaderMalus >= -tolerance
                ? TradeReason.HelpsTheLeader
                : TradeReason.TooLittle;
            return decision;
        }

        /// <summary>Value of a bundle for <paramref name="playerId"/> with that personality's preferences.</summary>
        public float Value(ResourceBundle bundle, int playerId, GameState state, PersonalityProfile personality)
        {
            float total = 0f;
            foreach (ResourceType type in ResourceBundle.AllTypes)
            {
                int amount = bundle[type];
                if (amount != 0)
                {
                    total += amount * _valuator.ValueOf(type, playerId, state) * personality.PreferenceFor(type);
                }
            }

            return total;
        }

        /// <summary>True when <paramref name="player"/> has the most visible points and at least <see cref="LeaderPoints"/>.</summary>
        public static bool IsLeader(PlayerState player, GameState state) =>
            player.VisiblePoints >= LeaderPoints
            && player.VisiblePoints >= state.Players.Max(p => p.VisiblePoints);

        // Box–Muller transform: two uniform numbers give one normally distributed one.
        private float Gaussian(float deviation)
        {
            double u1 = 1.0 - _rng.NextDouble();
            double u2 = 1.0 - _rng.NextDouble();
            double z = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            return deviation * (float)z;
        }

        private static TradeDecision Reject(TradeReason reason) =>
            new TradeDecision { Type = TradeDecisionType.Reject, Reason = reason };
    }
}
