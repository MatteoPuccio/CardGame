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
        public bool IsOptional { get; set; }

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
    /// Implemented by effects that contain child effects (e.g., wrappers and sequences).
    /// Enables generic traversal for pre-resolution preparation steps.
    /// </summary>
    public interface ICompositeEffect
    {
        IEnumerable<Effect> GetChildEffects();
    }

    /// <summary>
    /// Implemented by effects that require asynchronous preparation before resolution
    /// (e.g., prompting the player to choose cards from a zone).
    /// </summary>
    public interface IPreResolveEffect
    {
        System.Threading.Tasks.Task<PreResolveResult> PrepareAsync(EffectContext context);
    }

    public readonly struct PreResolveResult
    {
        public bool Ok { get; }
        public string CancelReason { get; }

        public PreResolveResult(bool ok, string cancelReason)
        {
            Ok = ok;
            CancelReason = cancelReason;
        }

        public static PreResolveResult Success() => new PreResolveResult(true, null);

        public static PreResolveResult Cancel(string reason)
            => new PreResolveResult(false, string.IsNullOrWhiteSpace(reason) ? "Cancelled." : reason);
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




