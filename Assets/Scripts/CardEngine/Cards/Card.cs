using System.Collections.Generic;
using Assets.Scripts.CardEngine.Game;
using Assets.Scripts.CardEngine.Effects;


namespace Assets.Scripts.CardEngine.Cards
{
    public class Card: ITargetable
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public CardType CardType { get; set; }

        public Player Owner { get; }
        public GameState GameState { get; set; }
        public IEffect OnPlayEffect { get; set; }

        public Card(
            string id, 
            string name, 
            CardType cardType, 
            Player owner, 
            IEffect effect = null,
            GameState gameState = null
        )
        {
            Id = id;
            Name = name;
            CardType = cardType;
            Owner = owner;
            OnPlayEffect = effect;
            GameState = gameState;
        }

        public void Play(ICardZone sourceZone)
        {
            EffectContext context = new EffectContext
            {
                Source = this,
                GameState = GameState,
            };
            OnPlayEffect?.Resolve(context);
        }
    }
}
