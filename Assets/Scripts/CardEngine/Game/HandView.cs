using UnityEngine;

using System.Collections.Generic;
using Assets.Scripts.CardEngine.Cards;

namespace Assets.Scripts.CardEngine.Game
{
    public class HandView : MonoBehaviour
    {
        private readonly List<CardView> _cardViews = new();

        public void AddCardView(CardView cardView)
        {
            cardView.transform.SetParent(this.transform, false);
            _cardViews.Add(cardView);
            UpdateCardPositions();
        }

        public void RemoveCardView(CardView cardView)
        {
            _cardViews.Remove(cardView);
            UpdateCardPositions();
        }

        private void UpdateCardPositions()
        {
            float spacing = 0.1f;
            float startX = -((_cardViews.Count - 1) * spacing) / 2f;

            for (int i = 0; i < _cardViews.Count; i++)
            {
                _cardViews[i].transform.localPosition = new Vector3(startX + i * spacing, 0, 0);
            }
        }
    }
}
