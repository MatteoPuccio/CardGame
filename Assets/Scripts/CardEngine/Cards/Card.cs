using System.Collections.Generic;
using Assets.Scripts.CardEngine.Game;
using Assets.Scripts.CardEngine.Effects;
using Assets.Scripts.CardEngine.Events;


namespace Assets.Scripts.CardEngine.Cards
{
    public class Card: ITargetable
    {
        public TargetableKind Kind => TargetableKind.Card;

        public string Id { get; set; }
        public string Name { get; set; }
        public string EffectText { get; set; }
		public CardType Category { get; }
		public CardBehavior Behavior { get; }

        public Player Owner { get; }
        public GameState GameState { get; set; }
        public Effect OnPlayEffect { get; set; }
        public List<RapidEffect> RapidEffects { get; } = new();

        public Card(
            string id, 
            CardType cardCategory, 
            Player owner,
            string name = "",
            string effectText = "",
            Effect effect = null,
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

        public bool TryBeginPlay(ICardZone sourceZone, out CardPlaySession session, out List<ITargetable> candidates)
        {
            session = null;
            candidates = null;

            if (GameState?.ActivePlayer != null && Owner != null && GameState.ActivePlayer != Owner)
                return false;

            if (Category == CardType.Ritual && GameState != null && GameState.Phase != Game.TurnPhase.Ritual)
                return false;

            var context = new EffectContext
            {
                Source = this,
                GameState = GameState,
                Targets = null,
            };

            session = new CardPlaySession(this, sourceZone, context);
            session.TryAdvance(out candidates);

            if (session.WasCancelled)
            {
                session = null;
                candidates = null;
                return false;
            }

            return true;
        }


        internal void FinishPlay(EffectContext context, ICardZone sourceZone)
        {
            Behavior?.AfterPlayed(context, sourceZone);
        }
    }
}
