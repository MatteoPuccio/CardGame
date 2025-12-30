using Assets.Scripts.CardEngine.Game;
using Assets.Scripts.CardEngine.Board;
using Assets.Scripts.CardEngine.Cards;

namespace Assets.Scripts.CardEngine.Cards
{
    public class Card : ITargetable
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public CardType CardType { get; set; }
        public CardView CardView { get; set; }

        public Player Owner { get; set; }
        public GameState GameState { get; set; }

        public void PlayFromHand(PlayAreaZone zone)
        {
            if (GameState != null && Owner != null && Owner.Hand != null && Owner.Hand.RemoveCard(this))
            {
                // CardView handles zone occupancy and reparenting
                GameState.PlayCard(this, Owner);
            }
        }

        
    }
}
