using System;
using System.Collections.Generic;

namespace Assets.Scripts.CardEngine.Rules
{
    public static class RaceTraitDescriptions
    {
        public static IEnumerable<RaceTrait> EnumerateFlags(RaceTrait traits)
        {
            if (traits == RaceTrait.None)
                yield break;

            foreach (RaceTrait flag in Enum.GetValues(typeof(RaceTrait)))
            {
                if (flag == RaceTrait.None)
                    continue;

                if ((traits & flag) != 0)
                    yield return flag;
            }
        }

        public static string GetDisplayName(RaceTrait trait)
        {
            return trait.ToString();
        }

        public static string GetDescription(RaceTrait trait)
        {
            return trait switch
            {
                RaceTrait.ImmuneToEarthSpells => "Unaffected by Earth spells.",
                RaceTrait.ImmuneToAirSpells => "Unaffected by Air spells.",
                RaceTrait.ImmuneToFireSpells => "Unaffected by Fire spells.",
                RaceTrait.ImmuneToWaterSpells => "Unaffected by Water spells.",
                RaceTrait.ImmuneToChaosSpells => "Unaffected by Chaos spells.",
                RaceTrait.ImmuneToOrderSpells => "Unaffected by Order spells.",
                _ => string.Empty,
            };
        }
    }
}
