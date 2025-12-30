using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Game;
using Assets.Scripts.CardEngine;

namespace Assets.Scripts.CardEngine.Game
{
    public class HandController : MonoBehaviour
    {
        [SerializeField] private HandView handView;
        public GameController GameController { get; set; }

        private Hand hand;
        private readonly Dictionary<Card, CardView> bindings = new();

        public void Initialize(Hand hand)
        {
            this.hand = hand;

            hand.CardAdded += OnCardAdded;
            hand.CardRemoved += OnCardRemoved;

            foreach (var card in hand.Cards)
                CreateView(card);
        }

        private void OnCardAdded(Card card)
        {
            // Reuse existing view if the card already has one (e.g., moved back from board)
            if (card.CardView != null)
            {
                var view = card.CardView;
                bindings[card] = view;
                handView.AddCardView(view);
            }
            else
            {
                CreateView(card);
            }
        }

        private void OnCardRemoved(Card card)
        {
            var view = bindings[card];
            bindings.Remove(card);
            handView.RemoveCardView(view);
        }

        private void CreateView(Card card)
        {
            CardView view = GameController.CardFactory.CreateCard(card, hand.Owner, hand.GameState);
            bindings[card] = view;
            card.CardView = view;
            handView.AddCardView(view);
        }
    }
}