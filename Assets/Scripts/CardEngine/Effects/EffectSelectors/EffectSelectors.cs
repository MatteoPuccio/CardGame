using Assets.Scripts.CardEngine.Cards;
using System.Collections.Generic;

namespace Assets.Scripts.CardEngine.Effects
{
    public interface IEffectSelector
    {
        List<ITargetable> Select(EffectContext context);
    }

    public class AllEnemyCharactersSelector : IEffectSelector
    {
        public List<ITargetable> Select(EffectContext context)
        {
            return context.GameState.GetEnemyCharacters(context.Source.Owner);
        }
    }
}