using Assets.Scripts.CardEngine.Cards;

namespace Assets.Scripts.CardEngine.Game
{
    public interface ICardZone
    {
        public string ZoneName { get; }
        
        public bool CanEnter(Card card);
        public bool EnterCard(Card card);
        public bool ExitCard(Card card);
    }
}