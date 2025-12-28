

using System.Collections.Generic;
using Assets.Scripts.CardEngine.Events;
using Assets.Scripts.CardEngine.Cards;

namespace Assets.Scripts.CardEngine.Game
{
    public class GameState
    {
        public Player Player1 { get; private set; }
        public Player Player2 { get; private set; }
        private readonly EventBus _eventBus;

        public GameState(EventBus bus)
        {
            _eventBus = bus;
        }

        public void AddPlayers(Player p1, Player p2)
        {
            Player1 = p1;
            Player2 = p2;
        }

        public Player GetOpponent(Player player) =>
            player == Player1 ? Player2 : Player1;

        public List<ITargetable> GetEnemyCharacters(Player player)
        {
            // Placeholder implementation until character model exists.
            return new List<ITargetable>();
        }

        public void PlayCard(Card card, Player player)
        {
            // Here you can add logic to remove the card from hand, etc.
            _eventBus.Publish(new CardPlayedEvent(card, player));
        }
    }
}