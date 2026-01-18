using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Game;

namespace Assets.Scripts.CardEngine.Events
{
    public sealed class CardMovedEvent : IGameEvent
    {
        public string EventType => "CardMoved";
        public Card Source { get; }
        public Player Player { get; }
        public string From { get; }
        public string To { get; }

        public CardMovedEvent(Card card, Player player, string from, string to)
        {
            Source = card;
            Player = player;
            From = from;
            To = to;
        }
    }
}
