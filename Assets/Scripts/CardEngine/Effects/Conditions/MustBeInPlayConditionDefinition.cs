using System;

namespace Assets.Scripts.CardEngine.Effects
{
    [Serializable]
    public sealed class MustBeInPlayConditionDefinition : RapidEffectConditionDefinition
    {
        public override IRapidEffectCondition CreateRuntimeCondition() => new MustBeInPlayCondition();

        private sealed class MustBeInPlayCondition : IRapidEffectCondition
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

                var owner = source.Owner;
                if (owner == null)
                {
                    reason = "Missing owner.";
                    return false;
                }

                // In play means: occupying any play zone slot OR in the ritual zone.
                if (owner.PlayZones != null)
                {
                    for (int i = 0; i < owner.PlayZones.Count; i++)
                    {
                        if (ReferenceEquals(owner.PlayZones[i]?.OccupyingCard, source))
                            return true;
                    }
                }

                if (owner.Rituals != null && owner.Rituals.Contains(source))
                    return true;

                reason = "Card must be in play.";
                return false;
            }
        }
    }
}
