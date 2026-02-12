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


        [Header("Main Deck")]
        [Tooltip("Cards in the main deck. Players draw from this deck.")]
        [SerializeField] private List<Entry> _mainDeckCards = new();

        [Header("Extra Deck")]
        [Tooltip("Cards in the extra deck. Should contain only Ritual and Avatar cards.")]
        [SerializeField] private List<Entry> _extraDeckCards = new();

        /// <summary>
        /// Entries explicitly configured for the main deck.
        /// </summary>
        public IReadOnlyList<Entry> MainDeckCards => _mainDeckCards;

        /// <summary>
        /// Entries explicitly configured for the extra deck.
        /// </summary>
        public IReadOnlyList<Entry> ExtraDeckCards => _extraDeckCards;
    }
}
