using Assets.Scripts.CardEngine.Game;
namespace Assets.Scripts.CardEngine.Cards
{
    public class Card : ITargetable
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public CardType CardType { get; set; }

        public Player Owner { get; set; }
        public GameState GameState { get; set; }

        public void Play()
        {
            if (GameState != null && Owner != null)
            {
                GameState.PlayCard(this, Owner);
            }
        }
    }
}
