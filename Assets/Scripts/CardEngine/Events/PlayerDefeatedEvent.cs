using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Game;

namespace Assets.Scripts.CardEngine.Events
{
    public sealed class PlayerDefeatedEvent : IGameEvent
    {
        public string EventType { get; } = "PlayerDefeated";
        public Card Source { get; }

        public Player DefeatedPlayer { get; }

        public PlayerDefeatedEvent(Player defeatedPlayer, Card source = null)
        {
            DefeatedPlayer = defeatedPlayer;
            Source = source;
        }
    }
}
