

using System;
using Assets.Scripts.CardEngine.Effects;
using Assets.Scripts.CardEngine.Game;
using System.Collections.Generic;

namespace Assets.Scripts.CardEngine.Cards
{
    public enum CardType
    {
        Troop,
        Spell,
        Ritual,
        Champion,
        None
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
        public int DeployCost;

        public int Power { get; private set; }
        public int MaxHealth { get; private set; }
        public int Health { get; private set; }

        public bool IsDead => Health <= 0;
		public override CardType Category => CardType.Troop;
        public override bool RequiresPlayZone => true;

        public event Action<TroopBehavior> OnStatsChanged;

        public void InitializeStats(int power, int health)
        {
            Power = Math.Max(0, power);
            MaxHealth = Math.Max(0, health);
            Health = MaxHealth;
            OnStatsChanged?.Invoke(this);
        }

        public void ApplyDamage(int amount)
        {
            if (amount <= 0)
                return;
            Health = Math.Max(0, Health - amount);
            OnStatsChanged?.Invoke(this);
        }

        public void Heal(int amount)
        {
            if (amount <= 0)
                return;
            Health = Math.Min(MaxHealth, Health + amount);
            OnStatsChanged?.Invoke(this);
        }


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
             _card.GameState.MoveToZone(
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

        private readonly List<Effect> _stages = new();
        private int _stageIndex;
        private int _lastAdvancedTurnNumber = -1;

        public int StageCount => _stages.Count;
        public int StageIndex => _stageIndex;

        public bool HasAdvancedThisTurn
        {
            get
            {
                var gs = _card?.GameState;
                if (gs == null)
                    return false;
                return _lastAdvancedTurnNumber == gs.TurnNumber;
            }
        }

        public void SetStages(List<Effect> stages)
        {
            _stages.Clear();
            if (stages != null)
                _stages.AddRange(stages);
            _stageIndex = 0;

            foreach (var stage in _stages)
            {
                if (stage is IResettableEffect resettable)
                    resettable.Reset();
            }
        }

        public override void AfterPlayed(EffectContext context, ICardZone sourceZone)
        {
            var gs = _card?.GameState;
            var owner = _card?.Owner;

            if (gs == null || owner == null || owner.Rituals == null || owner.Cemetery == null)
                return;

            if (gs.Phase != TurnPhase.Ritual)
                return;

            if (sourceZone != null)
                gs.MoveToZone(_card, sourceZone, owner.Rituals);
        }

        public void TryAdvanceStage()
        {
            var gs = _card?.GameState;
            var owner = _card?.Owner;
            if (gs == null || owner == null)
                return;

            if (gs.Targeting != null && gs.Targeting.IsActive)
                return;

            // Once per turn.
            if (_lastAdvancedTurnNumber == gs.TurnNumber)
                return;

            if (_stageIndex >= _stages.Count)
            {
                DestroyRitual();
                return;
            }

            var stage = _stages[_stageIndex];

            // Placeholder stages (for rapid-only effects) cannot be advanced by clicking.
            // They are consumed via TryConsumeStageFromRapid when the associated rapid effect resolves.
            if (stage is Effects.IPlaceholderEffect)
                return;

            var ctx = new EffectContext
            {
                Source = _card,
                GameState = gs,
                Targets = null,
                TriggeringEvent = null,
            };

            void StageFinished(bool success, string cancelReason)
            {
                if (!success)
                    return;

                _lastAdvancedTurnNumber = gs.TurnNumber;
                _stageIndex++;

                if (_stageIndex >= _stages.Count)
                    DestroyRitual();
            }

            var session = new Game.EffectResolveSession(_card, stage, ctx, StageFinished);

            if (session.TryAdvance(out var candidates) && candidates != null && candidates.Count > 0)
            {
                gs.Targeting.Begin(session, candidates);
            }
        }

        public bool TryConsumeStageFromRapid(int stageIndex)
        {
            var gs = _card?.GameState;
            if (gs == null)
                return false;

            if (stageIndex != _stageIndex)
                return false;

            // Treat this as the stage having advanced for this turn.
            _lastAdvancedTurnNumber = gs.TurnNumber;
            _stageIndex++;

            if (_stageIndex >= _stages.Count)
                DestroyRitual();

            return true;
        }

        private void DestroyRitual()
        {
            var gs = _card?.GameState;
            var owner = _card?.Owner;
            if (gs == null || owner?.Rituals == null || owner.Cemetery == null)
                return;

            gs.MoveToZone(_card, owner.Rituals, owner.Cemetery);
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