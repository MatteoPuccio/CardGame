using System;
using System.Collections.Generic;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Game;

namespace Assets.Scripts.CardEngine.Effects
{
    /// <summary>
    /// Immutable context passed through effect resolution.
    /// Use With* methods to create modified copies for nested effects.
    /// </summary>
    public class EffectContext
    {
        public Card Source { get; }
        public IReadOnlyList<ITargetable> Targets { get; }
        public GameState GameState { get; }
        public IGameEvent TriggeringEvent { get; }

        public EffectContext(
            Card source,
            GameState gameState,
            IReadOnlyList<ITargetable> targets = null,
            IGameEvent triggeringEvent = null)
        {
            Source = source;
            GameState = gameState;
            Targets = targets ?? Array.Empty<ITargetable>();
            TriggeringEvent = triggeringEvent;
        }

        /// <summary>
        /// Creates a new context with different targets, preserving all other properties.
        /// </summary>
        public virtual EffectContext WithTargets(IReadOnlyList<ITargetable> newTargets)
            => new(Source, GameState, newTargets ?? Array.Empty<ITargetable>(), TriggeringEvent);

        /// <summary>
        /// Creates a new context with a different source card.
        /// </summary>
        public virtual EffectContext WithSource(Card newSource)
            => new(newSource, GameState, Targets, TriggeringEvent);

        /// <summary>
        /// Creates a new context with a different triggering event.
        /// </summary>
        public virtual EffectContext WithTriggeringEvent(IGameEvent newEvent)
            => new(Source, GameState, Targets, newEvent);

        // Convenience accessors
        public Player SourceOwner => Source?.Owner;
        public Player Opponent => GameState?.GetOpponent(SourceOwner);
    }
}