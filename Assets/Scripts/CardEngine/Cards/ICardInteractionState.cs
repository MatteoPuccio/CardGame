
namespace Assets.Scripts.CardEngine.Cards
{
    public interface ICardInteractionState
    {
        void Enter(CardView view);
        void Exit(CardView view);
    
        void OnMouseDown(CardView view);
        void OnMouseDrag(CardView view);
        void OnMouseUp(CardView view);
    }

}