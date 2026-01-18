using Assets.Scripts.CardEngine.Cards;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.CardEngine.Effects
{
    /// <summary>
    /// Runs a list of effects in order. If any effect needs targeting, the sequence pauses and resumes.
    /// </summary>
    public sealed class SequentialEffect : Effect, ITargetingEffect, IResettableEffect
    {
        private readonly List<Effect> _effects;
        private int _index;
        private bool _complete;

        public bool IsComplete => _complete;

        public void Reset()
        {
            _index = 0;
            _complete = false;

            if (_effects == null)
                return;

            for (int i = 0; i < _effects.Count; i++)
            {
                if (_effects[i] is IResettableEffect resettable)
                    resettable.Reset();
            }
        }

        public SequentialEffect(List<Effect> effects)
        {
            _effects = effects ?? new List<Effect>();
        }

        protected override void ResolveCore(EffectContext effectContext)
        {
            ResolveAfterTargets(effectContext);
        }

        public void ResolveAfterTargets(EffectContext context)
        {
            if (context == null)
                return;

            if (_effects == null)
                return;

            for (int i = 0; i < _effects.Count; i++)
            {
                var eff = _effects[i];
                if (eff == null)
                    continue;

                if (eff is ITargetingEffect targeting)
                {
                    targeting.ResolveAfterTargets(context);
                    continue;
                }

                eff.Resolve(context);
            }
        }

        public bool TryGetTargetRequest(EffectContext context, out List<ITargetable> candidates, out string cancelReason)
        {
            candidates = null;
            cancelReason = null;

            if (_complete)
                return false;

            if (context == null)
            {
                _complete = true;
                return false;
            }

            while (_index < _effects.Count)
            {
                var current = _effects[_index];
                if (current == null)
                {
                    _index++;
                    continue;
                }

                if (current is ITargetingEffect targeting)
                    return TryHandleTargetingEffect(targeting, context, out candidates, out cancelReason);

                // Non-targeting effects do not need any selection; skip during the targeting phase.
                _index++;
            }

            _complete = true;
            return false;
        }

        private bool TryHandleTargetingEffect(
            ITargetingEffect targeting,
            EffectContext context,
            out List<ITargetable> candidates,
            out string cancelReason)
        {
            candidates = null;
            cancelReason = null;

            if (targeting == null)
            {
                _index++;
                return false;
            }

            if (targeting.IsComplete)
            {
                _index++;
                return false;
            }

            if (targeting.TryGetTargetRequest(context, out candidates, out cancelReason))
                return true;

            if (!string.IsNullOrEmpty(cancelReason))
            {
                _complete = true;
                return false;
            }

            // Targeting effect advanced/resolved without needing input.
            if (targeting.IsComplete)
            {
                _index++;
                return false;
            }

            // Defensive: avoid infinite loops if an effect doesn't progress.
            _complete = true;
            return false;
        }

        public void ApplyTargets(EffectContext context, List<ITargetable> targets)
        {
            if (_complete || context == null)
                return;

            while (_index < _effects.Count)
            {
                var current = _effects[_index];
                if (current == null)
                {
                    _index++;
                    continue;
                }

                if (current is ITargetingEffect targeting && !targeting.IsComplete)
                {
                    targeting.ApplyTargets(context, targets);

                    if (targeting.IsComplete)
                        _index++;
                    return;
                }

                _index++;
            }

            _complete = true;
        }
    }

    [Serializable]
    public sealed class SequentialEffectDefinition : EffectDefinition
    {
        [SerializeReference]
        [SerializeField] private List<EffectDefinition> _effects = new();

        public override Effect CreateRuntimeEffect()
        {
            var runtime = new List<Effect>();
            if (_effects != null)
            {
                for (int i = 0; i < _effects.Count; i++)
                {
                    var def = _effects[i];
                    if (def == null)
                        continue;

                    var eff = def.CreateRuntimeEffect();
                    if (eff != null)
                        runtime.Add(eff);
                }
            }

            return new SequentialEffect(runtime);
        }
    }
}
