using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Game;
using Assets.Scripts.CardEngine;

namespace Assets.Scripts.CardEngine.Game
{
    public class HandController : MonoBehaviour
    {
        [SerializeField] private HandView _handView;

        private Hand _hand;
        private readonly Dictionary<Card, CardView> bindings = new();

        public GameController GameController { get; set; }


        public void Initialize(Hand hand)
        {
            _hand = hand;

            _hand.CardAdded += OnCardAdded;
            _hand.CardRemoved += OnCardRemoved;

            foreach (var card in _hand.Cards)
                CreateView(card);
            _handView.UpdateCardPositions();
        }

        private void OnCardAdded(Card card)
        {
            // Reuse an existing view if it was already created elsewhere (e.g., moved back from board)
            if (GameController?.CardViewRegistry != null && GameController.CardViewRegistry.TryGet(card, out var existingView))
            {
                bindings[card] = existingView;
                _handView.AddCardView(existingView);
                return;
            }

            CreateView(card);
        }

        private void OnCardRemoved(Card card)
        {
            var view = bindings[card];
            bindings.Remove(card);
            _handView.RemoveCardView(view);
        }

        private void CreateView(Card card)
        {
            CardView view = GameController.CardFactory.CreateCard(card, _hand.GameState, GameController?.CardViewRegistry);
            view.SetState(new CardInHandState(_handView));
            bindings[card] = view;
            _handView.AddCardView(view);
            _handView.UpdateCardPositions();
        }
    }
}