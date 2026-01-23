using System;
using Assets.Scripts.CardEngine.Cards;
using UnityEngine;

namespace Assets.Scripts.CardEngine.Effects
{
    /// <summary>
    /// Adds/removes troop stats on the targeted troop cards.
    /// HealthDelta modifies MaxHealth, and can optionally also heal by the gained max health.
    /// </summary>
    public sealed class BuffTroopEffect : Effect
    {
        private readonly int _powerDelta;
        private readonly AmountDefinition _powerDeltaDynamic;

        private readonly int _healthDelta;
        private readonly AmountDefinition _healthDeltaDynamic;

        private readonly bool _healAddedHealth;

        public BuffTroopEffect(
            int powerDelta,
            AmountDefinition powerDeltaDynamic,
            int healthDelta,
            AmountDefinition healthDeltaDynamic,
            bool healAddedHealth)
        {
            _powerDelta = powerDelta;
            _powerDeltaDynamic = powerDeltaDynamic;
            _healthDelta = healthDelta;
            _healthDeltaDynamic = healthDeltaDynamic;
            _healAddedHealth = healAddedHealth;
        }

        public override bool CanActivate(EffectContext context, out string reason)
        {
            if (!base.CanActivate(context, out reason))
                return false;

            if (context?.Targets == null || context.Targets.Count == 0)
            {
                reason = "Missing troop target.";
                return false;
            }

            for (int i = 0; i < context.Targets.Count; i++)
            {
                if (context.Targets[i] is Card c && c.Behavior is TroopBehavior)
                {
                    reason = null;
                    return true;
                }
            }

            reason = "No troop targets.";
            return false;
        }

        protected override void ResolveCore(EffectContext effectContext)
        {
            int powerDelta = _powerDeltaDynamic?.Evaluate(effectContext) ?? _powerDelta;
            int healthDelta = _healthDeltaDynamic?.Evaluate(effectContext) ?? _healthDelta;

            if (effectContext?.Targets == null)
                return;

            foreach (var target in effectContext.Targets)
            {
                if (target is not Card card)
                    continue;

                if (card.Behavior is not TroopBehavior troop)
                    continue;

                troop.ModifyStats(powerDelta, healthDelta, healAddedHealth: _healAddedHealth);

                if (Debug.isDebugBuild)
                {
                    Debug.Log($"BuffTroopEffect: {card.Name} ({(powerDelta >= 0 ? "+" : string.Empty)}{powerDelta} P, {(healthDelta >= 0 ? "+" : string.Empty)}{healthDelta} HPmax)");
                }
            }
        }
    }

    [Serializable]
    public sealed class BuffTroopEffectDefinition : EffectDefinition
    {
        [Header("Power")]
        public int PowerDelta;

        [SerializeReference]
        public AmountDefinition PowerDeltaDynamic;

        [Header("Health (MaxHealth)")]
        public int HealthDelta;

        [SerializeReference]
        public AmountDefinition HealthDeltaDynamic;

        [Tooltip("If true and HealthDelta is positive, current health increases by the gained max health.")]
        public bool HealAddedHealth = true;

        protected override Effect CreateRuntimeEffectCore()
            => new BuffTroopEffect(PowerDelta, PowerDeltaDynamic, HealthDelta, HealthDeltaDynamic, HealAddedHealth);
    }
}
