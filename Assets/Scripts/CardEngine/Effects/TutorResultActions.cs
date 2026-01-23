using System;
using Assets.Scripts.CardEngine.Board;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Game;
using UnityEngine;

namespace Assets.Scripts.CardEngine.Effects
{
    public interface ITutorResultAction
    {
        void Execute(EffectContext context, Player player, ICardZone sourceZone, System.Collections.Generic.IReadOnlyList<Card> selectedCards);
    }

    [Serializable]
    public abstract class TutorResultActionDefinition
    {
        public abstract ITutorResultAction CreateRuntimeAction();
    }

    public enum TutorDestinationZone
    {
        Hand,
        Cemetery,
        Deck,
        Rituals,
    }

    [Serializable]
    public sealed class MoveToZoneTutorResultAction : ITutorResultAction
    {
        private readonly TutorDestinationZone _destination;

        public MoveToZoneTutorResultAction(TutorDestinationZone destination)
        {
            _destination = destination;
        }

        public void Execute(EffectContext context, Player player, ICardZone sourceZone, System.Collections.Generic.IReadOnlyList<Card> selectedCards)
        {
            var gs = context?.GameState;
            if (gs == null || player == null || sourceZone == null || selectedCards == null || selectedCards.Count == 0)
                return;

            var toZone = TutorZoneUtils.GetDestinationZone(player, _destination);
            if (toZone == null)
                return;

            for (int i = 0; i < selectedCards.Count; i++)
            {
                var card = selectedCards[i];
                if (card == null)
                    continue;

                gs.MoveToZone(card, sourceZone, toZone);
            }
        }
    }

    [Serializable]
    public sealed class MoveToZoneTutorResultActionDefinition : TutorResultActionDefinition
    {
        [SerializeField] private TutorDestinationZone _destination = TutorDestinationZone.Hand;

        public override ITutorResultAction CreateRuntimeAction() => new MoveToZoneTutorResultAction(_destination);
    }

    [Serializable]
    public sealed class PlayTutorResultAction : ITutorResultAction
    {
        private readonly bool _autoPlaceTroops;
        private readonly bool _requireHandForTroops;

        public PlayTutorResultAction(bool autoPlaceTroops, bool requireHandForTroops)
        {
            _autoPlaceTroops = autoPlaceTroops;
            _requireHandForTroops = requireHandForTroops;
        }

        public void Execute(EffectContext context, Player player, ICardZone sourceZone, System.Collections.Generic.IReadOnlyList<Card> selectedCards)
        {
            var gs = context?.GameState;
            if (gs == null || player == null || sourceZone == null || selectedCards == null || selectedCards.Count == 0)
                return;

            // Playing multiple cards would require sequencing/awaiting targeting sessions.
            // For now, play only the first selection.
            var card = selectedCards[0];
            if (card == null)
                return;

            if (!TryPlaceBoardCardIfNeeded(gs, player, sourceZone, card))
                return;

            TryBeginPlay(gs, sourceZone, card);
        }

        private bool TryPlaceBoardCardIfNeeded(GameState gs, Player player, ICardZone sourceZone, Card card)
        {
            if (gs == null || player == null || sourceZone == null || card == null)
                return false;

            if (card.Behavior == null || !card.Behavior.RequiresPlayZone)
                return true;

            if (!_autoPlaceTroops)
                return false;

            if (_requireHandForTroops && sourceZone is not Hand)
                return false;

            var zone = FindFirstEmptyPlayZone(player);
            if (zone == null)
                return false;

            // Tutor-play should bypass deploy costs (deploy points).
            return gs.MoveToZone(card, sourceZone, zone, interactionState: null, ignoreDeployCost: true);
        }

        private static void TryBeginPlay(GameState gs, ICardZone sourceZone, Card card)
        {
            if (gs == null || sourceZone == null || card == null)
                return;

            if (!card.TryBeginPlay(sourceZone, out var session, out var candidates))
                return;

            if (candidates != null && candidates.Count > 0)
                gs.Targeting.Begin(session, candidates);
        }

        private static PlayAreaZone FindFirstEmptyPlayZone(Player player)
        {
            var zones = player?.PlayZones;
            if (zones == null)
                return null;

            for (int i = 0; i < zones.Count; i++)
            {
                var z = zones[i];
                if (z != null && !z.IsOccupied)
                    return z;
            }

            return null;
        }
    }

    [Serializable]
    public sealed class PlayTutorResultActionDefinition : TutorResultActionDefinition
    {
        [Tooltip("If true, troops are automatically placed into the first available empty play zone.")]
        [SerializeField] private bool _autoPlaceTroops = true;

        [Tooltip("If true, troop plays will only work when tutoring from Hand.")]
        [SerializeField] private bool _requireHandForTroops = true;

        public override ITutorResultAction CreateRuntimeAction() => new PlayTutorResultAction(_autoPlaceTroops, _requireHandForTroops);
    }

    internal static class TutorZoneUtils
    {
        public static ICardZone GetDestinationZone(Player player, TutorDestinationZone destination)
        {
            if (player == null)
                return null;

            return destination switch
            {
                TutorDestinationZone.Hand => player.Hand,
                TutorDestinationZone.Cemetery => player.Cemetery,
                TutorDestinationZone.Deck => player.Deck,
                TutorDestinationZone.Rituals => player.Rituals,
                _ => null,
            };
        }
    }
}
