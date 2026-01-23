using System.Collections.Generic;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Events;
using Assets.Scripts.CardEngine.Game;

namespace Assets.Scripts.CardEngine.Keywords
{
    public abstract class KeywordHandlerBase : IKeywordHandler
    {
        public virtual void FilterAttackDefenders(GameState gameState, Card attacker, List<ITargetable> defenders) { }
        public virtual bool CanDirectAttackThroughTroops(GameState gameState, Card attacker) => false;
        public virtual void ModifyPendingAttackDamage(GameState gameState, AttackDeclaration declaration, List<PendingAttackDamage> pending) { }

        public virtual void OnTroopDamaged(GameState gameState, TroopDamagedEvent e) { }
        public virtual void OnPlayerLifeChanged(GameState gameState, PlayerLifeChangedEvent e) { }
    }
}
