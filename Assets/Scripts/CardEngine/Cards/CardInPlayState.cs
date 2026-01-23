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
        }

        public void OnMouseDrag(CardView view) { }
        public void OnMouseUp(CardView view) { }
    }
}
