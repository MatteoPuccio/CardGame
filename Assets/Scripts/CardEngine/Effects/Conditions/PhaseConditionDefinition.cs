using System;
using Assets.Scripts.CardEngine.Game;
using UnityEngine;

namespace Assets.Scripts.CardEngine.Effects
{
    [Serializable]
    public sealed class PhaseConditionDefinition : RapidEffectConditionDefinition
    {
        [Tooltip("The rapid effect is only activatable during this phase.")]
        public TurnPhase phase = TurnPhase.Play;

        public override IRapidEffectCondition CreateRuntimeCondition() => new PhaseCondition(phase);

        private sealed class PhaseCondition : IRapidEffectCondition
        {
            private readonly TurnPhase _phase;

            public PhaseCondition(TurnPhase phase)
            {
                _phase = phase;
            }

            public bool CanActivate(RapidEffectContext context, out string reason)
            {
                reason = null;

                var gameState = context?.GameState;
                if (gameState == null)
                {
                    reason = "Missing game state.";
                    return false;
                }

                if (gameState.Phase != _phase)
                {
                    reason = $"Only during {_phase} phase.";
                    return false;
                }

                return true;
            }
        }
    }
}
