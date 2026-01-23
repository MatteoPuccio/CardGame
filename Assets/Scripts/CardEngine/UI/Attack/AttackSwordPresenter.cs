using System;
using System.Collections.Generic;
using Assets.Scripts.CardEngine.Board;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Events;
using UnityEngine;

namespace Assets.Scripts.CardEngine.Game
{
	// Sword icons are shown for all eligible attackers (no hover gating).

	/// <summary>
	/// Shows a clickable sword icon over troop CardViews that can declare an attack.
	/// When clicked, it starts the existing TargetingManager flow (defender selection).
	/// </summary>
	[DisallowMultipleComponent]
	public sealed class AttackSwordPresenter : MonoBehaviour
	{
		[SerializeField] private CardView _cardView;
		[SerializeField] private GameObject _iconRoot;
		[SerializeField] private string _autoFindIconChildName = "AttackSwordIcon";
		[SerializeField] private bool _logIfMissingIcon = true;
		private bool _lastTargetingActive;
		private TurnPhase _lastPhase;
		private bool _lastShouldShow;
		private bool _subscribed;

		private EventBus Bus => _cardView?.CardData?.GameState?.EventBus;

		private void Awake()
		{
			_cardView ??= GetComponent<CardView>();
			BindIconFromPrefab();
		}

		private void OnEnable()
		{
			TrySubscribePhaseChanged();
			CacheState();
			RefreshVisibility();
		}

		private void OnDisable()
		{
			TryUnsubscribePhaseChanged();
		}

		private void Update()
		{
			// Keep UI in sync with TargetingManager/phase changes even if no events are raised.
			var gs = _cardView?.CardData?.GameState;
			if (gs == null)
				return;

			bool targetingActive = gs.Targeting != null && gs.Targeting.IsActive;
			bool phaseChanged = gs.Phase != _lastPhase;
			bool targetingChanged = targetingActive != _lastTargetingActive;
			bool shouldShow = ShouldShowSword();

			if (phaseChanged || targetingChanged || shouldShow != _lastShouldShow)
			{
				_lastTargetingActive = targetingActive;
				_lastPhase = gs.Phase;
				_lastShouldShow = shouldShow;
				if (_iconRoot != null)
					_iconRoot.SetActive(shouldShow);
			}
		}

		internal void RefreshVisibility()
		{
			if (_iconRoot == null)
				return;
			_iconRoot.SetActive(ShouldShowSword());
		}

		private bool ShouldShowSword()
		{
			var card = _cardView?.CardData;
			var gs = card?.GameState;
			if (gs == null)
				return false;

			if (gs.Phase != TurnPhase.Attack)
				return false;
			if (gs.Attack == null || !gs.Attack.IsActive)
				return false;
			if (gs.Targeting != null && gs.Targeting.IsActive)
				return false;

			if (!IsTroopCardInPlay())
				return false;
			if (_iconRoot == null)
				return false;

			return gs.Attack.CanDeclareAttackFrom(card);
		}

		private bool IsTroopCardInPlay()
		{
			var card = _cardView?.CardData;
			if (card?.Behavior is not TroopBehavior)
				return false;
			return BoardQueryUtils.IsInPlayZone(card);
		}

		private void BindIconFromPrefab()
		{
			if (_iconRoot == null && !string.IsNullOrWhiteSpace(_autoFindIconChildName))
			{
				var t = transform.Find(_autoFindIconChildName);
				if (t != null)
					_iconRoot = t.gameObject;
			}

			if (_iconRoot == null)
			{
				if (_logIfMissingIcon)
					Debug.LogWarning($"AttackSwordPresenter: No icon assigned/found for '{name}'. Assign _iconRoot or add a child named '{_autoFindIconChildName}'.", this);
				return;
			}

			var click = _iconRoot.GetComponent<AttackSwordIconClickHandler>();
			if (click == null)
				click = _iconRoot.AddComponent<AttackSwordIconClickHandler>();
			click.Bind(this);

			// Default to hidden; presenter will show it when eligible.
			_iconRoot.SetActive(false);
		}

		internal void HandleSwordClicked()
		{
			var card = _cardView?.CardData;
			var gs = card?.GameState;
			if (gs == null || card == null)
				return;

			if (gs.Attack == null)
				return;

			bool started = gs.Attack.TryBeginAttackWithAttacker(card, out var reason);
			if (!started && !string.IsNullOrWhiteSpace(reason))
				Debug.Log($"AttackSword: Cannot start attack for '{card.Name}': {reason}");
		}

		private void TrySubscribePhaseChanged()
		{
			if (_subscribed)
				return;
			var bus = Bus;
			if (bus == null)
				return;
			bus.Subscribe<PhaseChangedEvent>(OnPhaseChanged);
			_subscribed = true;
		}

		private void TryUnsubscribePhaseChanged()
		{
			if (!_subscribed)
				return;
			var bus = Bus;
			if (bus == null)
				return;
			bus.Unsubscribe<PhaseChangedEvent>(OnPhaseChanged);
			_subscribed = false;
		}

		private void OnPhaseChanged(PhaseChangedEvent e)
		{
			RefreshVisibility();
		}

		private void CacheState()
		{
			var gs = _cardView?.CardData?.GameState;
			_lastPhase = gs != null ? gs.Phase : TurnPhase.Draw;
			_lastTargetingActive = gs?.Targeting != null && gs.Targeting.IsActive;
			_lastShouldShow = false;
		}
	}

	[DisallowMultipleComponent]
	internal sealed class AttackSwordIconClickHandler : MonoBehaviour
	{
		private AttackSwordPresenter _presenter;

		public void Bind(AttackSwordPresenter presenter)
		{
			_presenter = presenter;
		}

		private void OnMouseDown()
		{
			_presenter?.HandleSwordClicked();
		}
	}
}
