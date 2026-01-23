using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Effects;
using Assets.Scripts.CardEngine.Game;
using System;

namespace Assets.Scripts.CardEngine.Effects
{
    public class DestroyEffect : Effect
    {
        protected override void ResolveCore(EffectContext effectContext)
        {
            foreach (var target in effectContext.Targets)
            {
                if (target is Card card)
                {
					effectContext.GameState?.DestroyCard(card, effectContext.Source);
                }
            }
        }
    }

    [Serializable]
    public sealed class DestroyEffectDefinition : EffectDefinition
    {
        protected override Effect CreateRuntimeEffectCore() => new DestroyEffect();
    }
}

