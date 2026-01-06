using UnityEngine;

using System;
using System.Collections.Generic;
using Assets.Scripts.CardEngine.Cards;
using UnityEngine.EventSystems;

namespace Assets.Scripts.CardEngine.Game
{
	public class CemeteryView : MonoBehaviour
	{
		private readonly List<CardView> _cardViews = new();

		public event Action<CemeteryView> Clicked;

		private void OnMouseDown()
		{
			Debug.Log("CemeteryView clicked.");
			Clicked?.Invoke(this);
		}

		public void AddCardView(CardView cardView)
		{
			if (cardView == null)
				return;

			if (!_cardViews.Contains(cardView))
				_cardViews.Add(cardView);

			cardView.transform.SetParent(transform, true);
			cardView.SetState(new CardInCemeteryState(this));
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
			// Keep the pile slightly above the zone surface/collider so cards remain clickable.
			float baseY = 0.005f;
			float spacing = 0.001f;
			for (int i = 0; i < _cardViews.Count; i++)
			{
				_cardViews[i].transform.localPosition = new Vector3(0, baseY + i * spacing, 0);
			}
		}
	}
}