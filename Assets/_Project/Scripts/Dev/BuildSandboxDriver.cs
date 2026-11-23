using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Chatan.AI;
using Chatan.Core;
using Chatan.Players;
using Chatan.Rules;
using Chatan.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Chatan.Dev
{
    /// <summary>
    /// Fake turn manager for <c>Sandbox_Claudia</c>: asks the <see cref="HumanPlayerController"/>
    /// for actions in a loop, applies them to a made-up state with no real rules, and
    /// logs each one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Takes over from <see cref="HudSandboxDriver"/> (the set-up menu disables that one),
    /// so only one script answers the HUD buttons.
    /// </para>
    /// <para>
    /// Each roll gives the human two random cards. A 7 starts the discard (if they hold
    /// more than 7 cards) and then the robber. Bot turns only wait <see cref="botTurnSeconds"/>.
    /// Pick a <see cref="scenario"/> to start straight in the discard, robber or setup phase.
    /// </para>
    /// <para>
    /// With a catalog and an info panel it also keeps a <see cref="RelationshipBook"/>, so
    /// robbing a bot lowers its affinity bar. R and T fake a robbery or a trade with Bot Azul.
    /// </para>
    /// </remarks>
    public class BuildSandboxDriver : MonoBehaviour
    {
        /// <summary>Where the sandbox match starts.</summary>
        public enum Scenario
        {
            /// <summary>Roll phase of the human's turn, with the factory hand.</summary>
            Normal,

            /// <summary>A 7 was just rolled and the human holds 9 cards.</summary>
            Discard,

            /// <summary>The human must move the robber.</summary>
            Robber,

            /// <summary>First setup round: free settlement and road.</summary>
            Setup,

            /// <summary>Main phase with plenty of every resource, to try every button.</summary>
            Rich
        }

        [SerializeField] private HumanPlayerController human;
        [SerializeField] private FakeBoardTargets board;
        [SerializeField] private Scenario scenario = Scenario.Normal;

        [Tooltip("Seconds a bot turn lasts.")]
        [SerializeField] private float botTurnSeconds = 1.5f;

        [Header("Optional")]
        [SerializeField] private PersonalityCatalog catalog;
        [SerializeField] private PlayerInfoPanel infoPanel;

        private readonly System.Random _rng = new System.Random();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private FakeRulesQuery _rules;
        private RelationshipBook _relationships;
        private GameState _state;

        private async void Start()
        {
            if (human == null || board == null)
            {
                Debug.LogError("[Sandbox] Assign the human controller and the fake board.", this);
                return;
            }

            _state = FakeGameStateFactory.Create();
            _rules = new FakeRulesQuery(board.VertexCount, board.EdgeCount, board.TileCount);
            // The factory hand has 2 settlements on the board; give them a place.
            _rules.AddSettlement(FakeGameStateFactory.HumanId, 0);
            _rules.AddSettlement(FakeGameStateFactory.HumanId, 4);
            human.Rules = _rules;
            ApplyScenario();

            if (infoPanel != null && catalog != null)
            {
                int seed = Environment.TickCount;
                int[] botIds = _state.Players.Where(p => !p.IsHuman).Select(p => p.Id).ToArray();
                var registry = new BotPersonalityRegistry(PersonalityAssigner.Assign(botIds, catalog.Profiles, seed), seed);
                infoPanel.SetPersonalities(registry);

                _relationships = new RelationshipBook(registry);
                _relationships.Subscribe();
                infoPanel.BindRelationships(_relationships);
                Debug.Log("[Sandbox] Keys: R = you rob Bot Azul, T = you trade with Bot Azul.", this);
            }

            Debug.Log($"[Sandbox] Build sandbox started in scenario {scenario}.", this);
            GameEvents.RaiseStateChanged(_state);

            try
            {
                if (scenario == Scenario.Discard)
                {
                    await DiscardThenRobberAsync();
                }

                await RunAsync(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Play mode stopped.
            }
            catch (Exception e)
            {
                Debug.LogException(e, this);
            }
        }

        // Test keys for the relationship memory, the real triggers come from the robber and the chat.
        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (_relationships == null || keyboard == null)
            {
                return;
            }

            if (keyboard.rKey.wasPressedThisFrame)
            {
                GameEvents.RaisePlayerRobbed(FakeGameStateFactory.HumanId, 1);
                LogAffinity(1);
            }
            else if (keyboard.tKey.wasPressedThisFrame)
            {
                _relationships.OnTradeCompleted(FakeGameStateFactory.HumanId, 1);
                LogAffinity(1);
            }
        }

        private void LogAffinity(int botId)
        {
            RelationshipMemory memory = _relationships.For(botId);
            Debug.Log($"[Sandbox] Bot {botId} about you: {memory.Get(FakeGameStateFactory.HumanId):0.00}, {memory.DescribeFor(FakeGameStateFactory.HumanId)}", this);
        }

        private void OnDestroy()
        {
            _relationships?.Unsubscribe();
            if (infoPanel != null)
            {
                infoPanel.BindRelationships(null);
            }

            _cts.Cancel();
            _cts.Dispose();
        }

        private void ApplyScenario()
        {
            PlayerState me = _state.Players[FakeGameStateFactory.HumanId];
            switch (scenario)
            {
                case Scenario.Discard:
                    me.Resources = new ResourceBundle(2, 2, 2, 2, 1);
                    _state.LastDiceA = 3;
                    _state.LastDiceB = 4;
                    break;
                case Scenario.Robber:
                    _state.Phase = GamePhase.MoveRobber;
                    break;
                case Scenario.Setup:
                    _state.Phase = GamePhase.SetupRound1;
                    me.SettlementsLeft = BuildChoices.MaxSettlements;
                    me.RoadsLeft = HumanTurnLogic.MaxRoads;
                    me.VisiblePoints = 0;
                    _rules = new FakeRulesQuery(board.VertexCount, board.EdgeCount, board.TileCount) { FreePlacement = true };
                    human.Rules = _rules;
                    break;
                case Scenario.Rich:
                    me.Resources = new ResourceBundle(5, 5, 5, 5, 5);
                    _state.Phase = GamePhase.Main;
                    break;
            }
        }

        private async Task RunAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested && _state.Phase != GamePhase.GameOver)
            {
                if (_state.CurrentPlayer.IsHuman)
                {
                    IGameAction action = await human.ChooseActionAsync(_state, ct);
                    Debug.Log($"[Sandbox] {action}", this);
                    await ApplyAsync(action);
                }
                else
                {
                    await Task.Delay(TimeSpan.FromSeconds(botTurnSeconds), ct);
                    NextTurn();
                }

                GameEvents.RaiseStateChanged(_state);
            }
        }

        private async Task ApplyAsync(IGameAction action)
        {
            PlayerState me = _state.Players[action.PlayerId];
            bool free = _rules.IsFreePlacement(_state);

            switch (action)
            {
                case RollDiceAction _:
                    await RollAsync(me);
                    break;
                case EndTurnAction _:
                    NextTurn();
                    break;
                case BuildRoadAction road:
                    Pay(me, ResourceBundle.RoadCost, free);
                    me.RoadsLeft--;
                    _rules.AddRoad(road.EdgeId);
                    if (free)
                    {
                        EndSetup();
                    }

                    break;
                case BuildSettlementAction settlement:
                    Pay(me, ResourceBundle.SettlementCost, free);
                    me.SettlementsLeft--;
                    me.VisiblePoints++;
                    _rules.AddSettlement(me.Id, settlement.VertexId);
                    break;
                case BuildCityAction city:
                    Pay(me, ResourceBundle.CityCost, false);
                    me.CitiesLeft--;
                    me.SettlementsLeft++;
                    me.VisiblePoints++;
                    _rules.UpgradeToCity(city.VertexId);
                    break;
                case BuyDevCardAction _:
                    Pay(me, ResourceBundle.DevCardCost, false);
                    me.DevCards.Add((DevCardType)_rng.Next(0, 5));
                    break;
                case MoveRobberAction robber:
                    _rules.MoveRobber(robber.TileId);
                    Steal(me, robber.VictimPlayerId);
                    if (robber.VictimPlayerId != MoveRobberAction.NoVictim && _relationships != null)
                    {
                        LogAffinity(robber.VictimPlayerId);
                    }

                    _state.Phase = GamePhase.Main;
                    break;
            }
        }

        private async Task RollAsync(PlayerState me)
        {
            _state.LastDiceA = _rng.Next(1, 7);
            _state.LastDiceB = _rng.Next(1, 7);
            GameEvents.RaiseDiceRolled(_state.LastDiceA, _state.LastDiceB);

            if (_state.LastDiceA + _state.LastDiceB == 7)
            {
                await DiscardThenRobberAsync();
                return;
            }

            for (int i = 0; i < 2; i++)
            {
                me.Resources[ResourceBundle.AllTypes[_rng.Next(0, 5)]] += 1;
            }

            _state.Phase = GamePhase.Main;
        }

        private async Task DiscardThenRobberAsync()
        {
            PlayerState me = _state.Players[FakeGameStateFactory.HumanId];
            if (me.Resources.Total > 7)
            {
                _state.Phase = GamePhase.Discard;
                GameEvents.RaiseStateChanged(_state);

                int count = me.Resources.Total / 2;
                ResourceBundle cards = await human.ChooseDiscardAsync(_state, count, _cts.Token);
                IGameAction discard = new DiscardAction(me.Id, cards);
                Debug.Log($"[Sandbox] {discard}", this);
                me.Resources -= cards;
            }

            _state.Phase = GamePhase.MoveRobber;
            GameEvents.RaiseStateChanged(_state);
        }

        private void Steal(PlayerState thief, int victimId)
        {
            if (victimId == MoveRobberAction.NoVictim)
            {
                return;
            }

            PlayerState victim = _state.Players[victimId];
            ResourceType[] held = ResourceBundle.AllTypes.Where(t => victim.Resources[t] > 0).ToArray();
            if (held.Length == 0)
            {
                return;
            }

            ResourceType stolen = held[_rng.Next(held.Length)];
            victim.Resources[stolen] -= 1;
            thief.Resources[stolen] += 1;
            GameEvents.RaisePlayerRobbed(thief.Id, victimId);
        }

        // No validation here on purpose: the button was enabled, so it can pay.
        private static void Pay(PlayerState player, ResourceBundle cost, bool free)
        {
            if (!free)
            {
                player.Resources -= cost;
            }
        }

        private void EndSetup()
        {
            _rules.FreePlacement = false;
            _state.Phase = GamePhase.Roll;
        }

        private void NextTurn()
        {
            _state.CurrentPlayerIndex = (_state.CurrentPlayerIndex + 1) % _state.Players.Count;
            _state.Phase = GamePhase.Roll;
            if (_state.CurrentPlayerIndex == 0)
            {
                _state.TurnNumber++;
            }

            _state.LastDiceA = 0;
            _state.LastDiceB = 0;
            GameEvents.RaiseTurnStarted(_state.CurrentPlayer);
        }
    }
}
