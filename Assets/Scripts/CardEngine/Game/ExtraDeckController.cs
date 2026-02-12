using System;
using System.Collections.Generic;
using Assets.Scripts.CardEngine.Cards;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.CardEngine.Game
{
    public sealed class ExtraDeckController : MonoBehaviour
    {
        private static event Action<ExtraDeckController> Selected;

        public static void DeselectAll()
        {
            Selected?.Invoke(null);
        }

        [SerializeField] private ExtraDeckView _extraDeckView;

        private ScrollRect _ownedScrollRect;
        private bool _startDisabled;

        private ExtraDeck _extraDeck;
        private CardFactory _cardFactory;
        private readonly Dictionary<Card, CardView> _bindings = new();

        public GameController GameController { get; set; }

        public void Initialize(ExtraDeck extraDeck)
        {
            _extraDeck = extraDeck;

            if (_extraDeckView == null)
                _extraDeckView = GetComponentInChildren<ExtraDeckView>(includeInactive: true);

            if (_extraDeck == null)
            {
                Debug.LogError("ExtraDeckController: Initialize called with null ExtraDeck.");
                return;
            }

            if (_extraDeckView == null)
            {
                Debug.LogError("ExtraDeckController: ExtraDeckView reference is null.");
                return;
            }

            if (GameController == null)
            {
                Debug.LogError("ExtraDeckController: GameController is not set before Initialize().");
                return;
            }

            _cardFactory = GameController.CardFactory;
            if (_cardFactory == null)
            {
                Debug.LogError("ExtraDeckController: GameController.CardFactory is null.");
                return;
            }

            _extraDeckView.Clicked -= OnExtraDeckViewClicked;
            _extraDeckView.Clicked += OnExtraDeckViewClicked;

            _extraDeck.CardAdded += OnCardAdded;
            _extraDeck.CardRemoved += OnCardRemoved;

            var list = _extraDeck.Cards;
            for (int i = 0; i < list.Count; i++)
                CreateOrReuseView(list[i]);
        }

        public void BindScrollRects(ScrollRect owned, bool startDisabled = true)
        {
            _ownedScrollRect = owned;
            _startDisabled = startDisabled;

            if (_ownedScrollRect == null)
            {
                Debug.LogWarning("ExtraDeckController: BindScrollRects called with null ScrollRect.");
                return;
            }

            if (_startDisabled)
                SetScrollRectEnabled(_ownedScrollRect, false);
        }

        private void OnEnable()
        {
            Selected -= OnAnyExtraDeckSelected;
            Selected += OnAnyExtraDeckSelected;

            if (_startDisabled)
                SetScrollRectEnabled(_ownedScrollRect, false);
        }

        private void OnDestroy()
        {
            if (_extraDeckView != null)
                _extraDeckView.Clicked -= OnExtraDeckViewClicked;

            Selected -= OnAnyExtraDeckSelected;

            if (_extraDeck != null)
            {
                _extraDeck.CardAdded -= OnCardAdded;
                _extraDeck.CardRemoved -= OnCardRemoved;
            }
        }

        private void OnExtraDeckViewClicked(ExtraDeckView _)
        {
            Selected?.Invoke(this);
        }

        private void OnAnyExtraDeckSelected(ExtraDeckController selected)
        {
            SetScrollRectEnabled(_ownedScrollRect, selected == this);
        }

        private static void SetScrollRectEnabled(ScrollRect scrollRect, bool enabled)
        {
            scrollRect?.gameObject.SetActive(enabled);
        }

        private void OnCardAdded(Card card)
        {
            CreateOrReuseView(card);
        }

        private void OnCardRemoved(Card card)
        {
            if (card == null)
                return;

            if (_bindings.TryGetValue(card, out var view) && view != null)
            {
                Debug.Log($"ExtraDeckController: Removing view for {card.Name} (IsHidden={view.IsHidden}).");
                _bindings.Remove(card);
                _extraDeckView.RemoveCardView(view);

                // The card is leaving the extra deck. Destroy the hidden-prefab view
                // immediately and unregister it so that the destination controller
                // (e.g. RitualZone) creates a fresh visible view.
                // DestroyImmediate ensures the old object is truly gone before the
                // destination's OnCardAdded handler runs (which fires next in the
                // same TryTransferCard call).
                GameController?.CardViewRegistry?.Unregister(card);
                DestroyImmediate(view.gameObject);
            }
        }

        private void CreateOrReuseView(Card card)
        {
            if (card == null)
                return;

            if (GameController?.CardViewRegistry != null && GameController.CardViewRegistry.TryGet(card, out var existingView))
            {
                // Extra deck cards are always hidden. If the existing view is visible,
                // destroy it and create a fresh hidden-prefab view.
                if (!existingView.IsHidden)
                {
                    GameController.CardViewRegistry.Unregister(card);
                    _bindings.Remove(card);
                    Destroy(existingView.gameObject);
                }
                else
                {
                    _bindings[card] = existingView;
                    existingView.SetState(new CardInExtraDeckState(_extraDeckView));
                    _extraDeckView.AddCardView(existingView);
                    return;
                }
            }

            var view = _cardFactory.CreateCard(card, _extraDeck.GameState, GameController?.CardViewRegistry, hidden: true);
            if (view == null)
                return;

            _bindings[card] = view;
            view.SetState(new CardInExtraDeckState(_extraDeckView));
            _extraDeckView.AddCardView(view);
        }
    }
}
