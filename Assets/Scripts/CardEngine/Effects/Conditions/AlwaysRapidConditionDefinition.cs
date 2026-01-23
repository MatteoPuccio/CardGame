using System;

namespace Assets.Scripts.CardEngine.Effects
{
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
}
