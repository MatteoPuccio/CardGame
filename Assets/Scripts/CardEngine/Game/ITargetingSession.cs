using System.Collections.Generic;
using Assets.Scripts.CardEngine.Cards;

namespace Assets.Scripts.CardEngine.Game
{
    public interface ITargetingSession
    {
        Card Card { get; }

        bool WasCancelled { get; }
        string CancelReason { get; }

        /// <summary>
        /// Advances the session until it needs player input (returns true with candidates), or finishes/cancels (returns false).
        /// </summary>
        bool TryAdvance(out List<ITargetable> candidates);

        void ProvideTargets(List<ITargetable> targets);

        /// <summary>
        /// Cancels the session due to player input/UI action.
        /// </summary>
        void Cancel(string reason = null);
    }
}
