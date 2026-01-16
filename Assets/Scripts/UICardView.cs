using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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

		public Card CardData { get; private set; }

		public void Bind(Card card)
		{
			CardData = card;
			Refresh();
		}

		private void Refresh()
		{
			if (_nameText != null)
				_nameText.text = CardData != null ? CardData.Name : string.Empty;

			if (_effectText != null)
			{
				_effectText.text = CardData != null ? (CardData.EffectText ?? string.Empty) : string.Empty;
			}

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
