using System;

namespace Assets.Scripts.CardEngine.Rules
{
    [Flags]
    public enum RaceTrait
    {
        None = 0,
        ImmuneToEarthSpells = 1 << 0,
        ImmuneToAirSpells = 1 << 1,
        ImmuneToFireSpells = 1 << 2,
        ImmuneToWaterSpells = 1 << 3,
        ImmuneToChaosSpells = 1 << 4,
        ImmuneToOrderSpells = 1 << 5,
    }
}
