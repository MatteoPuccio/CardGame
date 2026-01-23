using System;
using Assets.Scripts.CardEngine.Cards;
using UnityEngine;

namespace Assets.Scripts.CardEngine.Effects
{
    [Serializable]
    public sealed class TagCardFilterDefinition : CardFilterDefinition
    {
        [Tooltip("Matches cards with this tag (case-insensitive). Useful for archetypes like 'Dragon', 'Necromancy', etc.")]
        public string tag;

        public override ICardFilter CreateRuntimeFilter() => new TagCardFilter(tag);

        private sealed class TagCardFilter : ICardFilter
        {
            private readonly string _tag;

            public TagCardFilter(string tag)
            {
                _tag = tag;
            }

            public bool Matches(Card card, EffectContext context)
            {
                if (card == null)
                    return false;

                if (string.IsNullOrWhiteSpace(_tag))
                    return false;

                return card.HasTag(_tag);
            }
        }
    }
}
