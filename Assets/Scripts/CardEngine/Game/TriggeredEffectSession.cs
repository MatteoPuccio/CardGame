using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Effects;
using Assets.Scripts.CardEngine.Events;

namespace Assets.Scripts.CardEngine.Game
{
    /// <summary>
    /// Resolves a triggered effect with support for target selection and async pre-resolution preparation.
    /// Does not open a rapid chain window (triggered effects are treated as immediate).
    /// </summary>
    public sealed class TriggeredEffectSession : ITargetingSession
    {
        private readonly Card _card;
        private readonly EffectContext _context;
        private readonly Effect _rootEffect;
        private readonly ITargetingEffect _targetingRoot;
        private readonly Action<bool, string> _onFinished;

        private bool _finished;
        private bool _wasCancelled;
        private string _cancelReason;
        private bool _resolutionStarted;

        public Card Card => _card;

        public bool WasCancelled => _wasCancelled;

        public string CancelReason => _cancelReason;

        public TriggeredEffectSession(Card card, Effect rootEffect, EffectContext context, Action<bool, string> onFinished)
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

            if (_resolutionStarted)
                return false;

            if (_rootEffect == null)
            {
                Finish(success: true, cancelReason: null);
                return false;
            }

            if (_targetingRoot == null)
            {
                StartResolutionAsync();
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

            // Target selection is complete; now do async preparation and resolve.
            StartResolutionAsync();
            return false;
        }

        public void ProvideTargets(List<ITargetable> targets)
        {
            if (_finished || _wasCancelled || _targetingRoot == null || _targetingRoot.IsComplete)
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

        private void StartResolutionAsync()
        {
            if (_resolutionStarted)
                return;

            _resolutionStarted = true;
            _ = ResolveAsync();
        }

        private async Task ResolveAsync()
        {
            if (_wasCancelled)
                return;

            try
            {
                // Prep (zone selection prompts etc.)
                var prep = await PreResolvePreparationUtils.PrepareAsync(_rootEffect, _context);
                if (!prep.Ok)
                {
                    Cancel(prep.CancelReason);
                    return;
                }

                if (_wasCancelled)
                    return;

                // Publish activation for debug/event-driven systems.
                var gs = _card?.GameState;
                if (gs?.EventBus != null)
                    gs.EventBus.Publish(new EffectActivatedEvent(source: _card, effect: _rootEffect, activator: _card.Owner));

                if (_targetingRoot != null)
                    _targetingRoot.ResolveAfterTargets(_context);
                else
                    _rootEffect.Resolve(_context);

                Finish(success: true, cancelReason: null);
            }
            catch (Exception ex)
            {
                // Never let exceptions deadlock TriggeredEffectSystem's queue.
                Cancel($"Triggered effect failed: {ex.Message}");
            }
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
