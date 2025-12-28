using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.CardEngine.Cards;

namespace Assets.Scripts.CardEngine.Utils
{
    public class CardCollection
    {
        private readonly List<Card> _cards;
        private static readonly Random _rng = new();

        public CardCollection(List<Card> cards)
        {
            _cards = cards;
            Shuffle();
        }

        public CardCollection()
        {
            this._cards = new();
        }

        public IReadOnlyList<Card> GetCards => _cards.AsReadOnly();
        public int Count => _cards.Count;


        public void Shuffle()
        {
            int n = _cards.Count;
            while (n > 1)
            {
                n--;
                int k = _rng.Next(n + 1);
                (_cards[n], _cards[k]) = (_cards[k], _cards[n]);
            }
        }

        public void Reset(IEnumerable<Card> cards)
        {
            _cards.Clear();
            _cards.AddRange(cards);
        }

        public void AddCard(Card card)
        {
            _cards.Add(card);
        }

        public bool TakeCard(Card card)
        {
            return _cards.Remove(card);
        }

        public Card FirstCard()
        {
            if (_cards.Count > 0)
            {
                return _cards[0];
            }
            return null;
        }
    }
}
