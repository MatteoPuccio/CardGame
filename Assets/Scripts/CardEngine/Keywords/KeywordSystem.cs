using System.Collections.Generic;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Events;
using Assets.Scripts.CardEngine.Game;

namespace Assets.Scripts.CardEngine.Keywords
{
    public static class KeywordSystem
    {
        private static readonly HashSet<EventBus> _attachedBuses = new();

        private static readonly List<IKeywordHandler> _handlers = new()
        {
            new TauntKeywordHandler(),
            new FirstStrikeKeywordHandler(),
            new LifestealKeywordHandler(),
            new BypassTroopsKeywordHandler(),
        };

        public static void Attach(GameState gameState)
        {
            var bus = gameState?.EventBus;
            if (bus == null)
                return;

            if (_attachedBuses.Contains(bus))
                return;

            _attachedBuses.Add(bus);

            SubscribeTroopDamaged(bus, gameState);
            SubscribePlayerLifeChanged(bus, gameState);
            SubscribeAttackDefendersQuery(bus, gameState);
            SubscribeAttackDirectAttackQuery(bus, gameState);
            SubscribeAttackModifyPendingDamage(bus, gameState);
        }

        private static void SubscribeTroopDamaged(EventBus bus, GameState gameState)
        {
            bus.Subscribe<TroopDamagedEvent>(e =>
            {
                for (int i = 0; i < _handlers.Count; i++)
                    _handlers[i]?.OnTroopDamaged(gameState, e);
            });
        }

        private static void SubscribePlayerLifeChanged(EventBus bus, GameState gameState)
        {
            bus.Subscribe<PlayerLifeChangedEvent>(e =>
            {
                for (int i = 0; i < _handlers.Count; i++)
                    _handlers[i]?.OnPlayerLifeChanged(gameState, e);
            });
        }

        private static void SubscribeAttackDefendersQuery(EventBus bus, GameState gameState)
        {
            bus.Subscribe<AttackDefendersQueryEvent>(e =>
            {
                if (e?.Defenders == null)
                    return;

                for (int i = 0; i < _handlers.Count; i++)
                    _handlers[i]?.FilterAttackDefenders(gameState, e.Attacker, e.Defenders);
            });
        }

        private static void SubscribeAttackDirectAttackQuery(EventBus bus, GameState gameState)
        {
            bus.Subscribe<AttackDirectAttackQueryEvent>(e =>
            {
                if (e?.Attacker == null)
                    return;

                for (int i = 0; i < _handlers.Count; i++)
                {
                    if (_handlers[i] != null && _handlers[i].CanDirectAttackThroughTroops(gameState, e.Attacker))
                    {
                        e.Allow = true;
                        return;
                    }
                }
            });
        }

        private static void SubscribeAttackModifyPendingDamage(EventBus bus, GameState gameState)
        {
            bus.Subscribe<AttackModifyPendingDamageEvent>(e =>
            {
                if (e?.Pending == null)
                    return;

                for (int i = 0; i < _handlers.Count; i++)
                    _handlers[i]?.ModifyPendingAttackDamage(gameState, e.Declaration, e.Pending);
            });
        }

        public static void HealPlayer(GameState gameState, Player player, int amount, Card source)
        {
            if (player == null || amount <= 0)
                return;

            uint before = player.Life;
            ulong sum = (ulong)before + (ulong)amount;
            player.Life = sum >= uint.MaxValue ? uint.MaxValue : (uint)sum;

            if (player.Life != before)
                gameState?.EventBus?.Publish(new PlayerLifeChangedEvent(player, before, player.Life, source: source));
        }

        public static void FilterAttackDefenders(GameState gameState, Card attacker, List<ITargetable> defenders)
        {
            if (defenders == null || defenders.Count == 0)
                return;

            for (int i = 0; i < _handlers.Count; i++)
                _handlers[i]?.FilterAttackDefenders(gameState, attacker, defenders);
        }

        public static bool CanDirectAttackThroughTroops(GameState gameState, Card attacker)
        {
            for (int i = 0; i < _handlers.Count; i++)
            {
                if (_handlers[i] != null && _handlers[i].CanDirectAttackThroughTroops(gameState, attacker))
                    return true;
            }

            return false;
        }

        public static void ModifyPendingAttackDamage(GameState gameState, AttackDeclaration declaration, List<PendingAttackDamage> pending)
        {
            if (pending == null || pending.Count == 0)
                return;

            for (int i = 0; i < _handlers.Count; i++)
                _handlers[i]?.ModifyPendingAttackDamage(gameState, declaration, pending);
        }
    }
}
