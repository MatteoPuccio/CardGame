using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts;
using Assets.Scripts.CardEngine.Events;
using Assets.Scripts.CardEngine.Game;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace Assets.Scripts.CardEngine
{
	public class UIPhaseChanger : MonoBehaviour
	{
		[Header("References")]
		[SerializeField] private GameController _gameController;
		[SerializeField] private Button _mainPhaseButton;
		[SerializeField] private Transform _phaseButtonsRoot;
		[SerializeField] private Button _endTurnButton;

		[Header("Behavior")]
		[SerializeField] private bool _startCollapsed = true;
		[SerializeField] private bool _collapseOnPhaseChanged = true;
		[SerializeField] private bool _autoWireMainButton = true;
		[SerializeField] private bool _disableCurrentPhaseButton = true;

		private readonly List<GameObject> _phaseButtonObjects = new();
		private readonly Dictionary<TurnPhase, Button> _phaseButtons = new();
		private readonly Dictionary<Button, UnityAction> _phaseButtonClickHandlers = new();
		private UnityAction _endTurnHandler;
		private bool _isExpanded;
		private EventBus _bus;
		private Coroutine _bindRoutine;
		private TurnFlow TurnFlow => _gameController?.TurnFlow;

		private void Awake()
		{
			_phaseButtonsRoot ??= transform;
			_mainPhaseButton ??= GetComponent<Button>();
			CachePhaseButtons();
			WirePhaseButtonClicks();

			if (_autoWireMainButton)
				_mainPhaseButton?.onClick.AddListener(OnMainButtonClicked);
		}

		private void OnEnable()
		{
			if (_startCollapsed)
				SetExpanded(false);

			_bindRoutine ??= StartCoroutine(BindWhenReady());
		}

		private void OnDisable()
		{
			if (_bindRoutine != null)
			{
				StopCoroutine(_bindRoutine);
				_bindRoutine = null;
			}

			Unsubscribe();
			UnwirePhaseButtonClicks();
		}

		private void OnDestroy()
		{
			_mainPhaseButton?.onClick.RemoveListener(OnMainButtonClicked);
		}

		private IEnumerator BindWhenReady()
		{
			// GameController creates EventBus/GameState in Start(); wait until they exist.
			yield return new WaitUntil(() => ResolveBus() != null);
			Subscribe();
			RefreshFromState();
		}

		private EventBus ResolveBus()
		{
			if (_bus != null)
				return _bus;

			_gameController ??= FindFirstObjectByType<GameController>();
			_bus = _gameController?.GameState?.EventBus;
			return _bus;
		}

		private void Subscribe()
		{
			ResolveBus();
			_bus?.Subscribe<PhaseChangedEvent>(OnPhaseChanged);
		}

		private void Unsubscribe()
		{
			_bus?.Unsubscribe<PhaseChangedEvent>(OnPhaseChanged);
			_bus = null;
		}

		private void RefreshFromState()
		{
			var phase = _gameController?.GameState?.Phase;
			if (phase.HasValue)
				SetCurrentPhaseVisuals(phase.Value);
		}

		public void OnMainButtonClicked()
		{
			SetExpanded(!_isExpanded);
		}

		private void SetExpanded(bool expanded)
		{
			_isExpanded = expanded;
			foreach (var go in _phaseButtonObjects)
			{
				if (go == null)
					continue;
				go.SetActive(_isExpanded);
			}
		}

		private void CachePhaseButtons()
		{
			_phaseButtonObjects.Clear();
			_phaseButtons.Clear();

			if (_phaseButtonsRoot == null)
				return;

			var phaseButtonComponents = _phaseButtonsRoot.GetComponentsInChildren<UIPhaseButton>(includeInactive: true);
			
			foreach (var pb in phaseButtonComponents)
			{
				var button = pb?.Button;
				if (button == null)
					continue;
				if (button == _mainPhaseButton)
					continue;

				_phaseButtonObjects.Add(button.gameObject);
				_phaseButtons[pb.Phase] = button;
			}

			if (_endTurnButton != null)
				_phaseButtonObjects.Add(_endTurnButton.gameObject);
		}

		private void WirePhaseButtonClicks()
		{
			UnwirePhaseButtonClicks();
			foreach (var kvp in _phaseButtons)
			{
				var phase = kvp.Key;
				var button = kvp.Value;
				if (button == null)
					continue;

				UnityAction handler = () => OnPhaseButtonClicked(phase);
				_phaseButtonClickHandlers[button] = handler;
				button.onClick.AddListener(handler);
			}

			if (_endTurnButton != null)
			{
				_endTurnHandler = EndTurn;
				_endTurnButton.onClick.AddListener(_endTurnHandler);
			}
		}

		private void UnwirePhaseButtonClicks()
		{
			foreach (var kvp in _phaseButtonClickHandlers)
			{
				kvp.Key?.onClick.RemoveListener(kvp.Value);
			}
			_phaseButtonClickHandlers.Clear();

			if (_endTurnButton != null && _endTurnHandler != null)
				_endTurnButton.onClick.RemoveListener(_endTurnHandler);
			_endTurnHandler = null;
		}

		private void OnPhaseButtonClicked(TurnPhase targetPhase)
		{
			SetExpanded(false);
			_gameController ??= FindFirstObjectByType<GameController>();
			TurnFlow?.AdvanceToPhase(targetPhase);
		}

		private void EndTurn()
		{
			TurnFlow?.AdvanceTurn();
		}

		private void OnValidate()
		{
			if (!Application.isPlaying)
			{
				_phaseButtonsRoot ??= transform;
				_mainPhaseButton ??= GetComponent<Button>();
				CachePhaseButtons();
				if (_startCollapsed)
					SetExpanded(false);
			}
		}

		private void OnPhaseChanged(PhaseChangedEvent e)
		{
			if (e == null) return;
			SetCurrentPhaseVisuals(e.ToPhase);

			if (_collapseOnPhaseChanged)
				SetExpanded(false);
		}

		private void SetCurrentPhaseVisuals(TurnPhase phase)
		{
			if (!_disableCurrentPhaseButton || _phaseButtons.Count == 0)
				return;

			foreach (TurnPhase p in Enum.GetValues(typeof(TurnPhase)))
			{
				if (!_phaseButtons.TryGetValue(p, out var button) || button == null)
					continue;
				button.interactable = p != phase;
			}
		}
	}
}
