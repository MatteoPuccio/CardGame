using System;
using Assets.Scripts.CardEngine.Cards;
using UnityEngine;

namespace Assets.Scripts.CardEngine.Effects
{
    [Serializable]
    public sealed class CardTypeCardFilterDefinition : CardFilterDefinition
    {
        [Tooltip("If None, matches all card types.")]
        public CardType cardType = CardType.None;

        public override ICardFilter CreateRuntimeFilter() => new CardTypeCardFilter(cardType);

        private sealed class CardTypeCardFilter : ICardFilter
        {
            private readonly CardType _type;

            public CardTypeCardFilter(CardType type)
            {
                _type = type;
            }

            public bool Matches(Card card, EffectContext context)
            {
                if (_type == CardType.None)
                    return true;
                return card != null && card.Category == _type;
            }
        }
    }
}
