using System;
using System.Collections.Generic;
using Chatan.AI;
using Chatan.Core;
using UnityEngine;

namespace Chatan.UI
{
    /// <summary>
    /// Column of info cards, one per bot. Creates each card the first time the bot
    /// appears in the game state and repaints them on every state change.
    /// </summary>
    public class PlayerInfoPanel : MonoBehaviour
    {
        [SerializeField] private PlayerInfoCard cardPrefab;

        [Tooltip("Parent of the cards, with a Vertical Layout Group.")]
        [SerializeField] private Transform container;

        [Tooltip("Optional. Without it nothing secret is shown.")]
        [SerializeField] private DebugSettings debugSettings;

        [Tooltip("Player id of the human, who gets no card.")]
        [SerializeField] private int humanPlayerId;

        private readonly Dictionary<int, PlayerInfoCard> _cards = new Dictionary<int, PlayerInfoCard>();
        private BotPersonalityRegistry _personalities;
        private RelationshipBook _relationships;
        private readonly Dictionary<int, Action<int, float>> _affinityHandlers = new Dictionary<int, Action<int, float>>();

        /// <summary>Raised with the player id when any bot card is clicked. The chat panel listens to it.</summary>
        public event Action<int> BotCardClicked;

        /// <summary>Number of cards created so far.</summary>
        public int CardCount => _cards.Count;

        /// <summary>Gives the panel the personalities of the bots (only shown in debug mode).</summary>
        public void SetPersonalities(BotPersonalityRegistry registry) => _personalities = registry;

        /// <summary>
        /// Makes each card's affinity bar follow what that bot feels about the human.
        /// Pass null to stop. Cards created later start at the current value.
        /// </summary>
        public void BindRelationships(RelationshipBook book)
        {
            UnbindRelationships();
            _relationships = book;
            if (book == null)
            {
                return;
            }

            foreach (int botId in book.BotIds)
            {
                int id = botId;
                Action<int, float> handler = (other, value) =>
                {
                    if (other == humanPlayerId)
                    {
                        GetCard(id)?.ShowAffinity(value);
                    }
                };
                book.For(id).Changed += handler;
                _affinityHandlers[id] = handler;
                GetCard(id)?.ShowAffinity(AffinityWithHuman(id));
            }
        }

        /// <summary>The card of a bot, or null if it has none yet.</summary>
        public PlayerInfoCard GetCard(int playerId) =>
            _cards.TryGetValue(playerId, out PlayerInfoCard card) ? card : null;

        /// <summary>
        /// Creates the missing cards and repaints all of them. Called on every
        /// <see cref="GameEvents.StateChanged"/>; public so it can be tested directly.
        /// </summary>
        public void Refresh(GameState state)
        {
            if (state == null || state.Players.Count == 0)
            {
                return;
            }

            int currentId = state.CurrentPlayer.Id;
            foreach (PlayerState player in state.Players)
            {
                if (player.Id == humanPlayerId)
                {
                    continue;
                }

                // Cards are created once and reused: creating them on every state change
                // would pile up duplicates.
                if (!_cards.TryGetValue(player.Id, out PlayerInfoCard card))
                {
                    card = CreateCard(player.Id);
                }

                card.Show(player, player.Id == currentId, _personalities?.Get(player.Id), debugSettings);
            }
        }

        private PlayerInfoCard CreateCard(int playerId)
        {
            PlayerInfoCard card = Instantiate(cardPrefab, container != null ? container : transform);
            card.name = $"PlayerInfoCard_P{playerId}";
            card.Bind(playerId);
            card.Clicked += OnCardClicked;
            card.ShowAffinity(AffinityWithHuman(playerId));
            _cards[playerId] = card;
            return card;
        }

        private float AffinityWithHuman(int botId) => _relationships?.For(botId)?.Get(humanPlayerId) ?? 0f;

        private void UnbindRelationships()
        {
            if (_relationships != null)
            {
                foreach (KeyValuePair<int, Action<int, float>> pair in _affinityHandlers)
                {
                    RelationshipMemory memory = _relationships.For(pair.Key);
                    if (memory != null)
                    {
                        memory.Changed -= pair.Value;
                    }
                }
            }

            _affinityHandlers.Clear();
            _relationships = null;
        }

        private void OnDestroy() => UnbindRelationships();

        private void OnCardClicked(int playerId) => BotCardClicked?.Invoke(playerId);

        private void OnEnable() => GameEvents.StateChanged += Refresh;

        private void OnDisable() => GameEvents.StateChanged -= Refresh;
    }
}
