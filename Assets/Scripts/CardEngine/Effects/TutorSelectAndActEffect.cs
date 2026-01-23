using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Game;
using UnityEngine;

namespace Assets.Scripts.CardEngine.Effects
{
    public enum TutorSourceZone
    {
        Deck,
        Cemetery,
        Hand,
        Rituals,
    }

    [Serializable]
    public sealed class TutorSelectAndActEffect : Effect, IPreResolveEffect, IResettableEffect
    {
        public sealed class TutorSelectAndActConfig
        {
            public TutorSourceZone SourceZone;
            public int MinSelections;
            public AmountDefinition MinSelectionsDynamic;
            public int MaxSelections;
            public AmountDefinition MaxSelectionsDynamic;
            public string Title;
            public string Subtitle;
            public ICardFilter CandidateFilter;
            public ITutorResultAction ResultAction;
        }

        private readonly TutorSourceZone _sourceZone;
        private readonly int _minSelections;
        private readonly AmountDefinition _minSelectionsDynamic;
        private readonly int _maxSelections;
        private readonly AmountDefinition _maxSelectionsDynamic;
        private readonly string _title;
        private readonly string _subtitle;
        private readonly ICardFilter _candidateFilter;
        private readonly ITutorResultAction _resultAction;

        private List<Card> _chosen;
        private bool _prepared;

        public TutorSelectAndActEffect(TutorSelectAndActConfig config)
        {
            _sourceZone = config.SourceZone;
            _minSelections = config.MinSelections;
            _minSelectionsDynamic = config.MinSelectionsDynamic;
            _maxSelections = config.MaxSelections;
            _maxSelectionsDynamic = config.MaxSelectionsDynamic;
            _title = config.Title;
            _subtitle = config.Subtitle;
            _candidateFilter = config.CandidateFilter;
            _resultAction = config.ResultAction;
        }

        public void Reset()
        {
            _chosen = null;
            _prepared = false;
        }

        public async Task<PreResolveResult> PrepareAsync(EffectContext context)
        {
            if (_prepared)
                return PreResolveResult.Success();

            if (!TryGetPlayerAndGameState(context, out var gs, out var player, out var sourceCard))
                return PreResolveResult.Cancel("Missing game state or player.");

            var zone = TutorZoneUtils2.GetSourceZone(player, _sourceZone);
            if (zone == null)
                return PreResolveResult.Cancel("Missing source zone.");

            var candidates = FilterCandidates(TutorZoneUtils2.GetCandidates(player, _sourceZone), context);
            if (!TryComputeLimits(context, candidates.Count, out int min, out int max, out bool allowCancel, out var failReason))
                return PreResolveResult.Cancel(failReason);

            var prompter = gs.SelectCardFromZonePrompter;
            if (prompter == null)
                return PreResolveResult.Cancel("Missing SelectCardFromZonePrompter.");

            string title = string.IsNullOrWhiteSpace(_title) ? $"Select from {_sourceZone}" : _title;
            var options = new SelectCardFromZoneOptions(title, _subtitle, min, max, allowCancel);
            var request = new SelectCardFromZoneRequest(player, sourceCard, zone, candidates, options);

            var chosen = await prompter.ChooseCardsAsync(request);
            if (!TryNormalizeAndValidateChoice(chosen, candidates, min, max, out _chosen, out var cancel))
                return PreResolveResult.Cancel(cancel);

            _prepared = true;
            return PreResolveResult.Success();
        }

        private static bool TryGetPlayerAndGameState(EffectContext context, out GameState gs, out Player player, out Card sourceCard)
        {
            gs = context?.GameState;
            sourceCard = context?.Source;
            player = sourceCard?.Owner;
            return gs != null && player != null;
        }

        private bool TryComputeLimits(EffectContext context, int available, out int min, out int max, out bool allowCancel, out string failReason)
        {
            failReason = null;
            min = _minSelectionsDynamic?.Evaluate(context) ?? _minSelections;
            max = _maxSelectionsDynamic?.Evaluate(context) ?? _maxSelections;

            if (min < 0) min = 0;
            if (max < 0) max = 0;
            if (max == 0) max = 1;
            if (min > max) min = max;

            if (available < min)
            {
                allowCancel = false;
                failReason = "Not enough valid cards to choose from.";
                return false;
            }

            if (max > available)
                max = available;

            allowCancel = min == 0;
            return true;
        }

        private static bool TryNormalizeAndValidateChoice(
            IReadOnlyList<Card> chosen,
            IReadOnlyList<Card> candidates,
            int min,
            int max,
            out List<Card> normalized,
            out string cancelReason)
        {
            normalized = null;
            cancelReason = null;

            if (chosen == null)
            {
                if (min == 0)
                    chosen = Array.Empty<Card>();
                else
                {
                    cancelReason = "Selection cancelled.";
                    return false;
                }
            }

            var distinct = chosen.Where(c => c != null).Distinct().ToList();
            if (distinct.Count < min || distinct.Count > max)
            {
                cancelReason = "Invalid selection count.";
                return false;
            }

            var candidateSet = new HashSet<Card>(candidates.Where(c => c != null));
            for (int i = 0; i < distinct.Count; i++)
            {
                if (!candidateSet.Contains(distinct[i]))
                {
                    cancelReason = "Invalid selection.";
                    return false;
                }
            }

            normalized = distinct;
            return true;
        }

        protected override void ResolveCore(EffectContext effectContext)
        {
            if (!_prepared)
                return;

            var gs = effectContext?.GameState;
            var player = effectContext?.Source?.Owner;
            if (gs == null || player == null)
                return;

            if (_chosen == null || _chosen.Count == 0)
                return;

            var fromZone = TutorZoneUtils2.GetSourceZone(player, _sourceZone);
            if (fromZone == null)
                return;

            _resultAction?.Execute(effectContext, player, fromZone, _chosen);
        }

        private IReadOnlyList<Card> FilterCandidates(IReadOnlyList<Card> candidates, EffectContext context)
        {
            if (candidates == null || candidates.Count == 0)
                return Array.Empty<Card>();

            if (_candidateFilter == null)
                return candidates;

            var filtered = new List<Card>(candidates.Count);
            for (int i = 0; i < candidates.Count; i++)
            {
                var c = candidates[i];
                if (c == null)
                    continue;
                if (_candidateFilter.Matches(c, context))
                    filtered.Add(c);
            }

            return filtered;
        }
    }

    [Serializable]
    public sealed class TutorSelectAndActEffectDefinition : EffectDefinition
    {
        [Header("Selection")]
        [SerializeField] private TutorSourceZone _sourceZone = TutorSourceZone.Deck;

        [Min(0)] public int MinSelections = 1;
        [SerializeReference] public AmountDefinition MinSelectionsDynamic;

        [Min(0)] public int MaxSelections = 1;
        [SerializeReference] public AmountDefinition MaxSelectionsDynamic;

        [Header("UI")]
        [SerializeField] private string _title;
        [TextArea]
        [SerializeField] private string _subtitle;

        [Header("Candidate Filter")]
        [Tooltip("Optional. Restricts which cards can be selected (type, race, keyword, tags/archetype, etc.).")]
        [SerializeReference] public CardFilterDefinition CandidateFilter;

        [Header("Result Action")]
        [Tooltip("What to do with the selected cards.")]
        [SerializeReference] public TutorResultActionDefinition ResultAction;

        protected override Effect CreateRuntimeEffectCore()
            => new TutorSelectAndActEffect(
                new TutorSelectAndActEffect.TutorSelectAndActConfig
                {
                    SourceZone = _sourceZone,
                    MinSelections = MinSelections,
                    MinSelectionsDynamic = MinSelectionsDynamic,
                    MaxSelections = MaxSelections,
                    MaxSelectionsDynamic = MaxSelectionsDynamic,
                    Title = _title,
                    Subtitle = _subtitle,
                    CandidateFilter = CandidateFilter?.CreateRuntimeFilter(),
                    ResultAction = ResultAction?.CreateRuntimeAction(),
                });
    }

    internal static class TutorZoneUtils2
    {
        public static ICardZone GetSourceZone(Player player, TutorSourceZone sourceZone)
        {
            if (player == null)
                return null;

            return sourceZone switch
            {
                TutorSourceZone.Deck => player.Deck,
                TutorSourceZone.Cemetery => player.Cemetery,
                TutorSourceZone.Hand => player.Hand,
                TutorSourceZone.Rituals => player.Rituals,
                _ => null,
            };
        }

        public static IReadOnlyList<Card> GetCandidates(Player player, TutorSourceZone sourceZone)
        {
            if (player == null)
                return Array.Empty<Card>();

            return sourceZone switch
            {
                TutorSourceZone.Deck => player.Deck?.GetAllCards()?.Where(c => c != null).ToList() ?? (IReadOnlyList<Card>)Array.Empty<Card>(),
                TutorSourceZone.Cemetery => player.Cemetery?.Cards?.Where(c => c != null).ToList() ?? (IReadOnlyList<Card>)Array.Empty<Card>(),
                TutorSourceZone.Hand => player.Hand?.Cards?.Where(c => c != null).ToList() ?? (IReadOnlyList<Card>)Array.Empty<Card>(),
                TutorSourceZone.Rituals => player.Rituals?.Cards?.Where(c => c != null).ToList() ?? (IReadOnlyList<Card>)Array.Empty<Card>(),
                _ => Array.Empty<Card>(),
            };
        }
    }
}
