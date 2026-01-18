using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Effects;
using Assets.Scripts.CardEngine.Game;
using System;
using UnityEngine;

namespace Assets.Scripts.CardEngine.Effects
{
    [System.Serializable]
    public class DrawEffect : Effect
    {
        private readonly int _drawNumber;
        public DrawEffect(int drawNumber = 1) 
        {
            if (drawNumber <= 0)
                throw new ArgumentOutOfRangeException(nameof(drawNumber), "Draw number must be greater than zero.");
            _drawNumber = drawNumber;
        }
        protected override void ResolveCore(EffectContext effectContext)
        {
            if (effectContext.Source?.Owner == null)
                return;

            if (effectContext.Targets.Count == 0)
            {
                Player owner = effectContext.Source.Owner;
                for (int i = 0; i < _drawNumber; i++)
                    owner.Deck.DrawTop();
            } else {
                foreach (ITargetable target in effectContext.Targets)
                {
                    if (target is Player player)
                    {
                        for (int i = 0; i < _drawNumber; i++) 
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

        public override Effect CreateRuntimeEffect() => new DrawEffect(_drawNumber);
    }
}

