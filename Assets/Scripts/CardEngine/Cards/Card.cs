using System.Collections.Generic;
using System;
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

        private readonly HashSet<string> _tags = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyCollection<string> Tags => _tags;

        public Player Owner { get; }
        public GameState GameState { get; set; }
        public List<RapidEffect> RapidEffects { get; } = new();
        public List<TriggeredEffect> TriggeredEffects { get; } = new();

        public Card(
            string id, 
            CardType cardCategory, 
            Player owner,
            string name = "",
            string effectText = "",
            GameState gameState = null,
            IEnumerable<string> tags = null
        )
        {
            Id = id;
            Name = name;
            EffectText = effectText;
			Category = cardCategory;
            Owner = owner;
            GameState = gameState;
			Behavior = CardBehavior.Create(this, cardCategory);

            if (tags != null)
            {
                foreach (var t in tags)
                {
                    if (string.IsNullOrWhiteSpace(t))
                        continue;
                    _tags.Add(t.Trim());
                }
            }
        }

        public bool HasTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return false;
            return _tags.Contains(tag.Trim());
        }

        private Effect BuildPlayRootEffect(IReadOnlyList<Effect> playEffectsOverride)
        {
            if (playEffectsOverride != null)
            {
                if (playEffectsOverride.Count == 0)
                    return null;
                if (playEffectsOverride.Count == 1)
                    return playEffectsOverride[0];
                return new SequentialEffect(new List<Effect>(playEffectsOverride));
            }

            if (TriggeredEffects == null || TriggeredEffects.Count == 0)
                return null;

            var preview = new CardPlayedEvent(card: this, player: Owner);
            var effects = new List<Effect>();
            for (int i = 0; i < TriggeredEffects.Count; i++)
            {
                var te = TriggeredEffects[i];
                if (te == null)
                    continue;
                if (te.Matches(this, preview) && te.Effect != null)
                    effects.Add(te.Effect);
            }

            if (effects.Count == 0)
                return null;
            if (effects.Count == 1)
                return effects[0];
            return new SequentialEffect(effects);
        }

        public bool TryBeginPlay(ICardZone sourceZone, IReadOnlyList<Effect> playEffectsOverride, out CardPlaySession session, out List<ITargetable> candidates)
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

            var playRootEffect = BuildPlayRootEffect(playEffectsOverride);

            session = new CardPlaySession(this, sourceZone, context, playRootEffect);
            session.TryAdvance(out candidates);

            if (session.WasCancelled)
            {
                session = null;
                candidates = null;
                return false;
            }

            return true;
        }

        public bool TryBeginPlay(ICardZone sourceZone, out CardPlaySession session, out List<ITargetable> candidates)
        {
            return TryBeginPlay(sourceZone, playEffectsOverride: null, out session, out candidates);
        }


        internal void FinishPlay(EffectContext context, ICardZone sourceZone)
        {
            Behavior?.AfterPlayed(context, sourceZone);
        }
    }
}
