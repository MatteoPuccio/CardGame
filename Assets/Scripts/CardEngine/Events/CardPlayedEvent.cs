using System;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Game;
using Assets.Scripts.CardEngine.Board;

namespace Assets.Scripts.CardEngine.Events
{
    public class CardPlayedEvent : IGameEvent
    {
        public string EventType { get; }
        public Card Source { get; }
        public Player Player { get; }

        // Optional metadata for generic card flow
        public string From { get; }
        public string To { get; }

        public CardPlayedEvent(Card card, Player player)
            : this(eventType: "CardPlayed", card: card, player: player)
        {
        }

        public CardPlayedEvent(string eventType, Card card, Player player, string from = null, string to = null)
        {
            EventType = eventType;
            Source = card;
            Player = player;
            From = from;
            To = to;
        }
    }
}
