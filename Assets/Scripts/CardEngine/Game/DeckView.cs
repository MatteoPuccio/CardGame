using UnityEngine;

using System.Collections.Generic;
using Assets.Scripts.CardEngine.Cards;

namespace Assets.Scripts.CardEngine.Game
{
    public class DeckView : MonoBehaviour
    {
        private readonly List<CardView> _cardViews = new();

        public void AddCardView(CardView cardView)
        {
            if (!_cardViews.Contains(cardView))
                _cardViews.Add(cardView);

            cardView.transform.SetParent(this.transform, true);
            cardView.SetState(new CardInDeckState(this));
            UpdateCardPositions();
        }

        public void RemoveCardView(CardView cardView)
        {
            _cardViews.Remove(cardView);
            UpdateCardPositions();
        }

        private void UpdateCardPositions()
        {
            float spacing = 0.001f;

            for (int i = 0; i < _cardViews.Count; i++)
            {
                _cardViews[i].transform.localPosition = new Vector3(0, i * spacing, 0);
            }
        }
    }
}
