using System;
using Assets.Scripts.CardEngine.Cards;
using UnityEngine;

namespace Assets.Scripts.CardEngine.Effects
{
    [Serializable]
    public sealed class TroopRaceCardFilterDefinition : CardFilterDefinition
    {
        [Tooltip("Matches only Troops with this race. Use None to match Troops with no race.")]
        public TroopRaces race = TroopRaces.None;

        public override ICardFilter CreateRuntimeFilter() => new TroopRaceCardFilter(race);

        private sealed class TroopRaceCardFilter : ICardFilter
        {
            private readonly TroopRaces _race;

            public TroopRaceCardFilter(TroopRaces race)
            {
                _race = race;
            }

            public bool Matches(Card card, EffectContext context)
            {
                if (card?.Behavior is not TroopBehavior troop)
                    return false;

                if (_race == TroopRaces.None)
                    return !troop.HasRace;

                return troop.IsRace(_race);
            }
        }
    }
}
