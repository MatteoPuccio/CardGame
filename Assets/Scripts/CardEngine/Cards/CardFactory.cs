using UnityEngine;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Game;

namespace Assets.Scripts.CardEngine.Cards 
{
    public class CardFactory: MonoBehaviour
    {
        [SerializeField] private GameObject _cardPrefab;
        [SerializeField] private GameObject _board;

        public CardView CreateCard(Card card, Player owner = null, GameState gameState = null)
        {
            Debug.Log("CardFactory: Creating card view: " + card.Name);
            if (_cardPrefab == null)
            {
                Debug.LogError("CardFactory: cardPrefab is not assigned.");
                return null;
            }
            var cardGO = Object.Instantiate(original: _cardPrefab, parent: _board.transform);
            cardGO.name = $"Card_{card.Name}";
            card.Owner = owner;
            card.GameState = gameState;
            var cardView = cardGO.GetComponent<CardView>();
            if (cardView != null)
            {
                cardView.CardData = card;
                card.CardView = cardView;
            }
            return cardView;
        }

    }
}