using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Effects;
using Assets.Scripts.CardEngine.Rules;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.CardEngine.Effects
{
    public class TargetedEffect : Effect, ITargetingEffect, IResettableEffect, ICompositeEffect
    {
        private readonly IEffectSelector _selector;
        private readonly Effect _effect;

        private readonly ITargetingEffect _childTargeting;

        private enum Stage
        {
            NeedTargets,
            NeedChildTargets,
            Complete,
        }

        private Stage _stage = Stage.NeedTargets;
        private List<ITargetable> _chosenTargets;

        public bool IsComplete => _stage == Stage.Complete;

        public void Reset()
        {
            _stage = Stage.NeedTargets;
            _chosenTargets = null;

            if (_effect is IResettableEffect resettable)
                resettable.Reset();
        }

        public TargetedEffect(IEffectSelector selector, Effect effect)
        {
            _selector = selector;
            _effect = effect;
            _childTargeting = effect as ITargetingEffect;
        }

        public IEnumerable<Effect> GetChildEffects()
        {
            if (_effect != null)
                yield return _effect;
        }

        protected override void ResolveCore(EffectContext effectContext)
        {
            ResolveAfterTargets(effectContext);
        }

        public void ResolveAfterTargets(EffectContext context)
        {
            if (context == null)
                return;

            var previousTargets = context.Targets;
            try
            {
                List<ITargetable> targetsToUse;
                if (_chosenTargets != null)
                    targetsToUse = _chosenTargets;
                else if (previousTargets != null && previousTargets.Count > 0)
                    targetsToUse = previousTargets;
                else
                    targetsToUse = _selector?.Select(context) ?? new List<ITargetable>();

                targetsToUse = CardApplicabilityRules.FilterTargets(context, targetsToUse);

                context.Targets = targetsToUse;

                if (_effect is ITargetingEffect targeting)
                {
                    targeting.ResolveAfterTargets(context);
                    return;
                }

                _effect?.Resolve(context);
            }
            finally
            {
                context.Targets = previousTargets;
            }
        }

        public List<ITargetable> GetCandidates(EffectContext effectContext)
        {
            var candidates = _selector.Select(effectContext);
            return CardApplicabilityRules.FilterTargets(effectContext, candidates);
        }

        public bool TryGetTargetRequest(EffectContext context, out List<ITargetable> candidates, out string cancelReason)
        {
            candidates = null;
            cancelReason = null;

            if (IsComplete)
                return false;

            if (context == null)
            {
                _stage = Stage.Complete;
                return false;
            }

            if (_stage == Stage.NeedTargets && TryHandleNeedTargetsStage(context, out candidates, out cancelReason))
                return true;

            if (!string.IsNullOrEmpty(cancelReason))
                return false;

            return TryHandleChildTargetingStage(context, out candidates, out cancelReason);
        }

        private bool TryHandleNeedTargetsStage(EffectContext context, out List<ITargetable> candidates, out string cancelReason)
        {
            candidates = null;
            cancelReason = null;

            if (_chosenTargets != null)
            {
                _stage = _childTargeting != null ? Stage.NeedChildTargets : Stage.Complete;
                return false;
            }

            candidates = _selector?.Select(context);
            candidates = CardApplicabilityRules.FilterTargets(context, candidates);
            bool requiresPlayerChoice = _selector is IPlayerChoiceSelector choice && choice.RequiresPlayerChoice;

            if (requiresPlayerChoice && (candidates == null || candidates.Count == 0))
            {
                _stage = Stage.Complete;
                cancelReason = "No valid targets.";
                return false;
            }

            if (candidates != null && candidates.Count > 0)
            {
                if (!requiresPlayerChoice)
                {
                    _chosenTargets = candidates;
                    _stage = _childTargeting != null ? Stage.NeedChildTargets : Stage.Complete;
                    return false;
                }

                // Player needs to pick from these candidates.
                return true;
            }

            // No candidates but also no player choice required: treat as empty selection and continue.
            _chosenTargets = new List<ITargetable>();
            _stage = _childTargeting != null ? Stage.NeedChildTargets : Stage.Complete;
            return false;
        }

        public void ApplyTargets(EffectContext context, List<ITargetable> targets)
        {
            if (IsComplete || context == null)
                return;

            if (_stage == Stage.NeedTargets)
            {
                _chosenTargets = CardApplicabilityRules.FilterTargets(context, targets ?? new List<ITargetable>());
                _stage = _childTargeting != null ? Stage.NeedChildTargets : Stage.Complete;
                return;
            }

            if (_stage == Stage.NeedChildTargets && _childTargeting != null && !_childTargeting.IsComplete)
            {
                var previousTargets = context.Targets;
                try
                {
                    context.Targets = _chosenTargets;
                    _childTargeting.ApplyTargets(context, targets);
                }
                finally
                {
                    context.Targets = previousTargets;
                }
            }
        }

        private bool TryHandleChildTargetingStage(EffectContext context, out List<ITargetable> candidates, out string cancelReason)
        {
            candidates = null;
            cancelReason = null;

            if (_childTargeting == null)
            {
                _stage = Stage.Complete;
                return false;
            }

            var previousTargets = context.Targets;
            try
            {
                context.Targets = _chosenTargets;

                while (!_childTargeting.IsComplete)
                {
                    if (_childTargeting.TryGetTargetRequest(context, out candidates, out cancelReason))
                        return true;

                    if (!string.IsNullOrEmpty(cancelReason))
                    {
                        _stage = Stage.Complete;
                        return false;
                    }
                }

                _stage = Stage.Complete;
                return false;
            }
            finally
            {
                context.Targets = previousTargets;
            }
        }
    }

    [Serializable]
    public sealed class TargetedEffectDefinition : EffectDefinition
    {
        [SerializeReference] [SerializeField] private EffectSelectorDefinition _selector;
        [SerializeReference] [SerializeField] private EffectDefinition _effect;

        protected override Effect CreateRuntimeEffectCore()
        {
            IEffectSelector selector = _selector != null ? _selector.CreateRuntimeSelector() : null;
            Effect effect = _effect != null ? _effect.CreateRuntimeEffect() : null;

            if (selector == null || effect == null)
                return null;

            return new TargetedEffect(selector, effect);
        }
    }
}