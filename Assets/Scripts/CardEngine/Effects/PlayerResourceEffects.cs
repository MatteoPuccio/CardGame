using System;
using Assets.Scripts.CardEngine.Events;

namespace Assets.Scripts.CardEngine.Effects
{
    public class ModifyDeployPointsEffect : Effect
    {
        private readonly int _amount;
        public ModifyDeployPointsEffect(int amount)
        {
            _amount = amount;
        }
        protected override void ResolveCore(EffectContext effectContext)
        {
            var player = effectContext.Source?.Owner;
            if (player == null)
                return;
            player.DeployPoints += _amount;
        }
    }

    [Serializable]
    public sealed class ModifyDeployPointsEffectDefinition : EffectDefinition
    {
        public int Amount;

        public override Effect CreateRuntimeEffect() => new ModifyDeployPointsEffect(Amount);
    }

    public class ModifyLifeEffect : Effect
    {
        private readonly int _amount;
        public ModifyLifeEffect(int amount)
        {
            _amount = amount;
        }
        protected override void ResolveCore(EffectContext effectContext)
        {
            var player = effectContext.Source?.Owner;
            if (player == null)
                return;

            uint before = player.Life;

            if (_amount < 0 && player.Life < (uint)(-_amount))
            {
                player.Life = 0;
            }
            else
            {
                player.Life = (uint)(player.Life + _amount);
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

        public override Effect CreateRuntimeEffect() => new ModifyLifeEffect(Amount);
    }
}