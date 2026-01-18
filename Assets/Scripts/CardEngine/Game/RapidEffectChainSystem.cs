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
                if (!chosenMaybe.HasValue || !TryCreateChainLink(chosenMaybe.Value, current, triggeringEvent, out var link))
                {
                    consecutivePasses++;
                    Swap(ref current, ref other);
                    continue;
                }

                Debug.Log($"[Chain] Link {chain.Count + 1}: {current?.Name ?? "<null>"} activates {chosenMaybe.Value}");
                chain.Add(link);
                if (chosenMaybe.Value.Effect != null)
                    activatedThisWindow.Add(chosenMaybe.Value.Effect);
                consecutivePasses = 0;
                Swap(ref current, ref other);
            }

            ResolveChainLifo(chain);
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

            if (!RapidEffectConditionUtils.TryGetRitualStageIndex(link.Effect?.Condition, out int stageIndex))
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
                Targets = null,
                TriggeringEvent = triggeringEvent,
                Activator = activator,
                ChainWindowSource = triggeringEvent,
            };
        }

        private bool TryCreateChainLink(RapidEffectOption chosen, Player activator, IGameEvent triggeringEvent, out ChainLink link)
        {
            link = default;

            if (chosen.Effect == null || chosen.SourceCard == null)
                return false;

            var ctx = CreateRapidContext(chosen.SourceCard, activator, triggeringEvent);

            if (!chosen.Effect.CanActivate(ctx, out var cantReason))
            {
                Debug.Log($"RapidEffect: Cannot activate {chosen}: {cantReason}");
                return false;
            }

            if (!chosen.Effect.TryPayCost(ctx, out var costReason))
            {
                Debug.Log($"RapidEffect: Cost not paid for {chosen}: {costReason}");
                return false;
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

            link = new ChainLink(chosen.Effect, ctx);
            return true;
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
            return EnumerateActivatableOptions(player, triggeringEvent, activatedThisWindow, excluded).ToList();
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

                if (excludedCard != null && ReferenceEquals(card, excludedCard))
                    continue;

                for (int i = 0; i < card.RapidEffects.Count; i++)
                {
                    var eff = card.RapidEffects[i];
                    if (TryBuildRapidOption(player, card, eff, triggeringEvent, activatedThisWindow, out var option))
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
            out RapidEffectOption option)
        {
            option = default;

            if (player == null || sourceCard == null || effect == null)
                return false;

            if (activatedThisWindow != null && activatedThisWindow.Contains(effect))
                return false;

            var ctx = CreateRapidContext(sourceCard, player, triggeringEvent);

            if (!effect.CanActivate(ctx, out _))
                return false;

            option = new RapidEffectOption(sourceCard, effect);
            return true;
        }

        private static IEnumerable<Card> EnumerateCandidateCards(Player player)
        {
            if (player?.Hand?.Cards != null)
            {
                var handCards = player.Hand.Cards;
                for (int i = 0; i < handCards.Count; i++)
                    yield return handCards[i];
            }

            if (player?.Rituals?.Cards != null)
            {
                var ritualCards = player.Rituals.Cards;
                for (int i = 0; i < ritualCards.Count; i++)
                    yield return ritualCards[i];
            }

            if (player?.PlayZones != null)
            {
                var zones = player.PlayZones;
                for (int i = 0; i < zones.Count; i++)
                {
                    var zone = zones[i];
                    if (zone?.OccupyingCard != null)
                        yield return zone.OccupyingCard;
                }
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
