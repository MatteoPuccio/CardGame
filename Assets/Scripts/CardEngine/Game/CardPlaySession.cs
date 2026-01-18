using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Effects;
using Assets.Scripts.CardEngine.Events;

namespace Assets.Scripts.CardEngine.Game
{
    public sealed class CardPlaySession : ITargetingSession
    {
        private readonly Card _card;
        private readonly ICardZone _sourceZone;
        private readonly EffectContext _context;
        private readonly CardPlayedEvent _cardPlayedEvent;

        private bool _wasCancelled;
        private string _cancelReason;

        private readonly Effect _rootEffect;
        private readonly ITargetingEffect _targetingRoot;
        private bool _publishedActivation;
        private bool _resolutionStarted;

        public Card Card => _card;

        public bool WasCancelled => _wasCancelled;
        public string CancelReason => _cancelReason;

        public CardPlaySession(Card card, ICardZone sourceZone, EffectContext context)
        {
            _card = card ?? throw new ArgumentNullException(nameof(card));
            _sourceZone = sourceZone;
            _context = context ?? throw new ArgumentNullException(nameof(context));

            _cardPlayedEvent = new CardPlayedEvent(card: _card, player: _card.Owner);

            _rootEffect = _card.OnPlayEffect;
            _targetingRoot = _rootEffect as ITargetingEffect;

            if (_rootEffect is IResettableEffect resettable)
                resettable.Reset();
        }

        public bool TryAdvance(out List<ITargetable> candidates)
        {
            candidates = null;

            if (_wasCancelled)
                return false;

            if (_resolutionStarted)
                return false;

            if (_rootEffect == null)
            {
                StartResolutionAsync(triggeringEvent: _cardPlayedEvent);
                return false;
            }

            if (_targetingRoot == null)
            {
                // For card plays we treat the chain window as responding to the play/activation declaration.
                StartResolutionAsync(triggeringEvent: _cardPlayedEvent);
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

            // Target selection is complete (all player selections done).
            // Targets are now locked in. Open the chain window before resolving the effect.
            StartResolutionAsync(triggeringEvent: _cardPlayedEvent);
            return false;
        }

        public void Cancel(string reason = null)
        {
            _wasCancelled = true;
            _cancelReason = string.IsNullOrWhiteSpace(reason) ? "Cancelled." : reason;
        }

        private void StartResolutionAsync(IGameEvent triggeringEvent)
        {
            if (_resolutionStarted)
                return;

            _resolutionStarted = true;

            // Resolution may await rapid-effect UI; don't block the caller.
            _ = ResolveWithInterruptsAsync(triggeringEvent);
        }

        private async Task ResolveWithInterruptsAsync(IGameEvent triggeringEvent)
        {
            if (_wasCancelled)
                return;

            var chain = _card?.GameState?.RapidEffectChain;
            if (chain != null && triggeringEvent != null)
            {
                try
                {
                    await chain.TryOpenChainWindowAsync(triggeringEvent);
                }
                catch
                {
                    // Chain UI failures should not block resolution.
                }
            }

            if (_wasCancelled)
                return;

            // Publish activation only after the interrupt window, so the flow is:
            // target selection (if any) -> chain window -> resolve.
            PublishActivationIfNeeded();

            if (_targetingRoot != null)
                _targetingRoot.ResolveAfterTargets(_context);
            else
                _rootEffect?.Resolve(_context);
            Finish();
        }

        private void PublishActivationIfNeeded()
        {
            if (_publishedActivation)
                return;

            _publishedActivation = true;

            var gs = _card?.GameState;
            if (gs?.EventBus == null)
                return;

            gs.EventBus.Publish(_cardPlayedEvent);

            if (_rootEffect != null)
                gs.EventBus.Publish(new EffectActivatedEvent(source: _card, effect: _rootEffect, activator: _card.Owner));
        }

        public void ProvideTargets(List<ITargetable> targets)
        {
            if (_targetingRoot == null || _targetingRoot.IsComplete)
                return;

            _targetingRoot.ApplyTargets(_context, targets);
        }

        private void Finish()
        {
            if (_wasCancelled)
                return;

            _card.FinishPlay(_context, _sourceZone);
        }
    }
}
