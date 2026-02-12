using System;
using System.Collections.Generic;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Utils;

namespace Assets.Scripts.CardEngine.Game
{
    public class Deck: ICardZone
    {
        private readonly CardCollection _cards;
        private readonly Player _owner;
        private readonly GameState _gameState;

        public int CardCount => _cards.Count;
        public IEnumerable<Card> Cards => _cards.Cards;

        public Player Owner => _owner;
        public GameState GameState => _gameState;
        public event Action<Card> CardAdded;
        public event Action<Card> CardRemoved;
        public string ZoneName => ZoneNames.Deck;

        public Deck(Player owner, GameState gameState, List<Card> cards = null)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _gameState = gameState ?? throw new ArgumentNullException(nameof(gameState));
            _cards = cards != null ? new CardCollection(cards) : new CardCollection();
        }

        public void AddCard(Card card)
        {
            _cards.AddCard(card);
            _cards.Shuffle();
            CardAdded?.Invoke(card);
        }

        public void AddCardToTop(Card card)
        {
            _cards.InsertCardAt(card, 0);
            CardAdded?.Invoke(card);
        }

        public void AddCardToBottom(Card card)
        {
            _cards.AddCard(card);
            CardAdded?.Invoke(card);
        }

        public void RemoveCard(Card card)
        {
            bool removed = _cards.TakeCard(card);
            if (removed)
            {
                CardRemoved?.Invoke(card);
            }
        }

        public void Clear()
        {
            // Copy to avoid mutating while iterating.
            var snapshot = new List<Card>(_cards.Cards);
            foreach (var card in snapshot)
            {
                bool removed = _cards.TakeCard(card);
                if (removed)
                    CardRemoved?.Invoke(card);
            }
        }

        public Card DrawTop()
        {
            Card topCard = _cards.FirstCard();
            if (topCard == null)
                return null;

            if (_owner.Hand == null)
                throw new InvalidOperationException("Deck.DrawTop: owner.Hand is not initialized.");

            bool moved = _gameState.MoveToZone(topCard, this, _owner.Hand);
            return moved ? topCard : null;
        }

        public void Shuffle()
        {
            _cards.Shuffle();
        }

        public IEnumerable<Card> GetAllCards()
        {
            return _cards.Cards;
        }


        public bool CanEnter(Card card)
        {
            return true;
        }

        public bool EnterCard(Card card)
        {
            AddCard(card);
            return true;
        }

        public bool ExitCard(Card card)
        {
            bool removed = _cards.TakeCard(card);
            if (removed)
                CardRemoved?.Invoke(card);
            return removed;
        }
    }
}
