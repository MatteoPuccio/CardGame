using System.Collections.Generic;
using System.Threading.Tasks;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Effects;

namespace Assets.Scripts.CardEngine.Game
{
    public readonly struct OptionalEffectOption
    {
        public readonly Card SourceCard;
        public readonly Effect Effect;

        public OptionalEffectOption(Card sourceCard, Effect effect)
        {
            SourceCard = sourceCard;
            Effect = effect;
        }

        public override string ToString()
        {
            string cardName = SourceCard != null ? SourceCard.Name : "<card>";
            string effectName = Effect != null ? Effect.GetType().Name : "<effect>";
            return $"{cardName} — {effectName}";
        }
    }

    public interface IOptionalEffectPrompter
    {
        /// <summary>
        /// Like the rapid effect prompt: pick one optional effect to activate, or return null to pass.
        /// Callers can loop until null to allow activating multiple optional effects.
        /// </summary>
        Task<OptionalEffectOption?> ChooseActivationAsync(Player player, IReadOnlyList<OptionalEffectOption> options);
    }

    /// <summary>
    /// Utility prompter: always pass (decline all).
    /// </summary>
    public sealed class AutoDeclineOptionalEffectPrompter : IOptionalEffectPrompter
    {
        public Task<OptionalEffectOption?> ChooseActivationAsync(Player player, IReadOnlyList<OptionalEffectOption> options)
            => Task.FromResult<OptionalEffectOption?>(null);
    }
}
