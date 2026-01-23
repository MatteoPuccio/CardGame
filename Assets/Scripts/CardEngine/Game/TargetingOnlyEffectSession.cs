using System;
using System.Collections.Generic;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Effects;

namespace Assets.Scripts.CardEngine.Game
{
    /// <summary>
    /// Runs ONLY the target-selection phase of an <see cref="ITargetingEffect"/>.
    /// Does not call Resolve/ResolveAfterTargets; intended for lock-in before an interrupt/chain window.
    /// </summary>
    public sealed class TargetingOnlyEffectSession : ITargetingSession
    {
        private readonly Card _card;
        private readonly ITargetingEffect _targetingRoot;
        private readonly EffectContext _context;
        private readonly Action<bool, string> _onFinished;

        private bool _finished;
        private bool _wasCancelled;
        private string _cancelReason;

        public Card Card => _card;

        public bool WasCancelled => _wasCancelled;

        public string CancelReason => _cancelReason;

        public TargetingOnlyEffectSession(Card card, ITargetingEffect targetingRoot, EffectContext context, Action<bool, string> onFinished)
        {
            _card = card ?? throw new ArgumentNullException(nameof(card));
            _targetingRoot = targetingRoot ?? throw new ArgumentNullException(nameof(targetingRoot));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _onFinished = onFinished;

            if (_targetingRoot is IResettableEffect resettable)
                resettable.Reset();
        }

        public bool TryAdvance(out List<ITargetable> candidates)
        {
            candidates = null;

            if (_finished || _wasCancelled)
                return false;

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

            Finish(success: true, cancelReason: null);
            return false;
        }

        public void ProvideTargets(List<ITargetable> targets)
        {
            if (_finished || _wasCancelled || _targetingRoot.IsComplete)
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
