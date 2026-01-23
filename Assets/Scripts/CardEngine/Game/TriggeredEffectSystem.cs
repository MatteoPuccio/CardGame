using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Effects;
using Assets.Scripts.CardEngine.Events;

namespace Assets.Scripts.CardEngine.Game
{
    /// <summary>
    /// Listens to game events and resolves TriggeredEffects (e.g., "When this is sent to cemetery").
    /// </summary>
    public sealed class TriggeredEffectSystem
    {
        private readonly GameState _gameState;

        private readonly Queue<(Card card, IGameEvent triggeringEvent)> _pending = new();
        private bool _processing;

        public TriggeredEffectSystem(GameState gameState)
        {
            _gameState = gameState;
        }

        public void Bind(EventBus bus)
        {
            if (bus == null)
                return;

            bus.Subscribe<CardMovedEvent>(OnCardMoved);
            bus.Subscribe<TroopDiedEvent>(OnTroopDied);
            bus.Subscribe<CardDestroyedEvent>(OnCardDestroyed);
        }

        public void Unbind(EventBus bus)
        {
            if (bus == null)
                return;

            bus.Unsubscribe<CardMovedEvent>(OnCardMoved);
            bus.Unsubscribe<TroopDiedEvent>(OnTroopDied);
            bus.Unsubscribe<CardDestroyedEvent>(OnCardDestroyed);
        }

        private void OnCardMoved(CardMovedEvent e)
        {
            if (e?.Source == null)
                return;

            Enqueue(e.Source, e);
        }

        private void OnTroopDied(TroopDiedEvent e)
        {
            if (e?.Source == null)
                return;

            Enqueue(e.Source, e);
        }

        private void OnCardDestroyed(CardDestroyedEvent e)
        {
            if (e?.Target == null)
                return;

            Enqueue(e.Target, e);
        }

        private void Enqueue(Card card, IGameEvent triggeringEvent)
        {
            if (card == null || triggeringEvent == null)
                return;

            if (_gameState == null)
                return;

            // Avoid unbounded growth if something goes wrong.
            if (_pending.Count > 64)
                return;

            _pending.Enqueue((card, triggeringEvent));

            if (_processing)
                return;

            _ = ProcessQueueAsync();
        }

        private async Task ProcessQueueAsync()
        {
            if (_processing)
                return;

            _processing = true;
            try
            {
                while (_pending.Count > 0)
                {
                    // Don't start triggered effects while the user is in a targeting flow.
                    while (_gameState?.Targeting != null && _gameState.Targeting.IsActive)
                        await Task.Yield();

                    var (card, triggeringEvent) = _pending.Dequeue();
                    await ResolveTriggeredEffectsAsync(card, triggeringEvent);
                }
            }
            finally
            {
                _processing = false;
            }
        }

        private async Task ResolveTriggeredEffectsAsync(Card card, IGameEvent triggeringEvent)
        {
            if (card == null || triggeringEvent == null)
                return;

            if (_gameState == null)
                return;

            var effects = CollectMatchingEffects(card, triggeringEvent);
            if (effects == null || effects.Count == 0)
                return;

            // Resolve in order; optional prompts may filter out effects.
            await ResolveEffectsAsync(card, triggeringEvent, effects);
        }

        private static List<Effect> CollectMatchingEffects(Card card, IGameEvent triggeringEvent)
        {
            var list = card?.TriggeredEffects;
            if (list == null || list.Count == 0)
                return null;

            var effects = new List<Effect>();
            for (int i = 0; i < list.Count; i++)
            {
                var te = list[i];
                if (te == null)
                    continue;

                if (te.Matches(card, triggeringEvent) && te.Effect != null)
                    effects.Add(te.Effect);
            }

            return effects;
        }

        private async Task ResolveEffectsAsync(Card card, IGameEvent triggeringEvent, List<Effect> matchingEffects)
        {
            if (card == null || triggeringEvent == null || matchingEffects == null || matchingEffects.Count == 0)
                return;

            var owner = card.Owner;

            IReadOnlyList<Effect> ordered = matchingEffects;
            if (OptionalEffectPrompting.HasAnyOptional(matchingEffects))
            {
                try
                {
                    var overrideList = await OptionalEffectPrompting.BuildOverrideAsync(_gameState, owner, card, matchingEffects);
                    if (overrideList != null)
                        ordered = overrideList;
                    else
                        ordered = matchingEffects.Where(e => e != null && !e.IsOptional).ToList();
                }
                catch
                {
                    // If optional prompting fails, treat as declining optional effects.
                    ordered = matchingEffects.Where(e => e != null && !e.IsOptional).ToList();
                }
            }

            if (ordered == null || ordered.Count == 0)
                return;

            Effect root = ordered.Count == 1 ? ordered[0] : new SequentialEffect(ordered.ToList());

            var ctx = new EffectContext
            {
                Source = card,
                GameState = _gameState,
                Targets = null,
                TriggeringEvent = triggeringEvent,
            };

            var tcs = new TaskCompletionSource<bool>();
            void Finished(bool success, string cancelReason) => tcs.TrySetResult(success);

            var session = new TriggeredEffectSession(card, root, ctx, Finished);

            if (session.TryAdvance(out var candidates) && candidates != null && candidates.Count > 0)
                _gameState.Targeting.Begin(session, candidates);

            // Await completion to keep triggered effects sequenced.
            try
            {
                var completed = await Task.WhenAny(tcs.Task, Task.Delay(5000));
                if (completed != tcs.Task)
                {
                    // Timeout: don't permanently block later triggers.
                    tcs.TrySetResult(false);
                }
                else
                {
                    await tcs.Task;
                }
            }
            catch
            {
                // Ignore; triggered effects should not throw into the queue processor.
            }
        }
    }
}
