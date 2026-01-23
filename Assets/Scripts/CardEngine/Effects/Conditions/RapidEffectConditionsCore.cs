using System;
using System.Collections.Generic;
using Assets.Scripts.CardEngine.Cards;

namespace Assets.Scripts.CardEngine.Effects
{
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
        public static bool TryGetRitualStageIndex(IReadOnlyList<IRapidEffectCondition> conditions, out int stageIndex)
        {
            stageIndex = default;
            if (conditions == null || conditions.Count == 0)
                return false;

            for (int i = 0; i < conditions.Count; i++)
            {
                if (conditions[i] is RitualStageEqualsRapidCondition equals)
                {
                    stageIndex = equals.StageIndex;
                    return true;
                }
            }

            return false;
        }
    }

    [Serializable]
    public abstract class RapidEffectConditionDefinition
    {
        public abstract IRapidEffectCondition CreateRuntimeCondition();
    }
}
