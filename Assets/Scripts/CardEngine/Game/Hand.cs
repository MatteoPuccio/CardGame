
using System;
using System.Collections.Generic;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Utils;

using UnityEngine;
namespace Assets.Scripts.CardEngine.Game
{
    public class Hand: ICardZone
    {
        private readonly CardCollection _cards;
        public int CardCount => _cards.Count;
        private readonly Player _owner;
        private readonly GameState _gameState;

        public Player Owner => _owner;
        public GameState GameState => _gameState;
        public IReadOnlyList<Card> Cards => _cards.Cards;

        public event Action<Card> CardAdded;
        public event Action<Card> CardRemoved;
        public string ZoneName => "Hand";

        public Hand(Player owner = null, GameState gameState = null, List<Card> cards = null)
        {
            _owner = owner;
            _gameState = gameState;
            _cards = cards != null ? new CardCollection(cards) : new CardCollection();
        }

        public void AddCard(Card card)
        {
            _cards.AddCard(card);
            CardAdded?.Invoke(card);
            Debug.Log($"Hand: Card added: {card.Name}. Total cards now: {_cards.Count}");
        }

        public bool RemoveCard(Card card)
        {
            bool removed = _cards.TakeCard(card);
            if (removed)
            {
                CardRemoved?.Invoke(card);
            }
            return removed;
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
            RemoveCard(card);
            return true;
        }
    }
}