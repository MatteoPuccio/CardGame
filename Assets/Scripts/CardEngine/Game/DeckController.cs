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
        private CardFactory _cardFactory;
        public GameController GameController { get; set; }
        private readonly Dictionary<Card, CardView> bindings = new();


        public void Initialize(Deck deck)
        {
            if (deck == null)
                throw new ArgumentNullException(nameof(deck));

            _deck = deck;

            if (_deckView == null)
                _deckView = GetComponentInChildren<DeckView>(includeInactive: true);

            if (_deckView == null)
                throw new InvalidOperationException("DeckController: DeckView reference is null.");

            if (GameController == null)
                throw new InvalidOperationException("DeckController: GameController is not set before Initialize().");

            _cardFactory = GameController.CardFactory;
            if (_cardFactory == null)
                throw new InvalidOperationException("DeckController: GameController.CardFactory is null.");

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
            if (card == null)
                return;

            if (!bindings.TryGetValue(card, out var view) || view == null)
                return;

            bindings.Remove(card);
            _deckView.RemoveCardView(view);
        }

        private void CreateView(Card card)
        {
            if (card == null)
                return;

            CardView cardView = _cardFactory.CreateCard(card, _deck.GameState, GameController?.CardViewRegistry);
            cardView.SetState(new CardInDeckState(_deckView));
            bindings[card] = cardView;
            _deckView.AddCardView(cardView);
        }
    }
}