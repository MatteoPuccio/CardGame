using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.CardEngine.Cards
{
    [CreateAssetMenu(fileName = "Deck", menuName = "ScriptableObjects/Deck", order = 2)]
    public sealed class ScriptableDeck : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            [SerializeField] private ScriptableCard _card;
            [Min(1)] [SerializeField] private int _count = 1;

            public ScriptableCard Card => _card;
            public int Count => _count;
        }

        [SerializeField] private List<Entry> _cards = new();

        public IReadOnlyList<Entry> Cards => _cards;
    }
}
