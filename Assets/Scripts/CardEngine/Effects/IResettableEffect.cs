namespace Assets.Scripts.CardEngine.Effects
{
    /// <summary>
    /// Implemented by effects that keep internal state across resolution steps (e.g., targeting sequences).
    /// 
    /// <para><b>IMPORTANT:</b> Any effect that implements <see cref="ISelectableEffect"/> or maintains 
    /// mutable state between <c>TryGetTargetRequest</c>/<c>ApplyTargets</c> calls MUST implement this interface.
    /// Callers (e.g., <c>CardPlaySession</c>) are responsible for calling <c>Reset()</c> before each 
    /// new activation to avoid leaking state from cancelled or previous plays.</para>
    /// 
    /// <para>Effects implementing this interface should clear all targeting state, selected targets,
    /// and any resolution phase tracking back to initial values.</para>
    /// </summary>
    /// <seealso cref="ISelectableEffect"/>
    /// <seealso cref="TargetedEffect"/>
    /// <seealso cref="SequentialEffect"/>
    public interface IResettableEffect
    {
        /// <summary>
        /// Clears all internal state, resetting the effect to its initial configuration.
        /// Must be called before each new activation/play of the effect.
        /// </summary>
        void Reset();
    }
}
