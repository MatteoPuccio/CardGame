using System;
using System.Collections.Generic;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Effects;

namespace Assets.Scripts.CardEngine.Game
{
    public enum TargetingAdvanceState
    {
        NeedsPlayerInput,
        Completed,
        Cancelled,
    }

    public static class TargetingSelectionUtils
    {
        /// <summary>
        /// Advances an <see cref="ITargetingEffect"/> until it either:
        /// - requests player input (returns <see cref="TargetingAdvanceState.NeedsPlayerInput"/> and provides candidates),
        /// - completes selection (returns <see cref="TargetingAdvanceState.Completed"/>),
        /// - cancels (returns <see cref="TargetingAdvanceState.Cancelled"/> and provides a non-empty cancelReason).
        /// </summary>
        public static TargetingAdvanceState AdvanceToRequestOrComplete(
            ITargetingEffect targeting,
            EffectContext context,
            out List<ITargetable> candidates,
            out string cancelReason)
        {
            if (targeting == null)
                throw new ArgumentNullException(nameof(targeting));
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            candidates = null;
            cancelReason = null;

            while (!targeting.IsComplete)
            {
                if (targeting.TryGetTargetRequest(context, out candidates, out cancelReason))
                    return TargetingAdvanceState.NeedsPlayerInput;

                if (!string.IsNullOrEmpty(cancelReason))
                    return TargetingAdvanceState.Cancelled;
            }

            return TargetingAdvanceState.Completed;
        }
    }
}
