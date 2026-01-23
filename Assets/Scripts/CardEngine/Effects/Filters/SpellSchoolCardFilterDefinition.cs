using System;
using Assets.Scripts.CardEngine.Cards;
using UnityEngine;

namespace Assets.Scripts.CardEngine.Effects
{
    [Serializable]
    public sealed class SpellSchoolCardFilterDefinition : CardFilterDefinition
    {
        [Tooltip("Matches only Spells with this school. Use None to match Spells with no school.")]
        public SpellSchool school = SpellSchool.None;

        public override ICardFilter CreateRuntimeFilter() => new SpellSchoolCardFilter(school);

        private sealed class SpellSchoolCardFilter : ICardFilter
        {
            private readonly SpellSchool _school;

            public SpellSchoolCardFilter(SpellSchool school)
            {
                _school = school;
            }

            public bool Matches(Card card, EffectContext context)
            {
                if (card?.Behavior is not SpellBehavior spell)
                    return false;

                return spell.School == _school;
            }
        }
    }
}
