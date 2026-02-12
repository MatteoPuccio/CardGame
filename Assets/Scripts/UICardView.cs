using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Game;
using Assets.Scripts.CardEngine.Events;
using Assets.Scripts.CardEngine.Effects;
using System.Threading.Tasks;
using System.Linq;

namespace Assets.Scripts
{
	public interface IUICardClickHandler
	{
		/// <summary>
		/// Return true to consume the click and prevent default behavior.
		/// </summary>
		bool HandleClick(UICardView view, PointerEventData eventData);
	}

	public class UICardView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
	{
		[SerializeField] private TMP_Text _nameText;
		[SerializeField] private TMP_Text _effectText;
		[SerializeField] private TMP_Text _raceText;
		[Header("Troop Properties")]
		[SerializeField] private Image _oneStar;
        [SerializeField] private Image _twoStar;
        [SerializeField] private Image _threeStar;
		[SerializeField] private TMP_Text _powerText;
		[SerializeField] private TMP_Text _healthText;

		[Header("Troop UI Colors")]
		[SerializeField] private Color _filledStarColor = Color.yellow;
		[SerializeField] private Color _emptyStarColor = new Color(1f, 1f, 1f, 0.25f);

		public Card CardData { get; private set; }
		private TroopBehavior _boundTroop;

		public void Bind(Card card)
		{
			UnbindTroopEvents();
			CardData = card;
			BindTroopEvents();
			Refresh();
		}

		private void OnDestroy()
		{
			UnbindTroopEvents();
		}

		private void Refresh()
		{
			if (_nameText != null)
				_nameText.text = CardData != null ? CardData.Name : string.Empty;

			if (_effectText != null)
			{
				_effectText.text = CardData != null ? (CardData.EffectText ?? string.Empty) : string.Empty;
			}

			RefreshTroopUI();

		}

		private void BindTroopEvents()
		{
			if (CardData?.Behavior is TroopBehavior troop)
			{
				_boundTroop = troop;
				troop.OnStatsChanged += HandleTroopStatsChanged;
			}
		}

		private void UnbindTroopEvents()
		{
			if (_boundTroop != null)
				_boundTroop.OnStatsChanged -= HandleTroopStatsChanged;
			_boundTroop = null;
		}

		private void HandleTroopStatsChanged(TroopBehavior troop)
		{
			if (troop == null || CardData?.Behavior != troop)
				return;
			UpdateTroopStats(troop);
		}

		private void RefreshTroopUI()
		{
			if (CardData?.Behavior is TroopBehavior troop)
			{
				ApplyDeployCostStars(troop.DeployCost);
				UpdateTroopStats(troop);
				UpdateRaceTag(troop);
			}
			else
			{
				ApplyDeployCostStars(0);
				if (_powerText != null) _powerText.text = string.Empty;
				if (_healthText != null) _healthText.text = string.Empty;
				if (_raceText != null) _raceText.text = string.Empty;
			}
		}

		public void ApplyDeployCostStars(int deployCost)
		{
			if (CardData != null && CardData.Category != CardType.Troop)
				return;

			ApplyStar(_oneStar, deployCost >= 1);
			ApplyStar(_twoStar, deployCost >= 2);
			ApplyStar(_threeStar, deployCost >= 3);
		}

		private void ApplyStar(Image image, bool filled)
		{
			if (image == null)
				return;
			image.color = filled ? _filledStarColor : _emptyStarColor;
		}

		public void UpdateTroopStats(TroopBehavior troop)
		{
			if (troop == null)
				return;
			if (_powerText != null)
				_powerText.text = troop.Power.ToString();
			if (_healthText != null)
				_healthText.text = troop.Health.ToString();
		}

		private void UpdateRaceTag(TroopBehavior troop)
		{
			if (_raceText == null)
				return;
			if (troop == null || !troop.HasRace)
			{
				_raceText.text = string.Empty;
				return;
			}
			_raceText.text = troop.Race.ToString();
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
				return;
			if (CardData == null)
				return;

			// The preview instance is meant to be informational; only the race tag is interactive there.
			if (IsInPreview())
			{
				TryHandleRaceTagClick(eventData);
				return;
			}

			if (TryHandleRaceTagClick(eventData))
				return;

			if (HandleExternalClickHandlers(eventData))
				return;
			if (QuickPlayFromHand())
				return;
			if (QuickPlayFromExtraDeck())
				return;

			ShiftClickMoveCemeteryToDeck();
		}

		public void OnPointerEnter(PointerEventData eventData)
		{
			if (CardData == null)
				return;
			if (IsInPreview())
				return;

			CardPreviewController.Show(CardData);
		}

		public void OnPointerExit(PointerEventData eventData)
		{
			if (IsInPreview())
				return;

			CardPreviewController.ClearPreview();
		}

		private bool TryHandleRaceTagClick(PointerEventData eventData)
		{
			if (_raceText == null)
				return false;
			if (CardData?.Behavior is not TroopBehavior troop)
				return false;
			if (!troop.HasRace)
				return false;
			if (!IsInPreview())
				return false;

			var clicked = eventData.pointerCurrentRaycast.gameObject;
			if (clicked == null)
				return false;

			if (!clicked.transform.IsChildOf(_raceText.transform))
				return false;

			RaceInfoPanelController.Show(troop.Race, eventData.position);
			return true;
		}

		private bool IsInPreview()
		{
			return GetComponentInParent<UICardPreview>() != null;
		}

		private bool HandleExternalClickHandlers(PointerEventData eventData)
		{
			var behaviours = GetComponents<MonoBehaviour>();
			for (int i = 0; i < behaviours.Length; i++)
			{
				if (behaviours[i] is IUICardClickHandler handler && handler.HandleClick(this, eventData))
					return true;
			}
			return false;
		}

		private bool QuickPlayFromHand()
		{
			if (!TryGetQuickPlayContext(out var gs, out var hand))
				return false;

			var playEffects = GetPlayTriggeredEffects(CardData);
			if (OptionalEffectPrompting.HasAnyOptional(playEffects))
			{
				_ = QuickPlayFromHandAsync(gs, hand, playEffects);
				return true;
			}

			// Reuse the same targeting/session pipeline used by CardInHandState.
			if (!CardData.TryBeginPlay(hand, playEffectsOverride: null, out var session, out var candidates))
				return false;
			if (candidates != null && candidates.Count > 0)
				gs.Targeting.Begin(session, candidates);
			return true;
		}

		private bool QuickPlayFromExtraDeck()
		{
			if (!TryGetQuickPlayExtraDeckContext(out var gs, out var extraDeck))
				return false;

			var playEffects = GetPlayTriggeredEffects(CardData);
			if (OptionalEffectPrompting.HasAnyOptional(playEffects))
			{
				_ = QuickPlayFromHandAsync(gs, extraDeck, playEffects);
				return true;
			}

			if (!CardData.TryBeginPlay(extraDeck, playEffectsOverride: null, out var session, out var candidates))
				return false;
			if (candidates != null && candidates.Count > 0)
				gs.Targeting.Begin(session, candidates);
			return true;
		}

		private bool TryGetQuickPlayContext(out GameState gameState, out ICardZone hand)
		{
			gameState = null;
			hand = null;

			// Default "play on click" for UI cards that are not draggable.
			// We only do this for cards currently in the owner's hand that do NOT require a board zone (e.g., spells),
			// matching the existing drag-only rule for troops.
			if (CardData == null)
				return false;

			var gs = CardData.GameState;
			var owner = CardData.Owner;
			if (gs == null || owner?.Hand == null)
				return false;
			if (!owner.Hand.Cards.Contains(CardData))
				return false;

			bool isBoardCard = CardData.Behavior != null && CardData.Behavior.RequiresPlayZone;
			if (isBoardCard)
				return false;

			gameState = gs;
			hand = owner.Hand;
			return true;
		}

		private bool TryGetQuickPlayExtraDeckContext(out GameState gameState, out ICardZone extraDeck)
		{
			gameState = null;
			extraDeck = null;

			if (CardData == null)
				return false;

			var gs = CardData.GameState;
			var owner = CardData.Owner;
			if (gs == null || owner?.ExtraDeck == null)
				return false;
			if (!owner.ExtraDeck.Contains(CardData))
				return false;

			// Only allow click-play for non-board cards from UI lists.
			bool isBoardCard = CardData.Behavior != null && CardData.Behavior.RequiresPlayZone;
			if (isBoardCard)
				return false;

			gameState = gs;
			extraDeck = owner.ExtraDeck;
			return true;
		}

		private static List<Effect> GetPlayTriggeredEffects(Card card)
		{
			if (card == null)
				return null;

			var preview = new CardPlayedEvent(card: card, player: card.Owner);
			var list = card.TriggeredEffects;
			if (list == null)
				return null;

			var effects = new List<Effect>();
			for (int i = 0; i < list.Count; i++)
			{
				var te = list[i];
				if (te == null)
					continue;
				if (te.Matches(card, preview) && te.Effect != null)
					effects.Add(te.Effect);
			}

			return effects.Count > 0 ? effects : null;
		}

		private async Task QuickPlayFromHandAsync(GameState gameState, ICardZone sourceZone, IReadOnlyList<Effect> playEffects)
		{
			if (CardData == null || gameState == null || sourceZone == null || playEffects == null || playEffects.Count == 0)
				return;

			var playOverride = await OptionalEffectPrompting.BuildOverrideAsync(gameState, CardData.Owner, CardData, playEffects);
			if (playOverride == null)
				playOverride = playEffects;
			if (!CardData.TryBeginPlay(sourceZone, playOverride, out var session, out var candidates))
				return;

			if (candidates != null && candidates.Count > 0)
				gameState.Targeting.Begin(session, candidates);
		}

		private void ShiftClickMoveCemeteryToDeck()
		{
			bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
			if (!shift)
				return;
			if (CardData.GameState == null)
				return;
			var owner = CardData.Owner;
			if (owner == null || owner.Cemetery == null || owner.Deck == null)
				return;
			if (!owner.Cemetery.Cards.Contains(CardData))
				return;

			CardData.GameState.MoveToZone(CardData, owner.Cemetery, owner.Deck);
		}
	}
}
