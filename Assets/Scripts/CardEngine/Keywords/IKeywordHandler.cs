using System.Collections.Generic;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Events;
using Assets.Scripts.CardEngine.Game;

namespace Assets.Scripts.CardEngine.Keywords
{
    public interface IKeywordHandler
    {
        void FilterAttackDefenders(GameState gameState, Card attacker, List<ITargetable> defenders);
        bool CanDirectAttackThroughTroops(GameState gameState, Card attacker);
        void ModifyPendingAttackDamage(GameState gameState, AttackDeclaration declaration, List<PendingAttackDamage> pending);

        void OnTroopDamaged(GameState gameState, TroopDamagedEvent e);
        void OnPlayerLifeChanged(GameState gameState, PlayerLifeChangedEvent e);
    }
}
