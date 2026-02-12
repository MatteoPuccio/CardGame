using Assets.Scripts.CardEngine.Game;
using UnityEngine;

namespace Assets.Scripts.CardEngine.Cards
{
    public sealed class CardInExtraDeckState : ICardInteractionState
    {
        public ExtraDeckView OwnerExtraDeckView { get; private set; }

        public CardInExtraDeckState(ExtraDeckView ownerExtraDeckView)
        {
            OwnerExtraDeckView = ownerExtraDeckView;
        }

        public string Name => "CardInExtraDeckState";

        public void Enter(CardView view)
        {
            if (view?.CardData != null)
                Debug.Log($"CardInExtraDeckState: Entered for {view.CardData.Name}");
        }

        public void Exit(CardView view) { }

        public void OnMouseDown(CardView view)
        {
            OwnerExtraDeckView?.HandleClick();
        }

        public void OnMouseDrag(CardView view) { }
        public void OnMouseUp(CardView view) { }
    }
}
