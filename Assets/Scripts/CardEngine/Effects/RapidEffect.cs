using System;
using System.Collections.Generic;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Game;
using Assets.Scripts.CardEngine.Utils;
using UnityEngine;

namespace Assets.Scripts.CardEngine.Effects
{
    public enum RapidEffectActivationFrequency
    {
        Whenever = 0,
        OncePerTurn = 1,
    }

    public sealed class RapidEffectContext : EffectContext
    {
        public Player Activator { get; set; }

        /// <summary>
        /// Optional helper: if this rapid effect is being activated in response to something,
        /// this is the effect/event that opened the window.
        /// </summary>
        public object ChainWindowSource { get; set; }
    }

    [Serializable]
    public sealed class RapidEffect : Effect, ICostPayingEffect, ICompositeEffect
    {
        public Effect InnerEffect { get; }
        public IReadOnlyList<IRapidEffectCondition> Conditions { get; }

        public RapidEffectActivationFrequency ActivationFrequency { get; }

        private int _lastActivatedTurnNumber = int.MinValue;

        public RapidEffect(
            Effect innerEffect,
            IReadOnlyList<IRapidEffectCondition> conditions,
            RapidEffectActivationFrequency activationFrequency = RapidEffectActivationFrequency.Whenever)
        {
            InnerEffect = innerEffect;
            Conditions = conditions;
            ActivationFrequency = activationFrequency;
        }

        public IEnumerable<Effect> GetChildEffects()
        {
            if (InnerEffect != null)
                yield return InnerEffect;
        }

        public override bool IsOncePerTurn =>
            ActivationFrequency == RapidEffectActivationFrequency.OncePerTurn || (InnerEffect?.IsOncePerTurn ?? false);

        public override bool CanActivate(EffectContext context, out string reason)
        {
            if (context is not RapidEffectContext rapidContext)
            {
                reason = "Rapid effect requires a RapidEffectContext.";
                return false;
            }

            if (InnerEffect == null)
            {
                reason = "Missing inner effect.";
                return false;
            }

            if (IsOncePerTurn)
            {
                if (rapidContext.GameState == null)
                {
                    reason = "Missing game state.";
                    return false;
                }

                if (_lastActivatedTurnNumber == rapidContext.GameState.TurnNumber)
                {
                    reason = "Rapid effect is once per turn.";
                    return false;
                }
            }

            if (Conditions != null)
            {
                for (int i = 0; i < Conditions.Count; i++)
                {
                    var c = Conditions[i];
                    if (c != null && !c.CanActivate(rapidContext, out reason))
                        return false;
                }
            }

            return InnerEffect.CanActivate(context, out reason);
        }

        public bool TryPayCost(EffectContext context, out string reason)
        {
            if (InnerEffect == null)
            {
                reason = "Missing inner effect.";
                return false;
            }

            if (InnerEffect is ICostPayingEffect costPaying)
            {
                bool ok = costPaying.TryPayCost(context, out reason);
                if (ok)
                    ConsumeOncePerTurnIfNeeded(context);
                return ok;
            }

            reason = null;
            ConsumeOncePerTurnIfNeeded(context);
            return true;
        }

        private void ConsumeOncePerTurnIfNeeded(EffectContext context)
        {
            if (!IsOncePerTurn)
                return;

            int turn = context?.GameState?.TurnNumber ?? int.MinValue;
            _lastActivatedTurnNumber = turn;
        }

        protected override void ResolveCore(EffectContext effectContext)
        {
            InnerEffect?.Resolve(effectContext);
        }
    }

    [Serializable]
    public abstract class RapidEffectDefinition : EffectDefinition
    {
        [Tooltip("Whether this rapid effect can be activated whenever or only once per turn.")]
        public RapidEffectActivationFrequency activationFrequency = RapidEffectActivationFrequency.Whenever;

        [Tooltip("If true, the owning player may choose to activate this rapid effect or skip it.")]
        public bool optional;

        protected sealed override Effect CreateRuntimeEffectCore() => CreateRuntimeRapidEffect();

        public abstract RapidEffect CreateRuntimeRapidEffect();
    }

    [Serializable]
    public sealed class WrappedRapidEffectDefinition : RapidEffectDefinition
    {
        [SerializeReference] public EffectDefinition effect;

        [Tooltip("Optional activation conditions. All conditions must pass for the rapid effect to be activatable.")]
        [SerializeReference] public List<RapidEffectConditionDefinition> conditions = new();

        public override RapidEffect CreateRuntimeRapidEffect()
        {
            var runtimeEffect = effect != null ? effect.CreateRuntimeEffect() : null;

            var runtimeConditions = new List<IRapidEffectCondition>();
            if (conditions != null)
            {
                for (int i = 0; i < conditions.Count; i++)
                {
                    var def = conditions[i];
                    var c = def != null ? def.CreateRuntimeCondition() : null;
                    if (c != null)
                        runtimeConditions.Add(c);
                }
            }

            if (runtimeEffect == null)
                return null;

            var rapid = new RapidEffect(runtimeEffect, runtimeConditions, activationFrequency);
            rapid.IsOptional = optional;
            return rapid;
        }
    }

}
