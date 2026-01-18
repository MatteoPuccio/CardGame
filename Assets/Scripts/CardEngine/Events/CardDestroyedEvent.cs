using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Game;

namespace Assets.Scripts.CardEngine.Events
{
    public sealed class CardDestroyedEvent : IGameEvent
    {
        public string EventType => "CardDestroyed";
        public Card Source => Target;
        public Card Target { get; }
        public Card Instigator { get; }
        public bool MovedToCemetery { get; }

        public CardDestroyedEvent(Card target, Card instigator, bool movedToCemetery)
        {
            Target = target;
            Instigator = instigator;
            MovedToCemetery = movedToCemetery;
        }
    }
}
