using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Effects;
using Assets.Scripts.CardEngine.Game;
using System;
using UnityEngine;

namespace Assets.Scripts.CardEngine.Effects
{
    public class DamageTroopEffect : Effect
    {
        private readonly int _damageAmount;
        private readonly AmountDefinition _damageAmountDefinition;

        public DamageTroopEffect(int damageAmount, AmountDefinition damageAmountDefinition)
        {
            _damageAmount = damageAmount;
            _damageAmountDefinition = damageAmountDefinition;
        }

        public override bool CanActivate(EffectContext context, out string reason)
        {
            if (!base.CanActivate(context, out reason))
                return false;

            if (context.Targets.Count == 0)
            {
                reason = "Missing troop target.";
                return false;
            }

            reason = null;
            return true;
        }

        protected override void ResolveCore(EffectContext effectContext)
        {
            int damageAmount = _damageAmountDefinition?.Evaluate(effectContext) ?? _damageAmount;
            if (damageAmount < 0)
                damageAmount = 0;

            foreach (var target in effectContext.Targets)
            {
                if (target is Card card && card.Behavior is TroopBehavior)
                {
					// Use GameState pipeline so damage/death triggers and zone moves happen consistently.
					effectContext.GameState?.ApplyDamage(card, damageAmount, instigator: effectContext.Source);
                }
            }
        }
    }

    [Serializable]
    public sealed class DamageTroopEffectDefinition : EffectDefinition
    {
        public int DamageAmount;

        [SerializeReference]
        public AmountDefinition DamageAmountDynamic;

        protected override Effect CreateRuntimeEffectCore() => new DamageTroopEffect(DamageAmount, DamageAmountDynamic);
    }
}

