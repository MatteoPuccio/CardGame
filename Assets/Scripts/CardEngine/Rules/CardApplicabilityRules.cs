using System.Collections.Generic;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Effects;

namespace Assets.Scripts.CardEngine.Rules
{
    public static class CardApplicabilityRules
    {
        public static List<ITargetable> FilterTargets(EffectContext context, List<ITargetable> targets)
        {
            if (context?.Source == null || targets == null || targets.Count == 0)
                return targets ?? new List<ITargetable>();

            // For now: only spell-driven immunities.
            if (context.Source.Category != CardType.Spell)
                return targets;

            if (context.Source.Behavior is not SpellBehavior spell)
                return targets;

            if (spell.School == SpellSchool.None)
                return targets;

            for (int i = targets.Count - 1; i >= 0; i--)
            {
                if (!IsAffectedBySpellSchool(targets[i], spell.School))
                    targets.RemoveAt(i);
            }

            return targets;
        }

        private static bool IsAffectedBySpellSchool(ITargetable target, SpellSchool school)
        {
            if (target is not Card card)
                return true;

            if (card.Behavior is not TroopBehavior troop)
                return true;

            if (!troop.HasRace)
                return true;

            var traits = RaceRegistry.GetTraits(troop.Race);

            var immunityFlag = GetImmunityTraitForSchool(school);
            if (immunityFlag == RaceTrait.None)
                return true;

            return (traits & immunityFlag) == 0;
        }

        private static RaceTrait GetImmunityTraitForSchool(SpellSchool school)
        {
            return school switch
            {
                SpellSchool.Earth => RaceTrait.ImmuneToEarthSpells,
                SpellSchool.Air => RaceTrait.ImmuneToAirSpells,
                SpellSchool.Fire => RaceTrait.ImmuneToFireSpells,
                SpellSchool.Water => RaceTrait.ImmuneToWaterSpells,
                SpellSchool.Chaos => RaceTrait.ImmuneToChaosSpells,
                SpellSchool.Order => RaceTrait.ImmuneToOrderSpells,
                _ => RaceTrait.None,
            };
        }
    }
}
