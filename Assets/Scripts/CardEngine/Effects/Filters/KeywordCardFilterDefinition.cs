using System;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Keywords;
using UnityEngine;

namespace Assets.Scripts.CardEngine.Effects
{
    [Serializable]
    public sealed class KeywordCardFilterDefinition : CardFilterDefinition
    {
        [Tooltip("Matches only Troops that have this keyword.")]
        public CardKeyword keyword = CardKeyword.Taunt;

        public override ICardFilter CreateRuntimeFilter() => new KeywordCardFilter(keyword);

        private sealed class KeywordCardFilter : ICardFilter
        {
            private readonly CardKeyword _keyword;

            public KeywordCardFilter(CardKeyword keyword)
            {
                _keyword = keyword;
            }

            public bool Matches(Card card, EffectContext context)
            {
                if (card?.Behavior is not TroopBehavior troop)
                    return false;

                return troop.HasKeyword(_keyword);
            }
        }
    }
}
