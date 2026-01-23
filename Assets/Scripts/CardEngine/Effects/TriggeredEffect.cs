using System;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Events;
using Assets.Scripts.CardEngine.Game;
using Assets.Scripts.CardEngine.Utils;
using UnityEngine;

namespace Assets.Scripts.CardEngine.Effects
{
    public interface ITriggeredEffectCondition
    {
        bool Matches(Card source, IGameEvent triggeringEvent);
    }

    [Serializable]
    public abstract class TriggeredEffectConditionDefinition
    {
        public abstract ITriggeredEffectCondition CreateRuntimeCondition();
    }

    [Serializable]
    public sealed class WhenThisIsPlayedConditionDefinition : TriggeredEffectConditionDefinition
    {
        public override ITriggeredEffectCondition CreateRuntimeCondition() => new WhenThisIsPlayedCondition();

        private sealed class WhenThisIsPlayedCondition : ITriggeredEffectCondition
        {
            public bool Matches(Card source, IGameEvent triggeringEvent)
            {
                if (source == null || triggeringEvent == null)
                    return false;

                return triggeringEvent is CardPlayedEvent cpe && cpe.Source == source;
            }
        }
    }

    [Serializable]
    public sealed class WhenThisIsSentToCemeteryConditionDefinition : TriggeredEffectConditionDefinition
    {
        public override ITriggeredEffectCondition CreateRuntimeCondition() => new WhenThisIsSentToCemeteryCondition();

        private sealed class WhenThisIsSentToCemeteryCondition : ITriggeredEffectCondition
        {
            public bool Matches(Card source, IGameEvent triggeringEvent)
            {
                if (source == null || triggeringEvent == null)
                    return false;

                // Primary: explicit move-to-cemetery event.
                if (triggeringEvent is CardMovedEvent moved)
                {
                    if (!string.Equals(moved.To, ZoneNames.Cemetery, StringComparison.Ordinal))
                        return false;
                    return ReferenceEquals(moved.Source, source);
                }

                return false;
            }
        }
    }

    [Serializable]
    public sealed class TriggeredEffectDefinition
    {
        [Tooltip("If true, the owning player may choose to activate this triggered effect or skip it.")]
        public bool optional;

        [SerializeReference] public EffectDefinition effect;
        [SerializeReference] public TriggeredEffectConditionDefinition condition;

        public TriggeredEffect CreateRuntimeTriggeredEffect()
        {
            var runtimeEffect = effect != null ? effect.CreateRuntimeEffect() : null;
            if (runtimeEffect == null)
                return null;

            runtimeEffect.IsOptional = optional;

            var runtimeCondition = condition != null ? condition.CreateRuntimeCondition() : null;
            return new TriggeredEffect(runtimeEffect, runtimeCondition);
        }
    }

    public sealed class TriggeredEffect
    {
        public Effect Effect { get; }
        public ITriggeredEffectCondition Condition { get; }

        public TriggeredEffect(Effect effect, ITriggeredEffectCondition condition)
        {
            Effect = effect;
            Condition = condition;
        }

        public bool Matches(Card source, IGameEvent triggeringEvent)
        {
            if (Effect == null)
                return false;

            return Condition == null || Condition.Matches(source, triggeringEvent);
        }
    }
}
