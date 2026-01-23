using System;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Rules;

namespace Assets.Scripts.CardEngine.Effects
{
    public sealed class ChangeRaceEffect : Effect
    {
        private readonly TroopRaces _newRace;

        public ChangeRaceEffect(TroopRaces newRace)
        {
            _newRace = newRace;
        }

        protected override void ResolveCore(EffectContext effectContext)
        {
            if (effectContext?.Targets == null)
                return;

            for (int i = 0; i < effectContext.Targets.Count; i++)
            {
                if (effectContext.Targets[i] is not Card card)
                    continue;

                if (card.Behavior is not TroopBehavior troop)
                    continue;

                troop.SetRace(_newRace);

                var raceKeywords = _newRace == TroopRaces.None ? null : RaceRegistry.GetGrantedKeywords(_newRace);
                troop.SetRaceKeywords(raceKeywords);
            }
        }
    }

    [Serializable]
    public sealed class ChangeRaceEffectDefinition : EffectDefinition
    {
        public TroopRaces NewRace = TroopRaces.None;

        protected override Effect CreateRuntimeEffectCore() => new ChangeRaceEffect(NewRace);
    }
}
