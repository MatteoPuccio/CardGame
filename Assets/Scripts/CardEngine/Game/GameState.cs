using System.Collections.Generic;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Events;
using UnityEngine;

namespace Assets.Scripts.CardEngine.Game
{

    public class GameState
    {
        public Player Player1 { get; private set; }
        public Player Player2 { get; private set; }

        public TurnPhase Phase { get; private set; }
        public Player ActivePlayer { get; private set; }

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
        }

        public Player GetOpponent(Player player) =>
            player == Player1 ? Player2 : Player1;


        public void PlayCard(Card card, Player player)
        {
            _eventBus.Publish(new CardPlayedEvent(card, player));
        }

        public bool TryMoveToZone(Card card, ICardZone fromZone, ICardZone toZone, ICardInteractionState interactionState = null)
        {
            Debug.Log("InteractionState: " + (interactionState == null ? "null" : interactionState.GetName));
            if (card == null || toZone == null || fromZone == null) {
                Debug.LogError($"Card: {(card == null ? "null" : card.Name)}, FromZone: {(fromZone == null ? "null" : fromZone.ZoneName)}, ToZone: {(toZone == null ? "null" : toZone.ZoneName)}");
                return false;
            }
            Player owner = card.Owner;
            if (owner == null) // || owner != ActivePlayer
            {
                return false;
            }
            bool canMove = toZone.CanEnter(card);
            if (!canMove) {
                _eventBus.Publish(new CardPlayedEvent("CardMoveFailed", card, owner, from: fromZone.ZoneName, to: toZone.ZoneName));
                return false;
            }
            fromZone.ExitCard(card);
            toZone.EnterCard(card);
            _eventBus.Publish(new CardPlayedEvent("CardMoved", card, owner, from: fromZone.ZoneName, to: toZone.ZoneName));
            return canMove;
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
    }
}