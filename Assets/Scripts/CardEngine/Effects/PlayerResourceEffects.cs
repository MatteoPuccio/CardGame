using System;
using Assets.Scripts.CardEngine.Events;
using UnityEngine;

namespace Assets.Scripts.CardEngine.Effects
{
    public class ModifyDeployPointsEffect : Effect
    {
        private readonly int _amount;
        private readonly AmountDefinition _amountDefinition;

        public ModifyDeployPointsEffect(int amount, AmountDefinition amountDefinition)
        {
            _amount = amount;
            _amountDefinition = amountDefinition;
        }
        protected override void ResolveCore(EffectContext effectContext)
        {
            var player = effectContext.Source?.Owner;
            if (player == null)
                return;

            int amount = _amountDefinition?.Evaluate(effectContext) ?? _amount;
            player.DeployPoints += amount;
        }
    }

    [Serializable]
    public sealed class ModifyDeployPointsEffectDefinition : EffectDefinition
    {
        public int Amount;

        [SerializeReference]
        public AmountDefinition AmountDynamic;

        protected override Effect CreateRuntimeEffectCore() => new ModifyDeployPointsEffect(Amount, AmountDynamic);
    }

    public class ModifyLifeEffect : Effect
    {
        private readonly int _amount;
        private readonly AmountDefinition _amountDefinition;

        public ModifyLifeEffect(int amount, AmountDefinition amountDefinition)
        {
            _amount = amount;
            _amountDefinition = amountDefinition;
        }
        protected override void ResolveCore(EffectContext effectContext)
        {
            var player = effectContext.Source?.Owner;
            if (player == null)
                return;

            uint before = player.Life;

            int amount = _amountDefinition?.Evaluate(effectContext) ?? _amount;

            if (amount < 0 && player.Life < (uint)(-amount))
            {
                player.Life = 0;
            }
            else
            {
                player.Life = (uint)(player.Life + amount);
            }

            if (player.Life != before)
                effectContext.GameState?.EventBus?.Publish(new PlayerLifeChangedEvent(player, before, player.Life, source: effectContext.Source));

            if (before > 0 && player.Life == 0)
                effectContext.GameState?.EventBus?.Publish(new PlayerDefeatedEvent(player, source: effectContext.Source));
        }
    }

    [Serializable]
    public sealed class ModifyLifeEffectDefinition : EffectDefinition
    {
        public int Amount;

        [SerializeReference]
        public AmountDefinition AmountDynamic;

        protected override Effect CreateRuntimeEffectCore() => new ModifyLifeEffect(Amount, AmountDynamic);
    }
}