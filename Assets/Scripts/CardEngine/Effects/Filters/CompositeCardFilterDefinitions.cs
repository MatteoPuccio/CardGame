using System;
using System.Collections.Generic;
using Assets.Scripts.CardEngine.Cards;
using UnityEngine;

namespace Assets.Scripts.CardEngine.Effects
{
    [Serializable]
    public sealed class AndCardFilterDefinition : CardFilterDefinition
    {
        [SerializeReference]
        public List<CardFilterDefinition> filters = new();

        public override ICardFilter CreateRuntimeFilter()
        {
            var runtime = new List<ICardFilter>();
            if (filters != null)
            {
                for (int i = 0; i < filters.Count; i++)
                {
                    var f = filters[i]?.CreateRuntimeFilter();
                    if (f != null)
                        runtime.Add(f);
                }
            }

            return runtime.Count == 0 ? null : new AndCardFilter(runtime);
        }

        private sealed class AndCardFilter : ICardFilter
        {
            private readonly IReadOnlyList<ICardFilter> _filters;

            public AndCardFilter(IReadOnlyList<ICardFilter> filters)
            {
                _filters = filters;
            }

            public bool Matches(Card card, EffectContext context)
            {
                if (_filters == null)
                    return true;

                for (int i = 0; i < _filters.Count; i++)
                {
                    if (_filters[i] != null && !_filters[i].Matches(card, context))
                        return false;
                }

                return true;
            }
        }
    }

    [Serializable]
    public sealed class OrCardFilterDefinition : CardFilterDefinition
    {
        [SerializeReference]
        public List<CardFilterDefinition> filters = new();

        public override ICardFilter CreateRuntimeFilter()
        {
            var runtime = new List<ICardFilter>();
            if (filters != null)
            {
                for (int i = 0; i < filters.Count; i++)
                {
                    var f = filters[i]?.CreateRuntimeFilter();
                    if (f != null)
                        runtime.Add(f);
                }
            }

            return runtime.Count == 0 ? null : new OrCardFilter(runtime);
        }

        private sealed class OrCardFilter : ICardFilter
        {
            private readonly IReadOnlyList<ICardFilter> _filters;

            public OrCardFilter(IReadOnlyList<ICardFilter> filters)
            {
                _filters = filters;
            }

            public bool Matches(Card card, EffectContext context)
            {
                if (_filters == null || _filters.Count == 0)
                    return true;

                for (int i = 0; i < _filters.Count; i++)
                {
                    if (_filters[i] != null && _filters[i].Matches(card, context))
                        return true;
                }

                return false;
            }
        }
    }

    [Serializable]
    public sealed class NotCardFilterDefinition : CardFilterDefinition
    {
        [SerializeReference] public CardFilterDefinition filter;

        public override ICardFilter CreateRuntimeFilter()
        {
            var inner = filter?.CreateRuntimeFilter();
            return inner == null ? null : new NotCardFilter(inner);
        }

        private sealed class NotCardFilter : ICardFilter
        {
            private readonly ICardFilter _inner;

            public NotCardFilter(ICardFilter inner)
            {
                _inner = inner;
            }

            public bool Matches(Card card, EffectContext context)
            {
                return _inner == null || !_inner.Matches(card, context);
            }
        }
    }
}
