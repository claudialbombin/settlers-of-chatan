using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Chatan.Board;
using Chatan.Core;
using Chatan.Rules;
using Chatan.UI;
using UnityEngine;

namespace Chatan.Players
{
    /// <summary>
    /// The human seat: waits for the HUD, the build menu and clicks on the board, and
    /// hands the resulting action to the turn manager.
    /// </summary>
    /// <remarks>
    /// <para>
    /// It never decides what is legal. It asks <see cref="Rules"/> which pieces to light
    /// up, and the rules validate the action again when they apply it.
    /// </para>
    /// <para>
    /// Clicks outside the human's turn are ignored: an action is only accepted while a
    /// <see cref="ChooseActionAsync"/> call is waiting.
    /// </para>
    /// </remarks>
    public class HumanPlayerController : MonoBehaviour, IPlayerController
    {
        [SerializeField] private int playerId;
        [SerializeField] private BoardInput boardInput;
        [SerializeField] private DiscardDialog discardDialog;
        [SerializeField] private RobberVictimDialog robberDialog;

        private readonly List<BoardClickTarget> _lit = new List<BoardClickTarget>();
        private HashSet<int> _validIds = new HashSet<int>();
        private TaskCompletionSource<IGameAction> _pending;
        private CancellationTokenRegistration _cancel;
        private GameState _state;
        private bool _choosingVictim;

        /// <inheritdoc/>
        public int PlayerId => playerId;

        /// <summary>The rules to ask for valid positions. Set by whoever starts the match.</summary>
        public IRulesQuery Rules { get; set; }

        /// <summary>Current placement mode, <see cref="PlacementMode.None"/> when idle.</summary>
        public PlacementMode Mode { get; private set; } = PlacementMode.None;

        /// <summary>Whether an action is being waited for (it is the human's move).</summary>
        public bool IsWaitingForAction => _pending != null && !_pending.Task.IsCompleted;

        private void OnEnable()
        {
            UIEvents.RollDiceRequested += OnRoll;
            UIEvents.EndTurnRequested += OnEndTurn;
            UIEvents.PlacementModeChanged += EnterMode;
            UIEvents.BuyDevCardRequested += OnBuyDevCard;
            if (boardInput != null)
            {
                boardInput.TargetClicked += OnTargetClicked;
                boardInput.RightClicked += CancelMode;
            }
        }

        private void OnDisable()
        {
            UIEvents.RollDiceRequested -= OnRoll;
            UIEvents.EndTurnRequested -= OnEndTurn;
            UIEvents.PlacementModeChanged -= EnterMode;
            UIEvents.BuyDevCardRequested -= OnBuyDevCard;
            if (boardInput != null)
            {
                boardInput.TargetClicked -= OnTargetClicked;
                boardInput.RightClicked -= CancelMode;
            }

            _pending?.TrySetCanceled();
            ClearHighlights();
        }

        // ---------------------------------------------------------------- IPlayerController

        /// <inheritdoc/>
        public Task<IGameAction> ChooseActionAsync(GameState state, CancellationToken ct)
        {
            _pending?.TrySetCanceled();
            _cancel.Dispose();

            _state = state;
            _pending = new TaskCompletionSource<IGameAction>();
            TaskCompletionSource<IGameAction> pending = _pending;
            _cancel = ct.Register(() => pending.TrySetCanceled());

            PlacementMode forced = HumanTurnLogic.ForcedMode(state, Rules, Me);
            if (forced != PlacementMode.None)
            {
                EnterMode(forced);
            }

            return _pending.Task;
        }

        /// <inheritdoc/>
        public Task<ResourceBundle> ChooseDiscardAsync(GameState state, int count, CancellationToken ct)
        {
            _state = state;
            return discardDialog.Ask(Me.Resources, count, ct);
        }

        // ---------------------------------------------------------------- buttons

        private void OnRoll() => Submit(new RollDiceAction(playerId));

        private void OnBuyDevCard() => Submit(new BuyDevCardAction(playerId));

        private void OnEndTurn() => Submit(new EndTurnAction(playerId));

        // ---------------------------------------------------------------- placement

        private void EnterMode(PlacementMode mode)
        {
            ClearHighlights();
            Mode = PlacementMode.None;
            if (!IsWaitingForAction || mode == PlacementMode.None)
            {
                return;
            }

            Mode = mode;
            _validIds = HumanTurnLogic.ValidIds(mode, Rules, _state, playerId);

            BoardTargetKind kind = HumanTurnLogic.KindFor(mode);
            foreach (BoardClickTarget target in FindObjectsByType<BoardClickTarget>())
            {
                if (target.kind == kind && _validIds.Contains(target.id))
                {
                    target.SetHighlighted(true);
                    _lit.Add(target);
                }
            }

            if (_validIds.Count == 0)
            {
                Debug.Log($"[Human] No valid positions for {mode}.", this);
            }
        }

        private void CancelMode()
        {
            if (Mode == PlacementMode.None || _choosingVictim || !HumanTurnLogic.CanCancel(_state, Rules, Me))
            {
                return;
            }

            ClearHighlights();
            Mode = PlacementMode.None;
        }

        private async void OnTargetClicked(BoardClickTarget target)
        {
            if (_choosingVictim
                || Mode == PlacementMode.None
                || target.kind != HumanTurnLogic.KindFor(Mode)
                || !_validIds.Contains(target.id))
            {
                return;
            }

            try
            {
                IGameAction action = Mode == PlacementMode.Robber
                    ? await RobberActionAsync(target.id)
                    : HumanTurnLogic.BuildActionFor(Mode, playerId, target.id);

                Submit(action);
            }
            catch (OperationCanceledException)
            {
                // The turn was cancelled while the victim dialog was open.
            }
            catch (Exception e)
            {
                // async void would swallow it otherwise.
                Debug.LogException(e, this);
            }
        }

        private async Task<IGameAction> RobberActionAsync(int tileId)
        {
            List<int> victims = (Rules?.RobbablePlayersAt(_state, tileId, playerId) ?? Enumerable.Empty<int>()).ToList();
            int? victim = HumanTurnLogic.AutomaticVictim(victims);
            if (victim == null)
            {
                _choosingVictim = true;
                try
                {
                    victim = await robberDialog.Ask(victims, _state);
                }
                finally
                {
                    _choosingVictim = false;
                }
            }

            return new MoveRobberAction(playerId, tileId, victim.Value);
        }

        // ---------------------------------------------------------------- helpers

        private PlayerState Me => _state?.Players.FirstOrDefault(p => p.Id == playerId);

        private void Submit(IGameAction action)
        {
            if (action == null || !IsWaitingForAction)
            {
                return;
            }

            ClearHighlights();
            Mode = PlacementMode.None;
            _cancel.Dispose();
            _pending.TrySetResult(action);
        }

        private void ClearHighlights()
        {
            foreach (BoardClickTarget target in _lit)
            {
                if (target != null)
                {
                    target.SetHighlighted(false);
                }
            }

            _lit.Clear();
            _validIds.Clear();
        }
    }
}
