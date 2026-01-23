using System;
using Assets.Scripts.CardEngine.Events;
using Assets.Scripts.CardEngine.Game;

namespace Assets.Scripts.CardEngine.Effects
{
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
}
