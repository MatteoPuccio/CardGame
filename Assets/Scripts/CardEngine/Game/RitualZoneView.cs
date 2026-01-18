using System.Collections.Generic;
using Assets.Scripts.CardEngine.Cards;
using UnityEngine;

namespace Assets.Scripts.CardEngine.Game
{
    public sealed class RitualZoneView : MonoBehaviour
    {
        private readonly List<CardView> _cardViews = new();

        [Header("Layout")]
        [SerializeField] private float _baseY = 0.51f;
        [SerializeField] private float _xSpacing = 0.08f;
        [SerializeField] private float _zOffset = 0.0f;

        public void AddCardView(CardView cardView)
        {
            if (cardView == null)
                return;

            if (!_cardViews.Contains(cardView))
                _cardViews.Add(cardView);

            cardView.transform.SetParent(transform, true);
            cardView.SetState(new CardInRitualZoneState());
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
            // Simple row layout centered on the zone.
            int count = _cardViews.Count;
            if (count == 0)
                return;

            float startX = -((count - 1) * _xSpacing) * 0.5f;

            for (int i = 0; i < count; i++)
            {
                var view = _cardViews[i];
                if (view == null)
                    continue;

                view.transform.localPosition = new Vector3(startX + i * _xSpacing, _baseY, _zOffset);
                view.transform.localRotation = Quaternion.identity;
            }
        }

        private void OnMouseDown()
        {
            var cam = Camera.main;
            if (cam == null)
                return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            var hits = Physics.RaycastAll(ray, 100f);
            if (hits == null || hits.Length == 0)
                return;

            CardView best = null;
            float bestDist = float.PositiveInfinity;

            foreach (var hit in hits)
            {
                if (hit.collider == null)
                    continue;

                var cardView = hit.collider.GetComponentInParent<CardView>();
                if (cardView == null)
                    continue;

                // Only forward clicks to cards currently in this ritual zone.
                if (!_cardViews.Contains(cardView))
                    continue;

                if (hit.distance < bestDist)
                {
                    bestDist = hit.distance;
                    best = cardView;
                }
            }

            best?.HandleClick();
        }
    }
}
