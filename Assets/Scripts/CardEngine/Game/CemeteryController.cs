using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Board;

namespace Assets.Scripts.CardEngine.Game
{
	public class CemeteryController : MonoBehaviour
	{
		private static event Action<CemeteryController> Selected;

		public static void DeselectAll()
		{
			Selected?.Invoke(null);
		}

		[SerializeField] private CemeteryView _cemeteryView;

		private ScrollRect _ownedScrollRect;
		private bool _startDisabled;

		private Cemetery _cemetery;
		private CardFactory _cardFactory;
		private readonly Dictionary<Card, CardView> _bindings = new();

		public GameController GameController { get; set; }

		public void Initialize(Cemetery cemetery)
		{
			_cemetery = cemetery;

			if (_cemeteryView == null)
				_cemeteryView = GetComponentInChildren<CemeteryView>(includeInactive: true);

			if (_cemetery == null)
			{
				Debug.LogError("CemeteryController: Initialize called with null Cemetery.");
				return;
			}

			if (_cemeteryView == null)
			{
				Debug.LogError("CemeteryController: CemeteryView reference is null.");
				return;
			}

			if (GameController == null)
			{
				Debug.LogError("CemeteryController: GameController is not set before Initialize().");
				return;
			}

			_cardFactory = GameController.CardFactory;
			if (_cardFactory == null)
			{
				Debug.LogError("CemeteryController: GameController.CardFactory is null.");
				return;
			}

			_cemeteryView.Clicked -= OnCemeteryViewClicked;
			_cemeteryView.Clicked += OnCemeteryViewClicked;

			_cemetery.CardAdded += OnCardAdded;
			_cemetery.CardRemoved += OnCardRemoved;

			foreach (var card in _cemetery.Cards)
				CreateOrReuseView(card);
		}

		public void BindScrollRects(ScrollRect owned, bool startDisabled = true)
		{
			_ownedScrollRect = owned;
			_startDisabled = startDisabled;

			if (_ownedScrollRect == null)
			{
				Debug.LogWarning("CemeteryController: BindScrollRects called with null ScrollRect.");
				return;
			}

			if (_startDisabled)
				SetScrollRectEnabled(_ownedScrollRect, false);
		}

		private void OnEnable()
		{
			Selected -= OnAnyCemeterySelected;
			Selected += OnAnyCemeterySelected;

			if (_startDisabled)
				SetScrollRectEnabled(_ownedScrollRect, false);
		}

		private void OnDestroy()
		{
			if (_cemeteryView != null)
				_cemeteryView.Clicked -= OnCemeteryViewClicked;

			Selected -= OnAnyCemeterySelected;

			if (_cemetery != null)
			{
				_cemetery.CardAdded -= OnCardAdded;
				_cemetery.CardRemoved -= OnCardRemoved;
			}
		}

		private void OnCemeteryViewClicked(CemeteryView _)
		{
			Selected?.Invoke(this);
		}

		private void OnAnyCemeterySelected(CemeteryController selected)
		{
			SetScrollRectEnabled(_ownedScrollRect, selected == this);
		}

		private static void SetScrollRectEnabled(ScrollRect scrollRect, bool enabled)
		{
			if (scrollRect == null)
				return;
			Debug.Log($"CemeteryController: Set scroll enabled to {enabled}.");

			scrollRect.gameObject.SetActive(enabled);
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
				_bindings.Remove(card);
				_cemeteryView.RemoveCardView(view);
			}
		}

		private void CreateOrReuseView(Card card)
		{
			if (card == null)
				return;

			if (GameController?.CardViewRegistry != null && GameController.CardViewRegistry.TryGet(card, out var existingView))
			{
				_bindings[card] = existingView;
				_cemeteryView.AddCardView(existingView);
				return;
			}

			var view = _cardFactory.CreateCard(card, _cemetery.GameState, GameController?.CardViewRegistry);
			if (view == null)
				return;

			_bindings[card] = view;
			_cemeteryView.AddCardView(view);
		}
	}
}

