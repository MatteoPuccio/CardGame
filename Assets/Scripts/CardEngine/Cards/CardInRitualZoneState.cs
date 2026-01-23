using UnityEngine;
using Assets.Scripts.CardEngine.Game;

namespace Assets.Scripts.CardEngine.Cards
{
    public sealed class CardInRitualZoneState : ICardInteractionState
    {
        public string Name => "CardInRitualZoneState";

        public CardInRitualZoneState() { }

        public void Enter(CardView view)
        {
        }

        public void Exit(CardView view)
        {
        }

        public void OnMouseDown(CardView view)
        {
            var card = view?.CardData;
            var gs = card?.GameState;
            if (card == null || gs == null)
                return;

            // Only the active player can advance their rituals.
            if (gs.ActivePlayer != null && card.Owner != null && gs.ActivePlayer != card.Owner)
                return;

            if (gs.Targeting != null && gs.Targeting.IsActive)
                return;

            if (card.Behavior is RitualBehavior ritual)
                ritual.TryAdvanceStage();
        }

        public void OnMouseDrag(CardView view) { }
        public void OnMouseUp(CardView view) { }
    }
}
