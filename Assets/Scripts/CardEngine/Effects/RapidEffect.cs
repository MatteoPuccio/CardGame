using System;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Events;
using Assets.Scripts.CardEngine.Game;
using UnityEngine;

namespace Assets.Scripts.CardEngine.Effects
{
    public sealed class RapidEffectContext : EffectContext
    {
        public Player Activator { get; set; }

        /// <summary>
        /// Optional helper: if this rapid effect is being activated in response to something,
        /// this is the effect/event that opened the window.
        /// </summary>
        public object ChainWindowSource { get; set; }
    }

    public interface IRapidEffectCondition
    {
        bool CanActivate(RapidEffectContext context, out string reason);
    }

    internal sealed class AndRapidEffectCondition : IRapidEffectCondition
    {
        private readonly IRapidEffectCondition _a;
        private readonly IRapidEffectCondition _b;

        internal IRapidEffectCondition Left => _a;
        internal IRapidEffectCondition Right => _b;

        public AndRapidEffectCondition(IRapidEffectCondition a, IRapidEffectCondition b)
        {
            _a = a;
            _b = b;
        }

        public bool CanActivate(RapidEffectContext context, out string reason)
        {
            if (_a != null && !_a.CanActivate(context, out reason))
                return false;
            if (_b != null && !_b.CanActivate(context, out reason))
                return false;

            reason = null;
            return true;
        }
    }

    internal sealed class RitualStageAtLeastRapidCondition : IRapidEffectCondition
    {
        private readonly int _minStageIndex;

        public RitualStageAtLeastRapidCondition(int minStageIndex)
        {
            _minStageIndex = Math.Max(0, minStageIndex);
        }

        public bool CanActivate(RapidEffectContext context, out string reason)
        {
            reason = null;

            var source = context?.Source;
            if (source == null)
            {
                reason = "Missing source.";
                return false;
            }

            if (source.Behavior is not RitualBehavior ritual)
            {
                reason = "Not a ritual.";
                return false;
            }

            if (ritual.StageIndex < _minStageIndex)
            {
                reason = $"Available from ritual stage {_minStageIndex + 1}.";
                return false;
            }

            return true;
        }
    }

    internal sealed class RitualStageEqualsRapidCondition : IRapidEffectCondition
    {
        private readonly int _stageIndex;

        internal int StageIndex => _stageIndex;

        public RitualStageEqualsRapidCondition(int stageIndex)
        {
            _stageIndex = Math.Max(0, stageIndex);
        }

        public bool CanActivate(RapidEffectContext context, out string reason)
        {
            reason = null;

            var source = context?.Source;
            if (source == null)
            {
                reason = "Missing source.";
                return false;
            }

            if (source.Behavior is not RitualBehavior ritual)
            {
                reason = "Not a ritual.";
                return false;
            }

            if (ritual.StageIndex != _stageIndex)
            {
                reason = $"Available at ritual stage {_stageIndex + 1}.";
                return false;
            }

            return true;
        }
    }

    internal sealed class RitualNotAdvancedThisTurnRapidCondition : IRapidEffectCondition
    {
        public bool CanActivate(RapidEffectContext context, out string reason)
        {
            reason = null;

            var source = context?.Source;
            if (source == null)
            {
                reason = "Missing source.";
                return false;
            }

            if (source.Behavior is not RitualBehavior ritual)
            {
                reason = "Not a ritual.";
                return false;
            }

            if (ritual.HasAdvancedThisTurn)
            {
                reason = "Ritual stage already advanced this turn.";
                return false;
            }

            return true;
        }
    }

    internal sealed class RitualMustBeInPlayRapidCondition : IRapidEffectCondition
    {
        public bool CanActivate(RapidEffectContext context, out string reason)
        {
            reason = null;

            var source = context?.Source;
            if (source == null)
            {
                reason = "Missing source.";
                return false;
            }

            if (source.Behavior is not RitualBehavior)
            {
                reason = "Not a ritual.";
                return false;
            }

            var owner = source.Owner;
            if (owner?.Rituals == null)
            {
                reason = "Ritual zone not initialized.";
                return false;
            }

            if (!owner.Rituals.Contains(source))
            {
                reason = "Ritual must be in play.";
                return false;
            }

            return true;
        }
    }

    internal static class RapidEffectConditionUtils
    {
        public static bool TryGetRitualStageIndex(IRapidEffectCondition condition, out int stageIndex)
        {
            stageIndex = default;
            if (condition == null)
                return false;

            if (condition is RitualStageEqualsRapidCondition equals)
            {
                stageIndex = equals.StageIndex;
                return true;
            }

            if (condition is AndRapidEffectCondition and)
            {
                if (TryGetRitualStageIndex(and.Left, out stageIndex))
                    return true;
                if (TryGetRitualStageIndex(and.Right, out stageIndex))
                    return true;
            }

            return false;
        }
    }

    [Serializable]
    public abstract class RapidEffectConditionDefinition
    {
        public abstract IRapidEffectCondition CreateRuntimeCondition();
    }

    [Serializable]
    public sealed class AlwaysRapidConditionDefinition : RapidEffectConditionDefinition
    {
        public override IRapidEffectCondition CreateRuntimeCondition() => new AlwaysRapidCondition();

        private sealed class AlwaysRapidCondition : IRapidEffectCondition
        {
            public bool CanActivate(RapidEffectContext context, out string reason)
            {
                reason = null;
                return true;
            }
        }
    }

    [Serializable]
    public sealed class OpponentDidSomethingConditionDefinition : RapidEffectConditionDefinition
    {
        public override IRapidEffectCondition CreateRuntimeCondition() => new OpponentDidSomethingCondition();

        private sealed class OpponentDidSomethingCondition : IRapidEffectCondition
        {
            public bool CanActivate(RapidEffectContext context, out string reason)
            {
                reason = null;

                if (context?.GameState == null)
                {
                    reason = "Missing game state.";
                    return false;
                }

                if (context.Activator == null)
                {
                    reason = "Missing activator.";
                    return false;
                }

                var actingPlayer = ResolveActingPlayer(context.TriggeringEvent, context.GameState);
                if (actingPlayer == null)
                {
                    reason = "No opponent action detected.";
                    return false;
                }

                if (actingPlayer == context.Activator)
                {
                    reason = "Must respond to an opponent action.";
                    return false;
                }

                return true;
            }

            private static Player ResolveActingPlayer(IGameEvent triggeringEvent, GameState gameState)
            {
                if (triggeringEvent == null || gameState == null)
                    return null;

                return triggeringEvent switch
                {
                    CardPlayedEvent cpe when cpe.Player != null => cpe.Player,
                    EffectActivatedEvent eae when eae.Activator != null => eae.Activator,
                    TroopDamagedEvent tde when tde.Instigator != null => tde.Instigator.Owner,
                    TroopDiedEvent tdie when tdie.Instigator != null => tdie.Instigator.Owner,
                    CardDestroyedEvent cde when cde.Instigator != null => cde.Instigator.Owner,
                    _ => null
                };
            }
        }
    }

    [Serializable]
    public sealed class RapidEffect : Effect, ICostPayingEffect
    {
        public Effect InnerEffect { get; }
        public IRapidEffectCondition Condition { get; }

        public RapidEffect(Effect innerEffect, IRapidEffectCondition condition)
        {
            InnerEffect = innerEffect;
            Condition = condition;
        }

        public override bool IsOncePerTurn => InnerEffect?.IsOncePerTurn ?? false;

        public override bool CanActivate(EffectContext context, out string reason)
        {
            if (context is not RapidEffectContext rapidContext)
            {
                reason = "Rapid effect requires a RapidEffectContext.";
                return false;
            }

            if (InnerEffect == null)
            {
                reason = "Missing inner effect.";
                return false;
            }

            if (Condition != null && !Condition.CanActivate(rapidContext, out reason))
                return false;

            return InnerEffect.CanActivate(context, out reason);
        }

        public bool TryPayCost(EffectContext context, out string reason)
        {
            if (InnerEffect == null)
            {
                reason = "Missing inner effect.";
                return false;
            }

            if (InnerEffect is ICostPayingEffect costPaying)
                return costPaying.TryPayCost(context, out reason);

            reason = null;
            return true;
        }

        protected override void ResolveCore(EffectContext effectContext)
        {
            InnerEffect?.Resolve(effectContext);
        }
    }

    [Serializable]
    public abstract class RapidEffectDefinition : EffectDefinition
    {
        public sealed override Effect CreateRuntimeEffect() => CreateRuntimeRapidEffect();

        public abstract RapidEffect CreateRuntimeRapidEffect();
    }

    [Serializable]
    public sealed class WrappedRapidEffectDefinition : RapidEffectDefinition
    {
        [SerializeReference] public EffectDefinition effect;

        [Tooltip("Optional activation condition (e.g., opponent did something). If omitted, the rapid effect is always activatable.")]
        [SerializeReference] public RapidEffectConditionDefinition condition;

        public override RapidEffect CreateRuntimeRapidEffect()
        {
            var runtimeEffect = effect != null ? effect.CreateRuntimeEffect() : null;
            var runtimeCondition = condition != null ? condition.CreateRuntimeCondition() : null;

            if (runtimeEffect == null)
                return null;

            return new RapidEffect(runtimeEffect, runtimeCondition);
        }
    }

}
