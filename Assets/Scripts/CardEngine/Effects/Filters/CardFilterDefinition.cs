using System;
using Assets.Scripts.CardEngine.Cards;

namespace Assets.Scripts.CardEngine.Effects
{
    public interface ICardFilter
    {
        bool Matches(Card card, EffectContext context);
    }

    [Serializable]
    public abstract class CardFilterDefinition
    {
        public abstract ICardFilter CreateRuntimeFilter();
    }

    internal static class CardFilterUtils
    {
        public static bool MatchesOrTrue(Card card, EffectContext context, ICardFilter filter)
        {
            if (filter == null)
                return true;
            return filter.Matches(card, context);
        }
    }
}
