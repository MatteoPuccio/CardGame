using System;
using System.Collections.Generic;
using Assets.Scripts.CardEngine.Cards;
using UnityEngine;

namespace Assets.Scripts.CardEngine.Game
{
	public sealed class RitualZoneController : MonoBehaviour
	{
		[Header("Views (assign in prefab)")]
		[SerializeField] private RitualZoneView _ritualZoneView1;
		[SerializeField] private RitualZoneView _ritualZoneView2;

		private RitualZone _ritualZone;
		private CardFactory _cardFactory;

		private readonly Dictionary<Card, CardView> _bindings = new();
		private readonly Dictionary<Card, RitualZoneView> _assignedViewByCard = new();

		public GameController GameController { get; set; }

		public void Initialize(RitualZone ritualZone)
		{
			if (ritualZone == null)
				throw new ArgumentNullException(nameof(ritualZone));

			Unbind();

			_ritualZone = ritualZone;

			if (GameController == null)
				throw new InvalidOperationException("RitualZoneController: GameController is not set before Initialize().");

			_cardFactory = GameController.CardFactory;
			if (_cardFactory == null)
				throw new InvalidOperationException("RitualZoneController: GameController.CardFactory is null.");

			ResolveViews();

			_ritualZone.CardAdded += OnCardAdded;
			_ritualZone.CardRemoved += OnCardRemoved;

			// Bootstrap existing cards.
			var cards = _ritualZone.Cards;
			if (cards != null)
			{
				for (int i = 0; i < cards.Count; i++)
					OnCardAdded(cards[i]);
			}
		}

		private void ResolveViews()
		{
			if (_ritualZoneView1 == null || _ritualZoneView2 == null)
			{
				var views = GetComponentsInChildren<RitualZoneView>(includeInactive: true);
				if (views != null)
				{
					if (_ritualZoneView1 == null && views.Length > 0)
						_ritualZoneView1 = views[0];
					if (_ritualZoneView2 == null && views.Length > 1)
						_ritualZoneView2 = views[1];
				}
			}
		}

		private void OnDestroy()
		{
			Unbind();
		}

		private void Unbind()
		{
			if (_ritualZone != null)
			{
				_ritualZone.CardAdded -= OnCardAdded;
				_ritualZone.CardRemoved -= OnCardRemoved;
			}

			_bindings.Clear();
			_assignedViewByCard.Clear();
			_ritualZone = null;
		}

		private void OnCardAdded(Card card)
		{
			if (card == null)
				return;

			// Reuse an existing view if it was already created elsewhere.
			if (GameController?.CardViewRegistry != null && GameController.CardViewRegistry.TryGet(card, out var existingView))
			{
				_bindings[card] = existingView;
				AddToAssignedView(card, existingView);
				return;
			}

			var view = _cardFactory.CreateCard(card, _ritualZone?.GameState, GameController?.CardViewRegistry);
			if (view == null)
				return;

			_bindings[card] = view;
			AddToAssignedView(card, view);
		}

		private void OnCardRemoved(Card card)
		{
			if (card == null)
				return;

			_bindings.TryGetValue(card, out var cardView);

			if (_assignedViewByCard.TryGetValue(card, out var zoneView) && zoneView != null && cardView != null)
				zoneView.RemoveCardView(cardView);

			_bindings.Remove(card);
			_assignedViewByCard.Remove(card);
		}

		private void AddToAssignedView(Card card, CardView cardView)
		{
			if (card == null || cardView == null)
				return;

			var zoneView = GetPreferredViewFor(card);
			if (zoneView == null)
				return;

			_assignedViewByCard[card] = zoneView;
			zoneView.AddCardView(cardView);
		}

		private RitualZoneView GetPreferredViewFor(Card card)
		{
			if (_ritualZoneView1 == null && _ritualZoneView2 == null)
				return null;

			// If already assigned (e.g. re-entrant event), keep stable.
			if (_assignedViewByCard.TryGetValue(card, out var existing) && existing != null)
				return existing;

			// Prefer the first free "slot".
			bool view1Occupied = IsViewOccupied(_ritualZoneView1);
			bool view2Occupied = IsViewOccupied(_ritualZoneView2);

			if (_ritualZoneView1 != null && !view1Occupied)
				return _ritualZoneView1;

			if (_ritualZoneView2 != null && !view2Occupied)
				return _ritualZoneView2;
			return null;
		}

		private bool IsViewOccupied(RitualZoneView view)
		{
			if (view == null)
				return false;

			foreach (var kvp in _assignedViewByCard)
			{
				if (kvp.Value == view)
					return true;
			}

			return false;
		}
	}
}

