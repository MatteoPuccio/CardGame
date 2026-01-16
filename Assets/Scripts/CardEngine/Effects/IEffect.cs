using System.Collections.Generic;
using Assets.Scripts.CardEngine.Cards;

namespace Assets.Scripts.CardEngine.Effects
{
    /// <summary>
    /// Base class for all effects in the game.
    /// </summary>
    [System.Serializable]
    public abstract class Effect
    {
        /// <summary>
        /// Resolves the effect. Subclasses should override ResolveCore for the actual logic.
        /// This method ensures context and targets are never null.
        /// </summary>
        public void Resolve(EffectContext effectContext)
        {
            if (effectContext == null)
                return;

            // Ensure Targets is never null for effect implementations
            effectContext.Targets ??= new List<ITargetable>();
            
            ResolveCore(effectContext);
        }

        /// <summary>
        /// Override this to implement the effect's logic.
        /// Context and Targets are guaranteed to be non-null.
        /// </summary>
        protected virtual void ResolveCore(EffectContext effectContext) { }

        public virtual bool IsOncePerTurn => false;

        /// <summary>
        /// Returns true when this effect can currently be activated.
        /// If false, provide a short reason for UI/debug.
        /// </summary>
        public virtual bool CanActivate(EffectContext context, out string reason)
        {
            if (context == null)
            {
                reason = "Missing effect context.";
                return false;
            }
            reason = null;
            return true;
        }
    }

    /// <summary>
    /// Interface for effects that have an activation cost.
    /// Only implement this if your effect actually has a cost to pay.
    /// </summary>
    public interface ICostPayingEffect
    {
        /// <summary>
        /// Called exactly once when the player commits to activating this effect.
        /// Return false to cancel activation (e.g., cost could not be paid).
        /// </summary>
        bool TryPayCost(EffectContext context, out string reason);
    }

    /// <summary>
    /// Marker interface for placeholder effects (e.g., ritual stage slots reserved for rapid effects).
    /// Effects implementing this interface should NOT be directly activated by clicking;
    /// they are advanced only through the rapid effect chain system.
    /// </summary>
    public interface IPlaceholderEffect { }
}




