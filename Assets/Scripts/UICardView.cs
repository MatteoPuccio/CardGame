using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Assets.Scripts.CardEngine.Cards;
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

	public class UICardView : MonoBehaviour, IPointerClickHandler
	{
		[SerializeField] private TMP_Text _nameText;
		[SerializeField] private TMP_Text _effectText;
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
			}
			else
			{
				ApplyDeployCostStars(0);
				if (_powerText != null) _powerText.text = string.Empty;
				if (_healthText != null) _healthText.text = string.Empty;
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

		public void OnPointerClick(PointerEventData eventData)
		{
			if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
				return;
			if (CardData == null)
				return;

			if (HandleExternalClickHandlers(eventData))
				return;
			if (QuickPlayFromHand())
				return;

			CardPreviewController.Show(CardData);
			ShiftClickMoveCemeteryToDeck();
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
			// Default "play on click" for UI cards that are not draggable.
			// We only do this for cards currently in the owner's hand that do NOT require a board zone (e.g., spells),
			// matching the existing drag-only rule for troops.
			var gs = CardData.GameState;
			var cardOwner = CardData.Owner;
			if (gs == null || cardOwner?.Hand == null)
				return false;
			if (!cardOwner.Hand.Cards.Contains(CardData))
				return false;

			bool isBoardCard = CardData.Behavior != null && CardData.Behavior.RequiresPlayZone;
			if (isBoardCard)
				return false;

			// Reuse the same targeting/session pipeline used by CardInHandState.
			if (!CardData.TryBeginPlay(cardOwner.Hand, out var session, out var candidates))
				return false;
			if (candidates != null && candidates.Count > 0)
				gs.Targeting.Begin(session, candidates);
			return true;
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
