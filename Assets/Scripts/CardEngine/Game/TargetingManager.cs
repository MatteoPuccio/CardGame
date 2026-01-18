using System;
using System.Collections.Generic;
using Assets.Scripts.CardEngine.Cards;
using UnityEngine;

namespace Assets.Scripts.CardEngine.Game
{
    public sealed class TargetingManager
    {
        private ITargetingSession _session;
        private HashSet<ITargetable> _candidates;
        private Action _onCancelled;

        public bool IsActive => _session != null;

        public Card SourceCard => _session?.Card;

        public void Begin(ITargetingSession session, List<ITargetable> candidates, Action onCancelled = null)
        {
            if (session == null)
                throw new ArgumentNullException(nameof(session));

            _session = session;
            _candidates = candidates != null ? new (candidates) : new();
            _onCancelled = onCancelled;

            Debug.Log($"Targeting: Select a target for '{session.Card?.Name}'. Candidates: {_candidates.Count}");
        }

        public void Cancel(string reason = null)
        {
            if (!IsActive)
                return;

            Debug.Log($"Targeting: Cancelled for '{_session?.Card?.Name ?? "<null>"}'.");

            var session = _session;
            var cb = _onCancelled;

            _session = null;
            _candidates = null;
            _onCancelled = null;

            try
            {
                session?.Cancel(reason ?? "Cancelled by player.");
            }
            finally
            {
                cb?.Invoke();
            }
        }

        public bool TrySelect(ITargetable target)
        {
            if (!IsActive)
                return false;

            // While targeting, block other clicks even if invalid.
            if (target == null || _candidates == null || !_candidates.Contains(target))
                return true;

            var chosen = target;
            var session = _session;

            Debug.Log($"Targeting: Selected target '{(target as Card)?.Name ?? target.ToString()}' for '{session?.Card?.Name ?? "<null>"}'.");

            session?.ProvideTargets(new List<ITargetable> { chosen });

            // Continue resolution; if more targets are needed, keep targeting active.
            if (session != null && session.TryAdvance(out var nextCandidates) && nextCandidates != null && nextCandidates.Count > 0)
            {
                _candidates = new HashSet<ITargetable>(nextCandidates);
                Debug.Log($"Targeting: Next selection for '{session.Card?.Name}'. Candidates: {_candidates.Count}");
                return true;
            }

            // Finished (or no more valid candidates): clear targeting state.
            _session = null;
            _candidates = null;
            _onCancelled = null;
            return true;
        }
    }
}
