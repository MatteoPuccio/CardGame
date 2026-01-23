using System.Collections.Generic;
using System.Threading.Tasks;
using Assets.Scripts.CardEngine.Cards;

namespace Assets.Scripts.CardEngine.Game
{
    public readonly struct SelectCardFromZoneOptions
    {
        public string Title { get; }
        public string Subtitle { get; }
        public int MinSelections { get; }
        public int MaxSelections { get; }
        public bool AllowCancel { get; }

        public SelectCardFromZoneOptions(
            string title,
            string subtitle,
            int minSelections,
            int maxSelections,
            bool allowCancel)
        {
            Title = title;
            Subtitle = subtitle;
            MinSelections = minSelections;
            MaxSelections = maxSelections;
            AllowCancel = allowCancel;
        }
    }

    public readonly struct SelectCardFromZoneRequest
    {
        public Player Player { get; }
        public Card Source { get; }
        public ICardZone Zone { get; }
        public IReadOnlyList<Card> Candidates { get; }

        public string Title { get; }
        public string Subtitle { get; }

        public int MinSelections { get; }
        public int MaxSelections { get; }

        public bool AllowCancel { get; }

        public SelectCardFromZoneRequest(
            Player player,
            Card source,
            ICardZone zone,
            IReadOnlyList<Card> candidates,
            SelectCardFromZoneOptions options)
        {
            Player = player;
            Source = source;
            Zone = zone;
            Candidates = candidates;

            Title = options.Title;
            Subtitle = options.Subtitle;
            MinSelections = options.MinSelections < 0 ? 0 : options.MinSelections;
            MaxSelections = options.MaxSelections < 0 ? 0 : options.MaxSelections;
            if (MaxSelections != 0 && MinSelections > MaxSelections)
                MinSelections = MaxSelections;
            AllowCancel = options.AllowCancel;
        }
    }

    public interface ISelectCardFromZonePrompter
    {
        /// <summary>
        /// Prompts the player to choose cards from the provided candidate list.
        /// Return null to cancel (if allowed) or when no UI is available.
        /// </summary>
        Task<IReadOnlyList<Card>> ChooseCardsAsync(SelectCardFromZoneRequest request);
    }

    /// <summary>
    /// Utility prompter: always cancel / choose nothing.
    /// </summary>
    public sealed class AutoCancelSelectCardFromZonePrompter : ISelectCardFromZonePrompter
    {
        public Task<IReadOnlyList<Card>> ChooseCardsAsync(SelectCardFromZoneRequest request)
            => Task.FromResult<IReadOnlyList<Card>>(null);
    }

    /// <summary>
    /// Utility prompter: always pick the first available card (if any).
    /// Useful for bots/tests.
    /// </summary>
    public sealed class AutoSelectFirstCardFromZonePrompter : ISelectCardFromZonePrompter
    {
        public Task<IReadOnlyList<Card>> ChooseCardsAsync(SelectCardFromZoneRequest request)
        {
            var list = request.Candidates;
            if (list == null || list.Count == 0)
            {
                if (request.MinSelections == 0)
                    return Task.FromResult<IReadOnlyList<Card>>(System.Array.Empty<Card>());
                return Task.FromResult<IReadOnlyList<Card>>(null);
            }

            int max = request.MaxSelections;
            if (max <= 0)
                max = 1;

            var chosen = new List<Card>(max);
            for (int i = 0; i < list.Count && chosen.Count < max; i++)
            {
                if (list[i] != null)
                    chosen.Add(list[i]);
            }

            if (chosen.Count < request.MinSelections)
                return Task.FromResult<IReadOnlyList<Card>>(null);

            return Task.FromResult<IReadOnlyList<Card>>(chosen);
        }
    }
}
