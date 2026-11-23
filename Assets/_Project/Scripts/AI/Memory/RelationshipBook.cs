using System.Collections.Generic;
using Chatan.Core;

namespace Chatan.AI
{
    /// <summary>
    /// One <see cref="RelationshipMemory"/> per bot, fed from the game events.
    /// </summary>
    /// <remarks>
    /// A robbery is written in the victim's memory; the decay runs once per round, when
    /// the turn comes back to the human (seat 0). Trades are not game events yet, so the
    /// negotiation calls <see cref="OnTradeCompleted"/> and <see cref="OnOfferRejected"/>.
    /// Call <see cref="Subscribe"/> when the match starts and <see cref="Unsubscribe"/> when it ends.
    /// </remarks>
    public class RelationshipBook
    {
        /// <summary>Seat whose turn marks the start of a new round.</summary>
        public const int RoundStartSeat = 0;

        private readonly Dictionary<int, RelationshipMemory> _byBot = new Dictionary<int, RelationshipMemory>();

        /// <summary>Creates an empty memory for every bot in <paramref name="personalities"/>.</summary>
        public RelationshipBook(BotPersonalityRegistry personalities)
        {
            foreach (KeyValuePair<int, PersonalityProfile> pair in personalities.All)
            {
                _byBot[pair.Key] = new RelationshipMemory(pair.Key, pair.Value);
            }
        }

        /// <summary>Seats of the bots that have a memory.</summary>
        public IEnumerable<int> BotIds => _byBot.Keys;

        /// <summary>The memory of <paramref name="botId"/>, or null if it is not a bot.</summary>
        public RelationshipMemory For(int botId) =>
            _byBot.TryGetValue(botId, out RelationshipMemory memory) ? memory : null;

        /// <summary>Starts listening to <see cref="GameEvents"/>.</summary>
        public void Subscribe()
        {
            Unsubscribe();
            GameEvents.PlayerRobbed += OnRobbed;
            GameEvents.TurnStarted += OnTurnStarted;
        }

        /// <summary>Stops listening to <see cref="GameEvents"/>.</summary>
        public void Unsubscribe()
        {
            GameEvents.PlayerRobbed -= OnRobbed;
            GameEvents.TurnStarted -= OnTurnStarted;
        }

        /// <summary>Both sides of a closed trade like each other a bit more.</summary>
        public void OnTradeCompleted(int playerA, int playerB)
        {
            For(playerA)?.Record(playerB, RelationshipEventType.TradeCompleted);
            For(playerB)?.Record(playerA, RelationshipEventType.TradeCompleted);
        }

        /// <summary><paramref name="rejecterId"/> turned down an offer from <paramref name="botId"/>.</summary>
        public void OnOfferRejected(int botId, int rejecterId) =>
            For(botId)?.Record(rejecterId, RelationshipEventType.TradeRejectedByThem);

        private void OnRobbed(int thiefId, int victimId) =>
            For(victimId)?.Record(thiefId, RelationshipEventType.RobbedMe);

        private void OnTurnStarted(PlayerState player)
        {
            if (player == null || player.Id != RoundStartSeat)
            {
                return;
            }

            foreach (RelationshipMemory memory in _byBot.Values)
            {
                memory.DecayOneRound();
            }
        }
    }
}
