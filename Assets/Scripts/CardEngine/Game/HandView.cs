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
            if (!_cardViews.Contains(cardView))
                _cardViews.Add(cardView);

            cardView.transform.SetParent(this.transform, true);
            cardView.SetState(new CardInHandState(this));
            UpdateCardPositions();
        }

        public void RemoveCardView(CardView cardView)
        {
            _cardViews.Remove(cardView);
            UpdateCardPositions();
        }

        public void UpdateCardPositions()
        {
            float spacing = 0.1f;
            float startX = -((_cardViews.Count - 1) * spacing) / 2f;

            for (int i = 0; i < _cardViews.Count; i++)
            {
                _cardViews[i].transform.localPosition = new Vector3(startX + i * spacing, 0, 0);
            }
        }

        public void ReturnCard(CardView card)
        {
            if (!_cardViews.Contains(card))
                _cardViews.Add(card);

            // Preserve world scale/rotation when reparenting; avoids scale jump if HandView is scaled.
            card.transform.SetParent(transform, true);
            card.transform.localPosition = Vector3.zero;
            card.transform.localRotation = Quaternion.identity;

            card.SetState(new CardInHandState(this));
            UpdateCardPositions();
        }
    }
}
