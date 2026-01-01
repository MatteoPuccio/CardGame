using UnityEngine;
using Assets.Scripts.CardEngine.Game;

namespace Assets.Scripts.CardEngine.Cards
{
    public class CardInDeckState : ICardInteractionState
    {

        public DeckView OwnerDeckView { get; private set;}

        public CardInDeckState(DeckView ownerDeckView)
        {
            OwnerDeckView = ownerDeckView;
        }


        public void Enter(CardView view)
        {
            Debug.Log($"CardInDeckState: Entered deck state for card {view.CardData.Name}");
        }
        public void Exit(CardView view) { }

        public void OnMouseDown(CardView view)
        {
            if (view.CardData?.GameState == null)
                return;

            view.CardData.GameState.TryMoveToZone(view.CardData, view.CardData.Owner.Deck, view.CardData.Owner.Hand);

            view.OccupiedZone = null;
            view.OccupiedZoneView = null;
        }
        public void OnMouseDrag(CardView view) { }
        public void OnMouseUp(CardView view) { }
    }
}
