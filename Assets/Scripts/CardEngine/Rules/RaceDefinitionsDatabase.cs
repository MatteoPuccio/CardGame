using System;
using System.Collections.Generic;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Keywords;
using UnityEngine;

namespace Assets.Scripts.CardEngine.Rules
{
    [CreateAssetMenu(fileName = "RaceDefinitionsDatabase", menuName = "ScriptableObjects/CardEngine/Race Definitions Database", order = 20)]
    public sealed class RaceDefinitionsDatabase : ScriptableObject
    {
        [Serializable]
        public sealed class RaceDefinition
        {
            public TroopRaces race;
            public RaceTrait traits = RaceTrait.None;
            public List<CardKeyword> grantedKeywords = new();

            [TextArea]
            public string description;
        }

        [SerializeField] private List<RaceDefinition> _definitions = new();

        public bool TryGet(TroopRaces race, out RaceTrait traits, out IReadOnlyList<CardKeyword> grantedKeywords, out string description)
        {
            if (_definitions != null)
            {
                for (int i = 0; i < _definitions.Count; i++)
                {
                    var def = _definitions[i];
                    if (def == null)
                        continue;
                    if (def.race != race)
                        continue;

                    traits = def.traits;
                    grantedKeywords = def.grantedKeywords != null ? def.grantedKeywords : Array.Empty<CardKeyword>();
                    description = def.description;
                    return true;
                }
            }

            traits = RaceTrait.None;
            grantedKeywords = Array.Empty<CardKeyword>();
            description = null;
            return false;
        }

        public bool TryGet(TroopRaces race, out RaceTrait traits, out IReadOnlyList<CardKeyword> grantedKeywords)
        {
            return TryGet(race, out traits, out grantedKeywords, out _);
        }
    }
}
