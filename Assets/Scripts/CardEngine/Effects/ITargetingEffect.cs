using System.Collections.Generic;
using Assets.Scripts.CardEngine.Cards;

namespace Assets.Scripts.CardEngine.Effects
{
    /// <summary>
    /// Implemented by effects that require player target selection.
    ///
    /// Two-phase model:
    /// - Phase 1 (selection): use TryGetTargetRequest / ApplyTargets until IsComplete.
    /// - Phase 2 (resolution): call ResolveAfterTargets once, after the chain/interrupt window.
    /// </summary>
    public interface ITargetingEffect
    {
        /// <summary>
        /// True when all required target selections for this effect have been completed.
        /// This does not imply the effect has resolved.
        /// </summary>
        bool IsComplete { get; }

        /// <summary>
        /// Resolves the effect after all required targets have been selected.
        /// Target selection should be performed via TryGetTargetRequest/ApplyTargets.
        /// </summary>
        void ResolveAfterTargets(EffectContext context);

        /// <summary>
        /// Advances internal state as much as possible.
        /// Returns true if player input is required to continue.
        /// If the effect determines the overall play is illegal (e.g., required target but none exist),
        /// it should return false and provide a non-null cancelReason.
        /// </summary>
        bool TryGetTargetRequest(EffectContext context, out List<ITargetable> candidates, out string cancelReason);

        /// <summary>
        /// Supplies the selected targets and advances selection state.
        /// </summary>
        void ApplyTargets(EffectContext context, List<ITargetable> targets);
    }
}
