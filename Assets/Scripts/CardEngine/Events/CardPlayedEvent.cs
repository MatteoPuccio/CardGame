using System;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Game;

namespace Assets.Scripts.CardEngine.Events
{
    public class CardPlayedEvent : IGameEvent
    {
        public string EventType => "CardPlayed";
        public Card Source { get; }
        public Player Player { get; }

        public CardPlayedEvent(Card card, Player player)
        {
            Source = card;
            Player = player;
        }
    }
}
