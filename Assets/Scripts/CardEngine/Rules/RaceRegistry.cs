using System;
using System.Collections.Generic;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Keywords;
using UnityEngine;

namespace Assets.Scripts.CardEngine.Rules
{
    public static class RaceRegistry
    {
        // Optional: create a RaceDefinitionsDatabase asset under a Resources folder
        // and name it "RaceDefinitionsDatabase" to override these defaults.
        private static RaceDefinitionsDatabase _db;

        private static RaceDefinitionsDatabase Db
        {
            get
            {
                if (_db != null)
                    return _db;

                _db = Resources.Load<RaceDefinitionsDatabase>("RaceDefinitionsDatabase");
                return _db;
            }
        }

        public static RaceTrait GetTraits(TroopRaces race)
        {
            if (Db != null && Db.TryGet(race, out var traits, out _))
                return traits;

            // Code defaults (used if no database asset exists).
            return race switch
            {
                TroopRaces.Drake => RaceTrait.ImmuneToEarthSpells,
                _ => RaceTrait.None,
            };
        }

        public static IReadOnlyList<CardKeyword> GetGrantedKeywords(TroopRaces race)
        {
            if (Db != null && Db.TryGet(race, out _, out var keywords))
                return keywords;

            return Array.Empty<CardKeyword>();
        }

        public static string GetDescription(TroopRaces race)
        {
            if (race == TroopRaces.None)
                return string.Empty;

            Db.TryGet(race, out _, out _, out var description);
            return description;
        }
    }
}
