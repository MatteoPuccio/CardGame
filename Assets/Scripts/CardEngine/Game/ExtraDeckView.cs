using System;
using System.Collections.Generic;
using Assets.Scripts.CardEngine.Cards;
using UnityEngine;

namespace Assets.Scripts.CardEngine.Game
{
    public sealed class ExtraDeckView : MonoBehaviour
    {
        private readonly List<CardView> _cardViews = new();

        public event Action<ExtraDeckView> Clicked;

        private void OnMouseDown()
        {
            HandleClick();
        }

        public void HandleClick()
        {
            Clicked?.Invoke(this);
        }

        public void AddCardView(CardView cardView)
        {
            if (cardView == null)
                return;

            if (!_cardViews.Contains(cardView))
                _cardViews.Add(cardView);

            cardView.transform.SetParent(transform, true);
            UpdateCardPositions();
        }

        public void RemoveCardView(CardView cardView)
        {
            if (cardView == null)
                return;

            _cardViews.Remove(cardView);
            UpdateCardPositions();
        }

        private void UpdateCardPositions()
        {
            // Stack very similarly to Deck/Cemetery.
            float baseY = 0.005f;
            float spacing = 0.001f;
            for (int i = 0; i < _cardViews.Count; i++)
            {
                var v = _cardViews[i];
                if (v == null)
                    continue;

                v.transform.localPosition = new Vector3(0, baseY + i * spacing, 0);
                v.transform.localRotation = Quaternion.identity;
            }
        }
    }
}
