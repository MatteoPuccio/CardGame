using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Effects;
using Assets.Scripts.CardEngine.Events;
using Assets.Scripts.CardEngine.Utils;
using UnityEngine;

namespace Assets.Scripts.CardEngine.Game
{
    /// <summary>
    /// Manages the chain window system for rapid effects.
    /// Handles priority passing, option enumeration, and LIFO resolution.
    /// </summary>
    public sealed class RapidEffectChainSystem
    {
        /// <summary>
        /// Maximum frames to wait for an active targeting session to complete before aborting the chain window.
        /// </summary>
        private const int MaxTargetingWaitFrames = 3;

        private readonly GameState _gameState;
        private readonly EventBus _bus;

        private bool _inChainWindow;

        public IRapidEffectPrompter Prompter { get; set; } = new AutoPassRapidEffectPrompter();

        public RapidEffectChainSystem(GameState gameState)
        {
            _gameState = gameState ?? throw new ArgumentNullException(nameof(gameState));
            _bus = _gameState.EventBus;
        }

        public void Bind()
        {
            _bus?.Subscribe<TroopDamagedEvent>(OnTroopDamaged);
            _bus?.Subscribe<TroopDiedEvent>(OnTroopDied);
            _bus?.Subscribe<CardDestroyedEvent>(OnCardDestroyed);
        }

        public void Unbind()
        {
            _bus?.Unsubscribe<TroopDamagedEvent>(OnTroopDamaged);
            _bus?.Unsubscribe<TroopDiedEvent>(OnTroopDied);
            _bus?.Unsubscribe<CardDestroyedEvent>(OnCardDestroyed);
        }

        private static async Task FireAndForgetAsync(Task task)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private void OnTroopDamaged(TroopDamagedEvent e)
        {
            if (e == null)
                return;
            _ = FireAndForgetAsync(TryOpenChainWindowAsync(e));
        }

        private void OnTroopDied(TroopDiedEvent e)
        {
            if (e == null)
                return;
            _ = FireAndForgetAsync(TryOpenChainWindowAsync(e));
        }

        private void OnCardDestroyed(CardDestroyedEvent e)
        {
            if (e == null)
                return;
            _ = FireAndForgetAsync(TryOpenChainWindowAsync(e));
        }


        public async Task<bool> TryOpenChainWindowAsync(IGameEvent triggeringEvent)
        {
            if (triggeringEvent == null)
                return false;

            if (_inChainWindow)
                return false;

            if (_gameState.Targeting != null && _gameState.Targeting.IsActive)
            {
                for (int i = 0; i < MaxTargetingWaitFrames && _gameState.Targeting.IsActive; i++)
                    await Task.Yield();

                if (_gameState.Targeting.IsActive)
                    return false;
            }

            Player firstPriority = ResolveFirstPriorityPlayer(triggeringEvent) ?? _gameState.GetOpponent(_gameState.ActivePlayer);
            Player secondPriority = firstPriority != null ? _gameState.GetOpponent(firstPriority) : null;

            if (firstPriority == null || secondPriority == null)
                return false;

            // Early check: if nobody can respond, don't open a window.
            int firstCount = GetActivatableOptions(firstPriority, triggeringEvent).Count;
            int secondCount = GetActivatableOptions(secondPriority, triggeringEvent).Count;

            if (firstCount == 0 && secondCount == 0)
            {
                Debug.Log($"[Chain] No rapid effects available in response to {triggeringEvent.GetType().Name}; skipping window.");
                return false;
            }

            try
            {
                _inChainWindow = true;

                Debug.Log($"[Chain] Window opened by {triggeringEvent.GetType().Name}. " +
                          $"Priority: {firstPriority.Name}({firstCount}) -> {secondPriority.Name}({secondCount})");
                await RunChainWindowAsync(triggeringEvent, firstPriority, secondPriority);
                return true;
            }
            finally
            {
                _inChainWindow = false;
            }
        }

        private async Task RunChainWindowAsync(IGameEvent triggeringEvent, Player firstPriority, Player secondPriority)
        {
            var chain = new List<ChainLink>();
            var activatedThisWindow = new HashSet<RapidEffect>();

            int consecutivePasses = 0;
            var current = firstPriority;
            var other = secondPriority;

            while (consecutivePasses < 2)
            {
                var chosenMaybe = await ChooseValidOptionAsync(current, triggeringEvent, activatedThisWindow);
                var linkMaybe = chosenMaybe.HasValue
                    ? await TryCreateChainLinkAsync(chosenMaybe.Value, current, triggeringEvent)
                    : null;

                if (!linkMaybe.HasValue)
                {
                    consecutivePasses++;
                    Swap(ref current, ref other);
                    continue;
                }

                Debug.Log($"[Chain] Link {chain.Count + 1}: {current?.Name ?? "<null>"} activates {chosenMaybe.Value}");
                chain.Add(linkMaybe.Value);
                if (chosenMaybe.Value.Effect != null)
                    activatedThisWindow.Add(chosenMaybe.Value.Effect);
                consecutivePasses = 0;
                Swap(ref current, ref other);
            }

            ResolveChainLifo(chain);
        }

        private async Task<ChainLink?> TryCreateChainLinkAsync(RapidEffectOption chosen, Player activator, IGameEvent triggeringEvent)
        {
            if (chosen.Effect == null || chosen.SourceCard == null)
                return null;

            var ctx = CreateRapidContext(chosen.SourceCard, activator, triggeringEvent);

            if (!chosen.Effect.CanActivate(ctx, out var cantReason))
            {
                Debug.Log($"RapidEffect: Cannot activate {chosen}: {cantReason}");
                return null;
            }

            // If the rapid effect needs target selection, lock in targets now.
            // Without this, TargetedEffect will treat the selector's candidate list as actual targets.
            if (chosen.Effect.InnerEffect is ITargetingEffect targeting)
            {
                bool ok = await TryLockInTargetsAsync(chosen.SourceCard, targeting, ctx);
                if (!ok)
                    return null;
            }

            // Some effects require async preparation (e.g., selecting cards from a zone).
            // Do this before costs are paid so a cancellation prevents commitment.
            var prep = await PreResolvePreparationUtils.PrepareAsync(chosen.Effect, ctx);
            if (!prep.Ok)
                return null;

            if (!chosen.Effect.TryPayCost(ctx, out var costReason))
            {
                Debug.Log($"RapidEffect: Cost not paid for {chosen}: {costReason}");
                return null;
            }

            // Publish activation events immediately upon commitment so UI/game systems can react.
            _bus?.Publish(new CardPlayedEvent(card: chosen.SourceCard, player: activator));
            _bus?.Publish(new EffectActivatedEvent(source: chosen.SourceCard, effect: chosen.Effect, activator: activator));

            // If the chosen rapid effect comes from a Spell currently in hand, treat it as "played":
            // remove it from hand and move it to cemetery right away.
            // This makes the prompt activation behave like an actual play.
            var owner = chosen.SourceCard.Owner;
            if (chosen.SourceCard.Category == CardType.Spell && owner?.Hand != null && owner.Cemetery != null &&
                owner.Hand.Cards != null && owner.Hand.Cards.Contains(chosen.SourceCard))
                _gameState.MoveToZone(chosen.SourceCard, owner.Hand, owner.Cemetery);

            return new ChainLink(chosen.Effect, ctx);
        }

        private async Task<bool> TryLockInTargetsAsync(Card sourceCard, ITargetingEffect targeting, EffectContext ctx)
        {
            if (sourceCard == null || targeting == null || ctx == null)
                return false;

            // Drive the selection through the existing TargetingManager click pipeline.
            // Note: this method awaits until the player picks targets (or cancels).
            var tcs = new TaskCompletionSource<(bool ok, string reason)>();

            var session = new TargetingOnlyEffectSession(
                card: sourceCard,
                targetingRoot: targeting,
                context: ctx,
                onFinished: (ok, reason) => tcs.TrySetResult((ok, reason)));

            if (session.TryAdvance(out var candidates))
            {
                _gameState.Targeting.Begin(
                    session,
                    candidates,
                    onCancelled: () =>
                    {
                        if (!tcs.Task.IsCompleted)
                            tcs.TrySetResult((false, session.CancelReason));
                    });
            }

            var (ok2, reason2) = await tcs.Task;
            if (!ok2 && !string.IsNullOrWhiteSpace(reason2))
                Debug.Log($"[Chain] Rapid targeting cancelled: {reason2}");

            return ok2;
        }

        private async Task<RapidEffectOption?> ChooseValidOptionAsync(Player player, IGameEvent triggeringEvent, HashSet<RapidEffect> activatedThisWindow)
        {
            var options = GetActivatableOptions(player, triggeringEvent, activatedThisWindow);
            if (options.Count == 0)
                return null;

            var chosenMaybe = await PromptChoiceAsync(player, options);
            if (!chosenMaybe.HasValue)
                return null;

            var chosen = chosenMaybe.Value;
            if (chosen.Effect == null || chosen.SourceCard == null)
                return null;

            return chosen;
        }

        /// <summary>
        /// Returns the card that triggered the event, which should be excluded from responding to itself.
        /// </summary>
        private static Card GetCardToExcludeFromResponses(IGameEvent triggeringEvent)
        {
            return GameEventUtils.GetSubjectCard(triggeringEvent);
        }

        private static void ResolveChainLifo(List<ChainLink> chain)
        {
            if (chain == null || chain.Count == 0)
                return;

            Debug.Log($"[Chain] Resolving {chain.Count} link(s)...");

            // Resolve LIFO.
            for (int i = chain.Count - 1; i >= 0; i--)
            {
                var link = chain[i];
                try
                {
                    bool resolvedOk = false;
                    if (link.Effect?.InnerEffect is ITargetingEffect targeting)
                    {
                        targeting.ResolveAfterTargets(link.Context);
                        resolvedOk = true;
                    }
                    else
                    {
                        link.Effect?.Resolve(link.Context);
                        resolvedOk = true;
                    }

                    if (resolvedOk)
                        TryAdvanceRitualStageIfNeeded(link);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[Chain] Exception while resolving link {i + 1}: {ex}");
                }
            }
        }

        private static void TryAdvanceRitualStageIfNeeded(ChainLink link)
        {
            var source = link.Context?.Source;
            if (source?.Behavior is not RitualBehavior ritual)
                return;

            if (!RapidEffectConditionUtils.TryGetRitualStageIndex(link.Effect?.Conditions, out int stageIndex))
                return;

            ritual.TryConsumeStageFromRapid(stageIndex);
        }

        private async Task<RapidEffectOption?> PromptChoiceAsync(Player player, List<RapidEffectOption> options)
        {
            var prompter = Prompter ?? new AutoPassRapidEffectPrompter();

            try
            {
                return await prompter.ChooseActivationAsync(player, options);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Chain] Rapid effect prompt threw: {ex}");
                return null;
            }
        }

        private RapidEffectContext CreateRapidContext(Card source, Player activator, IGameEvent triggeringEvent)
        {
            return new RapidEffectContext
            {
                Source = source,
                GameState = _gameState,
                Targets = new System.Collections.Generic.List<ITargetable>(),
                TriggeringEvent = triggeringEvent,
                Activator = activator,
                ChainWindowSource = triggeringEvent,
            };
        }

        private static void Swap(ref Player a, ref Player b)
        {
            (a, b) = (b, a);
        }

        private List<RapidEffectOption> GetActivatableOptions(Player player, IGameEvent triggeringEvent, HashSet<RapidEffect> activatedThisWindow = null)
        {
            if (player == null)
                return new List<RapidEffectOption>();

            var excluded = GetCardToExcludeFromResponses(triggeringEvent);
            var options = EnumerateActivatableOptions(player, triggeringEvent, activatedThisWindow, excluded).ToList();
            return options;
        }

        private IEnumerable<RapidEffectOption> EnumerateActivatableOptions(
            Player player,
            IGameEvent triggeringEvent,
            HashSet<RapidEffect> activatedThisWindow,
            Card excludedCard)
        {
            foreach (var card in EnumerateCandidateCards(player))
            {
                if (card?.RapidEffects == null || card.RapidEffects.Count == 0)
                    continue;

                for (int i = 0; i < card.RapidEffects.Count; i++)
                {
                    var eff = card.RapidEffects[i];
                    if (TryBuildRapidOption(player, card, eff, triggeringEvent, activatedThisWindow, excludedCard, out var option))
                        yield return option;
                }
            }
        }

        private bool TryBuildRapidOption(
            Player player,
            Card sourceCard,
            RapidEffect effect,
            IGameEvent triggeringEvent,
            HashSet<RapidEffect> activatedThisWindow,
            Card excludedCard,
            out RapidEffectOption option)
        {
            option = default;

            if (player == null || sourceCard == null || effect == null)
                return false;

            if (activatedThisWindow != null && activatedThisWindow.Contains(effect))
                return false;

            // Default rule: the subject card doesn't respond to itself.
            if (excludedCard != null && ReferenceEquals(sourceCard, excludedCard))
                return false;

            // Prevent rapid effects from being activatable while a card is sitting in the cemetery.
            if (player?.Cemetery != null && player.Cemetery.Contains(sourceCard))
                return false;

            var ctx = CreateRapidContext(sourceCard, player, triggeringEvent);

            if (!effect.CanActivate(ctx, out _))
                return false;

            option = new RapidEffectOption(sourceCard, effect);
            return true;
        }

        private static IEnumerable<Card> EnumerateCandidateCards(Player player)
        {
            var yielded = new HashSet<Card>();

            foreach (var card in EnumerateUniqueCards(player?.Hand?.Cards, yielded))
                yield return card;

            foreach (var card in EnumerateUniqueCards(player?.Rituals?.Cards, yielded))
                yield return card;

            foreach (var card in EnumerateUniqueFromPlayZones(player, yielded))
                yield return card;
        }

        private static IEnumerable<Card> EnumerateUniqueCards(IReadOnlyList<Card> cards, HashSet<Card> yielded)
        {
            if (cards == null || yielded == null)
                yield break;

            for (int i = 0; i < cards.Count; i++)
            {
                var c = cards[i];
                if (c != null && yielded.Add(c))
                    yield return c;
            }
        }

        private static IEnumerable<Card> EnumerateUniqueFromPlayZones(Player player, HashSet<Card> yielded)
        {
            if (player?.PlayZones == null || yielded == null)
                yield break;

            var zones = player.PlayZones;
            for (int i = 0; i < zones.Count; i++)
            {
                var c = zones[i]?.OccupyingCard;
                if (c != null && yielded.Add(c))
                    yield return c;
            }
        }

        private Player ResolveFirstPriorityPlayer(IGameEvent triggeringEvent)
        {
            // Default YGO-like priority: opponent of the player who performed the action.
            var actingPlayer = GameEventUtils.GetActingPlayer(triggeringEvent);
            if (actingPlayer != null)
                return _gameState.GetOpponent(actingPlayer);

            return null;
        }
    }
}
