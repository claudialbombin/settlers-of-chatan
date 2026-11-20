using System;
using System.Collections.Generic;
using System.Linq;

namespace Chatan.AI
{
    /// <summary>
    /// Picks a random personality for each bot at the start of a game.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The draw is reproducible: the same seed, bots and catalog always give the same
    /// assignment, so a game that shows a bug can be replayed with the seed from the log.
    /// </para>
    /// <para>
    /// It uses <see cref="System.Random"/> instead of <c>UnityEngine.Random</c>: a seeded
    /// <see cref="System.Random"/> does not disturb other systems that use Unity's global
    /// random state (dice, board generation), and it also works in tests and in the
    /// headless simulator.
    /// </para>
    /// </remarks>
    public static class PersonalityAssigner
    {
        /// <summary>
        /// Assigns one personality to each bot.
        /// </summary>
        /// <param name="botIds">Player ids of the bots (for example 1, 2 and 3).</param>
        /// <param name="catalog">Available personalities. Null entries are ignored; the list is never modified.</param>
        /// <param name="seed">Seed of the draw. Log it to be able to reproduce the game.</param>
        /// <param name="allowRepeats">
        /// If <c>false</c> (default) no two bots get the same personality, which requires
        /// at least as many personalities as bots.
        /// </param>
        /// <returns>Personality of each bot, keyed by player id. Empty if there are no bots.</returns>
        /// <exception cref="ArgumentException">
        /// If a bot id is repeated, the catalog has no personalities, or repeats are not
        /// allowed and there are fewer personalities than bots.
        /// </exception>
        public static Dictionary<int, PersonalityProfile> Assign(
            IReadOnlyList<int> botIds,
            IReadOnlyList<PersonalityProfile> catalog,
            int seed,
            bool allowRepeats = false)
        {
            var result = new Dictionary<int, PersonalityProfile>();
            if (botIds == null || botIds.Count == 0)
            {
                return result;
            }

            if (botIds.Distinct().Count() != botIds.Count)
            {
                throw new ArgumentException("Each bot id must appear only once.", nameof(botIds));
            }

            // Work on a copy: shuffling the catalog itself would reorder the asset's list.
            List<PersonalityProfile> pool = catalog?.Where(p => p != null).ToList() ?? new List<PersonalityProfile>();
            if (pool.Count == 0)
            {
                throw new ArgumentException("The personality catalog is empty.", nameof(catalog));
            }

            if (!allowRepeats && pool.Count < botIds.Count)
            {
                throw new ArgumentException(
                    $"There are {botIds.Count} bots but only {pool.Count} personalities: enable allowRepeats.",
                    nameof(catalog));
            }

            var rng = new Random(seed);

            if (allowRepeats)
            {
                foreach (int botId in botIds)
                {
                    result[botId] = pool[rng.Next(pool.Count)];
                }

                return result;
            }

            // Fisher-Yates shuffle, then the first N personalities go to the N bots in order.
            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }

            for (int k = 0; k < botIds.Count; k++)
            {
                result[botIds[k]] = pool[k];
            }

            return result;
        }
    }
}
