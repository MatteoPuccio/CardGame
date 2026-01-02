using System;
using System.Collections.Generic;
using Assets.Scripts.CardEngine.Game;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Utils;

using UnityEngine;

namespace Assets.Scripts.CardEngine.Game
{
    public class DeckController : MonoBehaviour
    {
        [SerializeField] private DeckView _deckView;

        private Deck _deck;
        public GameController GameController { get; set; }
        private readonly Dictionary<Card, CardView> bindings = new();


        public void Initialize(Deck deck)
        {
            _deck = deck;
            _deck.CardAdded += OnCardAdded;
            _deck.CardRemoved += OnCardRemoved;
            foreach (var card in _deck.GetAllCards())
                CreateView(card);
        }

        private void OnDestroy()
        {
            if (_deck != null)
            {
                _deck.CardAdded -= OnCardAdded;
                _deck.CardRemoved -= OnCardRemoved;
            }
        }

        private void OnCardAdded(Card card)
        {
            if (GameController?.CardViewRegistry != null && GameController.CardViewRegistry.TryGet(card, out var existingView))
            {
                bindings[card] = existingView;
                _deckView.AddCardView(existingView);
                return;
            }

            CreateView(card);
        }

        private void OnCardRemoved(Card card)
        {
            var view = bindings[card];
            bindings.Remove(card);
            _deckView.RemoveCardView(view);
        }

        private void CreateView(Card card)
        {
            if (GameController == null)
            {
                Debug.LogError("DeckController: GameController reference is null. Cannot create CardView.");
                return;
            }
            if (GameController.CardFactory == null)
            {
                Debug.LogError("DeckController: CardFactory reference is null in GameController. Cannot create CardView.");
                return;
            }
            CardView cardView = GameController.CardFactory.CreateCard(card, _deck.GameState, GameController?.CardViewRegistry);
            cardView.SetState(new CardInDeckState(_deckView));
            bindings[card] = cardView;
            _deckView.AddCardView(cardView);
        }
    }
}