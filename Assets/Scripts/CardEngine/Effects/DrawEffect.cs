using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Game;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.CardEngine.Effects
{
    [System.Serializable]
    public class DrawEffect : Effect
    {
        private readonly int _drawNumber;
        private readonly AmountDefinition _drawAmountDefinition;

        public DrawEffect(int drawNumber, AmountDefinition drawAmountDefinition) 
        {
            _drawNumber = drawNumber;
            _drawAmountDefinition = drawAmountDefinition;
        }
        protected override void ResolveCore(EffectContext effectContext)
        {
            if (effectContext.Source?.Owner == null)
                return;

            int drawCount = _drawAmountDefinition?.Evaluate(effectContext) ?? _drawNumber;
            if (drawCount <= 0)
                return;

            var targets = effectContext.Targets;
            if (targets == null || targets.Count == 0)
            {
                Player owner = effectContext.Source.Owner;
                for (int i = 0; i < drawCount; i++)
                    owner.Deck.DrawTop();
            } else {
                foreach (ITargetable target in targets)
                {
                    if (target is Player player)
                    {
                        for (int i = 0; i < drawCount; i++) 
                            player.Deck.DrawTop();
                    }
                }
            }
            
        }
    }

    [Serializable]
    public sealed class DrawEffectDefinition : EffectDefinition
    {
        [Min(1)]
        [SerializeField] private int _drawNumber = 1;

        [SerializeReference]
        [SerializeField] private AmountDefinition _drawAmountDynamic;

        protected override Effect CreateRuntimeEffectCore() => new DrawEffect(_drawNumber, _drawAmountDynamic);
    }
}

