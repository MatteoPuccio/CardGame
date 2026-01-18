using System;
using System.Collections.Generic;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Effects;

namespace Assets.Scripts.CardEngine.Game
{
    /// <summary>
    /// Runs an arbitrary effect as a session that can pause for player target selection.
    /// For ITargetingEffect, this session performs selection first and then resolves via ResolveAfterTargets.
    /// </summary>
    public sealed class EffectResolveSession : ITargetingSession
    {
        private readonly Card _card;
        private readonly EffectContext _context;
        private readonly Effect _rootEffect;
        private readonly ITargetingEffect _targetingRoot;
        private readonly Action<bool, string> _onFinished;

        private bool _finished;
        private bool _wasCancelled;
        private string _cancelReason;

        public Card Card => _card;
        public bool WasCancelled => _wasCancelled;
        public string CancelReason => _cancelReason;

        public EffectResolveSession(Card card, Effect rootEffect, EffectContext context, Action<bool, string> onFinished)
        {
            _card = card ?? throw new ArgumentNullException(nameof(card));
            _rootEffect = rootEffect;
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _onFinished = onFinished;

            _targetingRoot = _rootEffect as ITargetingEffect;

            if (_rootEffect is IResettableEffect resettable)
                resettable.Reset();
        }

        public bool TryAdvance(out List<ITargetable> candidates)
        {
            candidates = null;

            if (_finished || _wasCancelled)
                return false;

            if (_rootEffect == null)
            {
                Finish(success: true, cancelReason: null);
                return false;
            }

            if (_targetingRoot == null)
            {
                _rootEffect.Resolve(_context);
                Finish(success: true, cancelReason: null);
                return false;
            }

            var state = TargetingSelectionUtils.AdvanceToRequestOrComplete(
                _targetingRoot,
                _context,
                out candidates,
                out var cancelReason);

            if (state == TargetingAdvanceState.NeedsPlayerInput)
                return true;

            if (state == TargetingAdvanceState.Cancelled)
            {
                Cancel(cancelReason);
                return false;
            }

            _targetingRoot.ResolveAfterTargets(_context);
            Finish(success: true, cancelReason: null);
            return false;
        }

        public void ProvideTargets(List<ITargetable> targets)
        {
            if (_finished || _targetingRoot == null || _targetingRoot.IsComplete)
                return;

            _targetingRoot.ApplyTargets(_context, targets);
        }

        public void Cancel(string reason = null)
        {
            if (_finished || _wasCancelled)
                return;

            _wasCancelled = true;
            _cancelReason = string.IsNullOrWhiteSpace(reason) ? "Cancelled." : reason;
            Finish(success: false, cancelReason: _cancelReason);
        }

        private void Finish(bool success, string cancelReason)
        {
            if (_finished)
                return;

            _finished = true;
            _onFinished?.Invoke(success, cancelReason);
        }
    }
}
