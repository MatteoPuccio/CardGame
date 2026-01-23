using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Effects;

namespace Assets.Scripts.CardEngine.Game
{
    public static class OptionalEffectPrompting
    {
        public static bool HasAnyOptional(IReadOnlyList<Effect> effects)
        {
            if (effects == null || effects.Count == 0)
                return false;

            for (int i = 0; i < effects.Count; i++)
            {
                if (effects[i] != null && effects[i].IsOptional)
                    return true;
            }

            return false;
        }

        public static async Task<IReadOnlyList<Effect>> BuildOverrideAsync(
            GameState gameState,
            Player player,
            Card sourceCard,
            IReadOnlyList<Effect> effects)
        {
            if (gameState == null || sourceCard == null || effects == null || effects.Count == 0)
                return null;

            var optional = CollectOptionalOptions(sourceCard, effects);
            if (optional.Count == 0)
                return null;

            var chosen = await ChooseActivatedOptionalEffectsAsync(gameState.OptionalEffectPrompter, player, optional);
            return FilterInOrder(effects, chosen);
        }

        private static List<OptionalEffectOption> CollectOptionalOptions(Card sourceCard, IReadOnlyList<Effect> effects)
        {
            var optional = new List<OptionalEffectOption>();

            for (int i = 0; i < effects.Count; i++)
            {
                var eff = effects[i];
                if (eff != null && eff.IsOptional)
                    optional.Add(new OptionalEffectOption(sourceCard, eff));
            }

            return optional;
        }

        private static async Task<HashSet<Effect>> ChooseActivatedOptionalEffectsAsync(
            IOptionalEffectPrompter prompter,
            Player player,
            List<OptionalEffectOption> options)
        {
            var activated = new HashSet<Effect>();
            if (options == null || options.Count == 0)
                return activated;

            // If there's no prompter, no optional effects can be activated.
            if (prompter == null)
                return activated;

            var remaining = new List<OptionalEffectOption>(options);
            while (remaining.Count > 0)
            {
                OptionalEffectOption? choice = await prompter.ChooseActivationAsync(player, remaining);
                if (choice == null)
                    break;

                var chosen = choice.Value;
                if (chosen.Effect != null)
                    activated.Add(chosen.Effect);

                RemoveByEffectRef(remaining, chosen.Effect);
            }

            return activated;
        }

        private static IReadOnlyList<Effect> FilterInOrder(IReadOnlyList<Effect> effects, HashSet<Effect> activatedOptional)
        {
            var result = new List<Effect>();

            for (int i = 0; i < effects.Count; i++)
            {
                var eff = effects[i];
                if (eff == null)
                    continue;

                if (!eff.IsOptional || (activatedOptional != null && activatedOptional.Contains(eff)))
                    result.Add(eff);
            }

            return result;
        }

        private static void RemoveByEffectRef(List<OptionalEffectOption> list, Effect effect)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(list[i].Effect, effect))
                    list.RemoveAt(i);
            }
        }
    }
}
