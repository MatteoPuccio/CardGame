using System;
using System.Collections.Generic;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Utils;

namespace Assets.Scripts.CardEngine.Game
{
    public class Cemetery : ICardZone
    {
        private readonly CardCollection _cards;
        private readonly Player _owner;
        private readonly GameState _gameState;

        public string ZoneName => "Cemetery";
        public int CardCount => _cards.Count;
        public Player Owner => _owner;
        public GameState GameState => _gameState;
        public IEnumerable<Card> Cards => _cards.Cards;

        public event Action<Card> CardAdded;
        public event Action<Card> CardRemoved;

        public Cemetery(Player owner = null, GameState gameState = null, List<Card> cards = null)
        {
            _owner = owner;
            _gameState = gameState;
            _cards = cards != null ? new CardCollection(cards) : new CardCollection();
        }

        public void AddCard(Card card)
        {
            _cards.AddCard(card);
            CardAdded?.Invoke(card);
        }

        public void RemoveCard(Card card)
        {
            bool removed = _cards.TakeCard(card);
            if (removed)
                CardRemoved?.Invoke(card);
        }

        public void Clear()
        {
            // Copy to avoid mutating while iterating.
            var snapshot = new List<Card>(_cards.Cards);
            foreach (var card in snapshot)
            {
                _cards.TakeCard(card);
                CardRemoved?.Invoke(card);
            }
        }

        public bool CanEnter(Card card) => true;

        public bool EnterCard(Card card)
        {
            AddCard(card);
            return true;
        }

        public bool ExitCard(Card card)
        {
            RemoveCard(card);
            return true;
        }
    }
}