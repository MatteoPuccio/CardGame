using System;
using System.Collections.Generic;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Utils;

namespace Assets.Scripts.CardEngine.Game
{
    public class Deck
    {
        private readonly CardCollection _cards;

        public int CardCount => _cards.Count;

        public Deck(List<Card> cards = null)
        {
            _cards = cards != null ? new CardCollection(cards) : new CardCollection();
        }

        public void AddCard(Card card)
        {
            _cards.AddCard(card);
        }

        public Card DrawTop()
        {
            Card topCard = _cards.FirstCard();
            if (topCard != null)
            {
                _cards.TakeCard(topCard);
                return topCard;
            }
            throw new InvalidOperationException("Deck is empty");
        }

    }
}
