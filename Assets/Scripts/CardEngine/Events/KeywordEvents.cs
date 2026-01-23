using System.Collections.Generic;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Game;

namespace Assets.Scripts.CardEngine.Events
{
    public sealed class AttackDefendersQueryEvent : IGameEvent
    {
        public string EventType => "AttackDefendersQuery";
        public Card Source { get; }

        public Card Attacker { get; }
        public List<ITargetable> Defenders { get; }

        public AttackDefendersQueryEvent(Card attacker, List<ITargetable> defenders)
        {
            Attacker = attacker;
            Defenders = defenders;
            Source = attacker;
        }
    }

    public sealed class AttackDirectAttackQueryEvent : IGameEvent
    {
        public string EventType => "AttackDirectAttackQuery";
        public Card Source { get; }

        public Card Attacker { get; }
        public bool Allow { get; set; }

        public AttackDirectAttackQueryEvent(Card attacker)
        {
            Attacker = attacker;
            Source = attacker;
            Allow = false;
        }
    }

    public sealed class AttackModifyPendingDamageEvent : IGameEvent
    {
        public string EventType => "AttackModifyPendingDamage";
        public Card Source { get; }

        public AttackDeclaration Declaration { get; }
        public List<PendingAttackDamage> Pending { get; }

        public AttackModifyPendingDamageEvent(AttackDeclaration declaration, List<PendingAttackDamage> pending)
        {
            Declaration = declaration;
            Pending = pending;
            Source = declaration?.Attacker;
        }
    }
}
