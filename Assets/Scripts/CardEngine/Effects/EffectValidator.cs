using System.Collections.Generic;
using Assets.Scripts.CardEngine.Cards;

namespace Assets.Scripts.CardEngine.Effects
{
    /// <summary>
    /// Result of effect validation containing all errors found.
    /// </summary>
    public readonly struct ValidationResult
    {
        private readonly List<ValidationError> _errors;

        public IReadOnlyList<ValidationError> Errors => _errors ?? (IReadOnlyList<ValidationError>)System.Array.Empty<ValidationError>();
        public bool IsValid => _errors == null || _errors.Count == 0;

        /// <summary>
        /// Returns a single combined reason string, or null if valid.
        /// </summary>
        public string CombinedReason
        {
            get
            {
                if (IsValid)
                    return null;

                if (_errors.Count == 1)
                    return _errors[0].Reason;

                var sb = new System.Text.StringBuilder();
                for (int i = 0; i < _errors.Count; i++)
                {
                    if (i > 0)
                        sb.Append("; ");
                    sb.Append(_errors[i].Reason);
                }
                return sb.ToString();
            }
        }

        private ValidationResult(List<ValidationError> errors)
        {
            _errors = errors;
        }

        public static ValidationResult Valid() => new(null);

        public static ValidationResult Invalid(List<ValidationError> errors) => new(errors);

        public static ValidationResult FromReason(Effect effect, string reason)
        {
            if (string.IsNullOrEmpty(reason))
                return Valid();

            return new(new List<ValidationError> { new(effect, reason) });
        }
    }

    /// <summary>
    /// A single validation error with the effect that caused it and the reason.
    /// </summary>
    public readonly struct ValidationError
    {
        public Effect Effect { get; }
        public string Reason { get; }

        public ValidationError(Effect effect, string reason)
        {
            Effect = effect;
            Reason = reason ?? "Unknown validation error.";
        }

        public override string ToString() =>
            Effect != null
                ? $"{Effect.GetType().Name}: {Reason}"
                : Reason;
    }

    /// <summary>
    /// Centralized validator for effects. Validates an effect tree recursively,
    /// checking CanActivate, targeting validity, and child effects.
    /// </summary>
    public sealed class EffectValidator
    {
        /// <summary>
        /// If true, validation stops at the first error found (faster).
        /// If false, collects all errors (better for debugging/UI).
        /// </summary>
        public bool StopOnFirstError { get; set; }

        /// <summary>
        /// Maximum depth for recursive validation to prevent infinite loops.
        /// </summary>
        public int MaxDepth { get; set; } = 32;

        public EffectValidator(bool stopOnFirstError = false)
        {
            StopOnFirstError = stopOnFirstError;
        }

        /// <summary>
        /// Validates the effect and all its children recursively.
        /// </summary>
        public ValidationResult Validate(Effect effect, EffectContext context)
        {
            if (effect == null)
                return ValidationResult.FromReason(null, "Effect is null.");

            if (context == null)
                return ValidationResult.FromReason(effect, "Context is null.");

            var errors = new List<ValidationError>();
            ValidateRecursive(effect, context, errors, depth: 0);

            return errors.Count == 0
                ? ValidationResult.Valid()
                : ValidationResult.Invalid(errors);
        }

        /// <summary>
        /// Quick check that returns true/false without collecting all errors.
        /// More efficient when you just need a boolean result.
        /// </summary>
        public bool IsValid(Effect effect, EffectContext context, out string reason)
        {
            if (effect == null)
            {
                reason = "Effect is null.";
                return false;
            }

            if (context == null)
            {
                reason = "Context is null.";
                return false;
            }

            return IsValidRecursive(effect, context, out reason, depth: 0);
        }

        private void ValidateRecursive(Effect effect, EffectContext context, List<ValidationError> errors, int depth)
        {
            if (TryHandleDepthExceeded(effect, errors, depth))
                return;

            ValidateCanActivate(effect, context, errors);
            if (StopOnFirstError && errors.Count > 0)
                return;

            ValidateTargetingIfNeeded(effect, context, errors);
            if (StopOnFirstError && errors.Count > 0)
                return;

            ValidateChildrenIfAny(effect, context, errors, depth);
        }

        private bool TryHandleDepthExceeded(Effect effect, List<ValidationError> errors, int depth)
        {
            if (depth <= MaxDepth)
                return false;

            errors.Add(new ValidationError(effect, $"Validation depth exceeded {MaxDepth}. Possible cycle in effect tree."));
            return true;
        }

        private static void ValidateCanActivate(Effect effect, EffectContext context, List<ValidationError> errors)
        {
            if (effect.CanActivate(context, out var reason))
                return;

            errors.Add(new ValidationError(effect, reason ?? "Cannot activate."));
        }

        private static void ValidateTargetingIfNeeded(Effect effect, EffectContext context, List<ValidationError> errors)
        {
            if (effect is not ITargetingEffect targetingEffect)
                return;

            ValidateTargeting(effect, targetingEffect, context, errors);
        }

        private void ValidateChildrenIfAny(Effect effect, EffectContext context, List<ValidationError> errors, int depth)
        {
            if (effect is not ICompositeEffect composite)
                return;

            foreach (var child in composite.GetChildEffects())
            {
                if (child == null)
                    continue;

                ValidateRecursive(child, context, errors, depth + 1);

                if (StopOnFirstError && errors.Count > 0)
                    return;
            }
        }

        private bool IsValidRecursive(Effect effect, EffectContext context, out string reason, int depth)
        {
            reason = null;

            if (depth > MaxDepth)
            {
                reason = $"Validation depth exceeded {MaxDepth}. Possible cycle in effect tree.";
                return false;
            }

            // 1. Check the effect's own CanActivate
            if (!effect.CanActivate(context, out reason))
                return false;

            // 2. Check targeting validity
            if (effect is ITargetingEffect)
            {
                // For quick validation, we don't deeply validate targeting
                // since TryGetTargetRequest handles that at runtime.
                // This could be expanded if needed.
            }

            // 3. Recursively validate children
            if (effect is ICompositeEffect composite)
            {
                foreach (var child in composite.GetChildEffects())
                {
                    if (child == null)
                        continue;

                    if (!IsValidRecursive(child, context, out reason, depth + 1))
                        return false;
                }
            }

            return true;
        }

        private static void ValidateTargeting(
            Effect effect,
            ITargetingEffect targeting,
            EffectContext context,
            List<ValidationError> errors)
        {
            // Try to get a target request to see if targeting would fail
            // Note: This is a read-only check - we don't actually advance the targeting state
            if (targeting.TryGetTargetRequest(context, out var candidates, out var cancelReason))
            {
                // Targeting requires player input - check if there are any valid candidates
                if (!effect.IsOptional && (candidates == null || candidates.Count == 0))
                    errors.Add(new ValidationError(effect, "No valid targets available."));
            }
            else if (!string.IsNullOrEmpty(cancelReason))
            {
                // Targeting system indicated this play should be cancelled
                errors.Add(new ValidationError(effect, cancelReason));
            }
            // If TryGetTargetRequest returns false with no cancel reason, targeting is complete
            // (e.g., auto-selected or no targets needed)
        }
    }

    /// <summary>
    /// Extension methods for convenient validation.
    /// </summary>
    public static class EffectValidationExtensions
    {
        private static readonly EffectValidator s_defaultValidator = new(stopOnFirstError: true);
        private static readonly EffectValidator s_fullValidator = new(stopOnFirstError: false);

        /// <summary>
        /// Quick check if this effect can be activated in the given context.
        /// </summary>
        public static bool CanActivateRecursive(this Effect effect, EffectContext context, out string reason)
        {
            return s_defaultValidator.IsValid(effect, context, out reason);
        }

        /// <summary>
        /// Validates this effect and returns a full result with all errors.
        /// </summary>
        public static ValidationResult ValidateFully(this Effect effect, EffectContext context)
        {
            return s_fullValidator.Validate(effect, context);
        }
    }
}
