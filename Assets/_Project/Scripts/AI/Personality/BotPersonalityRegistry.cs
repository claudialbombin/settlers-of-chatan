using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chatan.AI
{
    /// <summary>
    /// Answers "which personality does player N have?" during a game.
    /// </summary>
    /// <remarks>
    /// Built once at the start of the game from the result of
    /// <see cref="PersonalityAssigner.Assign"/>. Read by the bot brain, the trade
    /// evaluator, the response generator and, in debug mode only, the player info cards:
    /// the personality of each bot is a secret for the human player.
    /// </remarks>
    public class BotPersonalityRegistry
    {
        private readonly Dictionary<int, PersonalityProfile> _byPlayer;

        /// <summary>Creates the registry from an assignment and the seed that produced it.</summary>
        /// <param name="assignment">Personality of each bot keyed by player id. It is copied, so later changes to it are not seen.</param>
        /// <param name="seed">Seed used for the draw, kept to reproduce the game.</param>
        public BotPersonalityRegistry(IReadOnlyDictionary<int, PersonalityProfile> assignment, int seed)
        {
            _byPlayer = assignment == null
                ? new Dictionary<int, PersonalityProfile>()
                : assignment.ToDictionary(kv => kv.Key, kv => kv.Value);
            Seed = seed;
        }

        /// <summary>Seed used for the draw.</summary>
        public int Seed { get; }

        /// <summary>Number of bots with a personality.</summary>
        public int Count => _byPlayer.Count;

        /// <summary>Every bot with its personality.</summary>
        public IEnumerable<KeyValuePair<int, PersonalityProfile>> All => _byPlayer;

        /// <summary>Gets the personality of a player, if it is a bot.</summary>
        public bool TryGet(int playerId, out PersonalityProfile profile) =>
            _byPlayer.TryGetValue(playerId, out profile);

        /// <summary>The personality of a player, or <c>null</c> if the player is not a bot (the human).</summary>
        public PersonalityProfile Get(int playerId) =>
            _byPlayer.TryGetValue(playerId, out PersonalityProfile profile) ? profile : null;

        /// <summary>Line for the log, for example <c>Seed 42: P1=Greedy P2=Merchant P3=Cautious</c>.</summary>
        public override string ToString()
        {
            var sb = new StringBuilder($"Seed {Seed}:");
            foreach (KeyValuePair<int, PersonalityProfile> kv in _byPlayer.OrderBy(kv => kv.Key))
            {
                string id = kv.Value != null ? kv.Value.id.ToString() : "none";
                sb.Append($" P{kv.Key}={id}");
            }

            return sb.ToString();
        }
    }
}
