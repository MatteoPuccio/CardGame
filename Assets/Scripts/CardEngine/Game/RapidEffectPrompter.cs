using System.Collections.Generic;
using System.Threading.Tasks;

namespace Assets.Scripts.CardEngine.Game
{
    public interface IRapidEffectPrompter
    {
        Task<RapidEffectOption?> ChooseActivationAsync(Player player, IReadOnlyList<RapidEffectOption> options);
    }

    public sealed class AutoPassRapidEffectPrompter : IRapidEffectPrompter
    {
        public Task<RapidEffectOption?> ChooseActivationAsync(Player player, IReadOnlyList<RapidEffectOption> options)
            => Task.FromResult<RapidEffectOption?>(null);
    }

    public sealed class AutoActivateFirstRapidEffectPrompter : IRapidEffectPrompter
    {
        private readonly bool _onlyLocalPlayer;

        public AutoActivateFirstRapidEffectPrompter(bool onlyLocalPlayer = true)
        {
            _onlyLocalPlayer = onlyLocalPlayer;
        }

        public Task<RapidEffectOption?> ChooseActivationAsync(Player player, IReadOnlyList<RapidEffectOption> options)
        {
            RapidEffectOption? chosen = null;

            if (options == null || options.Count == 0)
                return Task.FromResult(chosen);

            if (_onlyLocalPlayer && player != null && !player.IsLocalPlayer)
                return Task.FromResult(chosen);

            chosen = options[0];
            return Task.FromResult(chosen);
        }
    }
}
