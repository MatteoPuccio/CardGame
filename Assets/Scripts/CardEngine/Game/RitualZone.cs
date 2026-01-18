using System;
using System.Collections.Generic;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Utils;

namespace Assets.Scripts.CardEngine.Game
{
    public sealed class RitualZone : ICardZone
    {
        private readonly CardCollection _cards;
        private readonly Player _owner;
        private readonly GameState _gameState;

        public string ZoneName => "RitualZone";
        public Player Owner => _owner;
        public GameState GameState => _gameState;
        public IReadOnlyList<Card> Cards => _cards.Cards;

        public event Action<Card> CardAdded;
        public event Action<Card> CardRemoved;

        public RitualZone(Player owner = null, GameState gameState = null, List<Card> cards = null)
        {
            _owner = owner;
            _gameState = gameState;
            _cards = cards != null ? new CardCollection(cards) : new CardCollection();
        }

        public bool CanEnter(Card card)
        {
            // Only ritual cards should live here.
            return card != null && card.Category == CardType.Ritual;
        }

        public bool EnterCard(Card card)
        {
            if (!CanEnter(card))
                return false;

            _cards.AddCard(card);
            CardAdded?.Invoke(card);
            return true;
        }

        public bool ExitCard(Card card)
        {
            bool removed = _cards.TakeCard(card);
            if (removed)
                CardRemoved?.Invoke(card);
            return removed;
        }

        public bool Contains(Card card)
        {
            if (card == null)
                return false;

            var list = _cards.Cards;
            if (list == null)
                return false;

            for (int i = 0; i < list.Count; i++)
            {
                if (ReferenceEquals(list[i], card))
                    return true;
            }

            return false;
        }
    }
}
