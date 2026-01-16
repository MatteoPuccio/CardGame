using UnityEngine;
using Assets.Scripts.CardEngine.Board;
using Assets.Scripts.CardEngine.Cards;

namespace Assets.Scripts.CardEngine.Cards
{
    public class CardInPlayState : ICardInteractionState
    {
        public PlayAreaZoneView OccupiedZoneView { get; private set; }
        public CardInPlayState(PlayAreaZoneView occupiedZoneView) 
        {
            OccupiedZoneView = occupiedZoneView;
        }
        public string Name => "CardInPlayState";

        public void Enter(CardView view)
        {
            Debug.Log($"CardInPlayState: Entered play state for card {view.CardData.Name}");
        }
        public void Exit(CardView view) { }

        public void OnMouseDown(CardView view)
        {
            if (view.CardData?.GameState == null)
                return;

            if (view.CardData.GameState.ActivePlayer != null && view.CardData.Owner != null && view.CardData.GameState.ActivePlayer != view.CardData.Owner)
                return;

            // Default: click returns card to hand.
            // Minimal extra: Shift+click sends card to deck (top).
            bool toDeck = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool moved = toDeck
                ? view.CardData.GameState.MoveToZone(view.CardData, view.OccupiedZone, view.CardData.Owner.Deck, this)
                : view.CardData.GameState.MoveToZone(view.CardData, view.OccupiedZone, view.CardData.Owner.Hand, this);
            
            if (!moved)
                return;
            view.OccupiedZone = null;
            view.OccupiedZoneView = null;
        }

        public void OnMouseDrag(CardView view) { }
        public void OnMouseUp(CardView view) { }
    }
}
