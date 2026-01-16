using System.Collections.Generic;
using Assets.Scripts.CardEngine.Board;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Events;
using UnityEngine;

namespace Assets.Scripts.CardEngine.Game
{

    public class GameState
    {
        public TargetingManager Targeting { get; } = new TargetingManager();

        public RapidEffectChainSystem RapidEffectChain { get; set; }

        public Player Player1 { get; private set; }
        public Player Player2 { get; private set; }

        public TurnPhase Phase { get; private set; }
        public Player ActivePlayer { get; private set; }
        public int TurnNumber { get; private set; }

        private readonly EventBus _eventBus;

        public EventBus EventBus => _eventBus;

        public GameState(EventBus bus)
        {
            _eventBus = bus;
        }

        public void AddPlayers(Player p1, Player p2)
        {
            Player1 = p1;
            Player2 = p2;
            ActivePlayer = Player1;
            Phase = TurnPhase.Draw;
            TurnNumber = 1;
        }

        internal void SetPhase(TurnPhase phase)
        {
            Phase = phase;
        }

        internal void SetActivePlayer(Player player)
        {
            ActivePlayer = player;
        }

        internal void IncrementTurnNumber()
        {
            TurnNumber++;
        }

        public Player GetOpponent(Player player) =>
            player == Player1 ? Player2 : Player1;


        private static PlayAreaZone FindPlayZoneContaining(Card card)
        {
            var owner = card?.Owner;
            var zones = owner?.PlayZones;
            if (zones == null)
                return null;

            for (int i = 0; i < zones.Count; i++)
            {
                var zone = zones[i];
                if (zone != null && zone.OccupyingCard == card)
                    return zone;
            }

            return null;
        }

        public bool ApplyDamage(Card target, int amount, Card instigator = null)
        {
            if (target == null)
                return false;
            if (amount <= 0)
                return false;

            if (target.Behavior is not TroopBehavior troop)
                return false;

            troop.ApplyDamage(amount);
            _eventBus?.Publish(new TroopDamagedEvent(target, amount, instigator));

            if (!troop.IsDead)
                return true;

            // Death handling only applies to troops currently on the board.
            var owner = target.Owner;
            var fromZone = FindPlayZoneContaining(target);
            if (owner == null || owner.Cemetery == null || fromZone == null)
            {
                _eventBus?.Publish(new TroopDiedEvent(target, instigator, movedToCemetery: false));
                return true;
            }

            bool moved = MoveToZone(target, fromZone, owner.Cemetery);
            _eventBus?.Publish(new TroopDiedEvent(target, instigator, movedToCemetery: moved));
            return true;
        }

        public bool DestroyCard(Card target, Card instigator = null)
        {
            if (target == null)
                return false;

            // Prefer the damage/death pipeline for troops.
            if (target.Behavior is TroopBehavior troop)
            {
                int lethal = troop.Health <= 0 ? troop.MaxHealth : troop.Health;
                lethal = lethal <= 0 ? 999 : lethal;
                return ApplyDamage(target, lethal, instigator);
            }

            var owner = target.Owner;
            var fromZone = FindPlayZoneContaining(target);
            if (owner == null || owner.Cemetery == null || fromZone == null)
                return false;

            bool moved = MoveToZone(target, fromZone, owner.Cemetery);
            _eventBus?.Publish(new CardDestroyedEvent(target, instigator, movedToCemetery: moved));
            return moved;
        }

        private static int GetTroopDeployCost(Card card, ICardZone fromZone, ICardZone toZone)
        {
            if (card == null || fromZone == null || toZone == null)
                return 0;

            bool isTroopDeploy =
                card.Category == CardType.Troop &&
                fromZone is Hand &&
                toZone is PlayAreaZone;

            if (!isTroopDeploy)
                return 0;

            if (card.Behavior is not TroopBehavior troop)
                return 0;

            return troop.DeployCost < 0 ? 0 : troop.DeployCost;
        }

        private void PublishMoveFailed(Card card, Player owner, ICardZone fromZone, ICardZone toZone)
        {
            _eventBus.Publish(new CardMoveFailedEvent(card, owner, from: fromZone.ZoneName, to: toZone.ZoneName));
        }

        private bool TryValidateMove(
            Card card,
            Player owner,
            ICardZone fromZone,
            ICardZone toZone,
            ICardInteractionState interactionState,
            out int deployCost)
        {
            deployCost = GetTroopDeployCost(card, fromZone, toZone);

            if (deployCost > 0 && owner.DeployPoints < deployCost)
            {
                PublishMoveFailed(card, owner, fromZone, toZone);
                return false;
            }

            if (interactionState != null && ActivePlayer != null && owner != ActivePlayer)
            {
                PublishMoveFailed(card, owner, fromZone, toZone);
                return false;
            }

            if (!toZone.CanEnter(card))
            {
                PublishMoveFailed(card, owner, fromZone, toZone);
                return false;
            }

            return true;
        }

        private bool TryTransferCard(Card card, Player owner, ICardZone fromZone, ICardZone toZone)
        {
            bool exited = fromZone.ExitCard(card);
            if (!exited)
            {
                PublishMoveFailed(card, owner, fromZone, toZone);
                return false;
            }

            bool entered = toZone.EnterCard(card);
            if (!entered)
            {
                fromZone.EnterCard(card);
                PublishMoveFailed(card, owner, fromZone, toZone);
                return false;
            }

            return true;
        }

        public bool MoveToZone(Card card, ICardZone fromZone, ICardZone toZone, ICardInteractionState interactionState = null)
        {
            Debug.Log("InteractionState: " + (interactionState == null ? "null" : interactionState.Name));
            if (card == null || toZone == null || fromZone == null) {
                Debug.LogError($"Card: {(card == null ? "null" : card.Name)}, FromZone: {(fromZone == null ? "null" : fromZone.ZoneName)}, ToZone: {(toZone == null ? "null" : toZone.ZoneName)}");
                return false;
            }
            Player owner = card.Owner;
            if (owner == null)
            {
                return false;
            }

            if (!TryValidateMove(card, owner, fromZone, toZone, interactionState, out int deployCost))
                return false;

            if (!TryTransferCard(card, owner, fromZone, toZone))
                return false;

            if (deployCost > 0)
                owner.DeployPoints -= deployCost;

            _eventBus.Publish(new CardMovedEvent(card, owner, from: fromZone.ZoneName, to: toZone.ZoneName));

            return true;
        }

        // Back-compat alias (older call sites). Prefer MoveToZone.
        public bool TryMoveToZone(Card card, ICardZone fromZone, ICardZone toZone, ICardInteractionState interactionState = null)
        {
            return MoveToZone(card, fromZone, toZone, interactionState);
        }

        public List<ITargetable> GetEnemyCharacters(Player player)
        {
            var opponent = GetOpponent(player);
            var targets = new List<ITargetable>();

            if (opponent?.PlayZones != null)
            {
                foreach (var zone in opponent.PlayZones)
                {
                    if (zone.OccupyingCard != null)
                    {
                        targets.Add(zone.OccupyingCard);
                    }
                }
            }

            return targets;
        }

        public static List<ITargetable> GetFriendlyCharacters(Player player)
        {
            var targets = new List<ITargetable>();

            if (player?.PlayZones != null)
            {
                foreach (var zone in player.PlayZones)
                {
                    if (zone.OccupyingCard != null)
                    {
                        targets.Add(zone.OccupyingCard);
                    }
                }
            }

            return targets;
        }
    }
}