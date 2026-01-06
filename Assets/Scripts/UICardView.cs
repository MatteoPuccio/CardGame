using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

using Assets.Scripts.CardEngine.Cards;

namespace Assets.Scripts
{
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
				_effectText.text = CardData != null ? (CardData.EffectText ?? string.Empty) : string.Empty;
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
				return;
			if (CardData == null)
				return;

			CardPreviewController.Show(CardData);

			bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
			if (!shift)
				return;
			if (CardData.GameState == null)
				return;
			var owner = CardData.Owner;
			if (owner == null || owner.Cemetery == null || owner.Deck == null)
				return;

			if (owner.Cemetery.Contains(CardData))
			{
				CardData.GameState.TryMoveToZone(CardData, owner.Cemetery, owner.Deck);
			}
		}
	}
}
