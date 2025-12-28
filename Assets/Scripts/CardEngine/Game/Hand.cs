
using System.Collections.Generic;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Utils;

using UnityEngine;
namespace Assets.Scripts.CardEngine.Game
{
    public class Hand
    {
        private readonly CardCollection _cards;
        private readonly GameObject _cardPrefab;
        private readonly GameObject _handArea;
        private readonly List<GameObject> _cardGameObjects = new List<GameObject>();
        private readonly Player _owner;
        private readonly GameState _gameState;
        public int CardCount => _cards.Count;

        public Hand(GameObject cardPrefab, GameObject handArea, Player owner = null, GameState gameState = null, List<Card> cards = null)
        {
            _cardPrefab = cardPrefab;
            _handArea = handArea;
            _owner = owner;
            _gameState = gameState;
            _cards = cards != null ? new CardCollection(cards) : new CardCollection();
            if (cards != null)
            {
                foreach (var card in cards)
                {
                    InstantiateCardObject(card);
                }
            }
        }

        public void AddCard(Card card)
        {
            _cards.AddCard(card);
            InstantiateCardObject(card);
            UpdateCardPositions();
        }

        private void InstantiateCardObject(Card card)
        {
            if (_cardPrefab != null)
            {
                var cardGO = Object.Instantiate(_cardPrefab, _handArea.transform);
                cardGO.name = $"Card_{card.Name}";
                card.Owner = _owner;
                card.GameState = _gameState;
                var draggable = cardGO.GetComponent<DraggableCard>();
                if (draggable != null)
                {
                    draggable.CardData = card;
                }
                _cardGameObjects.Add(cardGO);
            }
        }

        private void UpdateCardPositions()
        {
            // Arrange cards in a row, centered in the hand area
            int cardCount = _cardGameObjects.Count;
            float spacing = 0.1f;
            float totalWidth = (cardCount - 1) * spacing;
            float startX = -totalWidth / 2f;
            for (int i = 0; i < cardCount; i++)
            {
                if (_cardGameObjects[i] != null)
                {
                    _cardGameObjects[i].transform.localPosition = new Vector3(startX + i * spacing, 0, 0);
                }
            }
        }
    }
}