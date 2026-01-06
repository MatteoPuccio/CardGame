using UnityEngine;
using Assets.Scripts.CardEngine.Game;

namespace Assets.Scripts.CardEngine.Cards
{
	public class CardInCemeteryState : ICardInteractionState
	{
		public CemeteryView OwnerCemeteryView { get; private set; }

		public CardInCemeteryState(CemeteryView ownerCemeteryView)
		{
			OwnerCemeteryView = ownerCemeteryView;
		}

		public string GetName => "CardInCemeteryState";

		public void Enter(CardView view)
		{
			Debug.Log($"CardInCemeteryState: Entered for {view.CardData.Name}");
		}

		public void Exit(CardView view) { }

		public void OnMouseDown(CardView view)
		{
			if (view?.CardData?.GameState == null)
				return;
			if (view.CardData.Owner?.Cemetery == null)
				return;

			bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
			if (shift)
			{
				if (view.CardData.Owner.Deck == null)
					return;
				view.CardData.GameState.TryMoveToZone(view.CardData, view.CardData.Owner.Cemetery, view.CardData.Owner.Deck, this);
				return;
			}

			if (view.CardData.Owner.Hand == null)
				return;
			view.CardData.GameState.TryMoveToZone(view.CardData, view.CardData.Owner.Cemetery, view.CardData.Owner.Hand);
		}

		public void OnMouseDrag(CardView view) { }
		public void OnMouseUp(CardView view) { }
	}
}

