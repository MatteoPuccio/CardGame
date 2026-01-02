using UnityEngine;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Game;

namespace Assets.Scripts.CardEngine.Cards 
{
    public class CardFactory: MonoBehaviour
    {
        [SerializeField] private GameObject _cardPrefab;
        [SerializeField] private GameObject _board;

        public CardView CreateCard(Card card, GameState gameState = null, CardViewRegistry registry = null)
        {
            Debug.Log("CardFactory: Creating card view: " + card.Name);
            if (_cardPrefab == null)
            {
                Debug.LogError("CardFactory: cardPrefab is not assigned.");
                return null;
            }
            var cardGO = Object.Instantiate(original: _cardPrefab, parent: _board.transform);
            cardGO.name = $"Card_{card.Name}";
            card.GameState = gameState;
            var cardView = cardGO.GetComponent<CardView>();
            if (cardView != null)
            {
                cardView.CardData = card;
                registry?.Register(card, cardView);
            }
            return cardView;
        }

    }
}