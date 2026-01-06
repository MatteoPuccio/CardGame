

using System;
using Assets.Scripts.CardEngine.Effects;
using Assets.Scripts.CardEngine.Game;

namespace Assets.Scripts.CardEngine.Cards
{
    public enum CardType
    {
        Troop,
        Spell,
        Ritual,
        Champion
    }

    public abstract class CardBehavior
    {
        protected readonly Card _card;

        protected CardBehavior(Card card)
        {
            _card = card;
        }

        public abstract string Name { get; }
        public abstract CardType Category { get; }

		public virtual bool RequiresPlayZone => false;

        public virtual void AfterPlayed(EffectContext context, ICardZone sourceZone)
        {
           
        }

        public static CardBehavior Create(Card card, CardType category)
        {
            return category switch
            {
                CardType.Troop => new TroopBehavior(card),
                CardType.Spell => new SpellBehavior(card),
                CardType.Ritual => new RitualBehavior(card),
                CardType.Champion => new ChampionBehavior(card),
                _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unsupported card category")
            };
        }
    }

    public class TroopBehavior : CardBehavior
    {
		public TroopBehavior(Card card) : base(card) { }
		public override string Name => "Troop";
		public override CardType Category => CardType.Troop;
        public override bool RequiresPlayZone => true;

        public override void AfterPlayed(EffectContext context, ICardZone sourceZone)
        {

        }
    }

    public class SpellBehavior : CardBehavior
    {
		public SpellBehavior(Card card) : base(card) { }
		public override string Name => "Spell";
		public override CardType Category => CardType.Spell;

        public override void AfterPlayed(EffectContext context, ICardZone sourceZone)
        {
             _card.GameState.TryMoveToZone(
                _card,
                sourceZone,
                _card.Owner.Cemetery
            );
        }
    }

    public class RitualBehavior : CardBehavior
    {
		public RitualBehavior(Card card) : base(card) { }
		public override string Name => "Ritual";
		public override CardType Category => CardType.Ritual;

        public override void AfterPlayed(EffectContext context, ICardZone sourceZone)
        {
        }
    }

    public class ChampionBehavior : CardBehavior
    {
		public ChampionBehavior(Card card) : base(card) { }
		public override string Name => "Champion";
		public override CardType Category => CardType.Champion;

        public override void AfterPlayed(EffectContext context, ICardZone sourceZone)
        {
        }
    }
}