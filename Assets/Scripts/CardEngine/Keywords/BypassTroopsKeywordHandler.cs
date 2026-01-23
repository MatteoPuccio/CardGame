using System.Collections.Generic;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Events;
using Assets.Scripts.CardEngine.Game;

namespace Assets.Scripts.CardEngine.Keywords
{
    public sealed class BypassTroopsKeywordHandler : KeywordHandlerBase
    {
        public override bool CanDirectAttackThroughTroops(GameState gameState, Card attacker)
        {
            return CardKeywordUtils.HasKeyword(attacker, CardKeyword.BypassTroops);
        }

        public override void FilterAttackDefenders(GameState gameState, Card attacker, List<ITargetable> defenders) { }

        public override void ModifyPendingAttackDamage(GameState gameState, AttackDeclaration declaration, List<PendingAttackDamage> pending) { }
    }
}
