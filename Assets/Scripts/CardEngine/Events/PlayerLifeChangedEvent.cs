using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Game;

namespace Assets.Scripts.CardEngine.Events
{
    public sealed class PlayerLifeChangedEvent : IGameEvent
    {
        public string EventType { get; } = "PlayerLifeChanged";
        public Card Source { get; }

        public Player Player { get; }
        public uint Before { get; }
        public uint After { get; }

        public PlayerLifeChangedEvent(Player player, uint before, uint after, Card source = null)
        {
            Player = player;
            Before = before;
            After = after;
            Source = source;
        }
    }
}
