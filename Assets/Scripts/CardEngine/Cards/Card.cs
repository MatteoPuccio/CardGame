using System.Collections.Generic;
using Assets.Scripts.CardEngine.Game;
using Assets.Scripts.CardEngine.Effects;


namespace Assets.Scripts.CardEngine.Cards
{
    public class Card: ITargetable
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string EffectText { get; set; }
		public CardType Category { get; }
		public CardBehavior Behavior { get; }

        public Player Owner { get; }
        public GameState GameState { get; set; }
        public IEffect OnPlayEffect { get; set; }

        public Card(
            string id, 
            CardType cardCategory, 
            Player owner,
            string name = "",
            string effectText = "",
            IEffect effect = null,
            GameState gameState = null
        )
        {
            Id = id;
            Name = name;
            EffectText = effectText;
			Category = cardCategory;
            Owner = owner;
            OnPlayEffect = effect;
            GameState = gameState;
			Behavior = CardBehavior.Create(this, cardCategory);
        }

        public void Play(ICardZone sourceZone)
        {
            EffectContext context = new EffectContext
            {
                Source = this,
                GameState = GameState,
            };
            OnPlayEffect?.Resolve(context);
			Behavior?.AfterPlayed(context, sourceZone);
        }
    }
}
