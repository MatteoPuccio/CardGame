using System.Collections.Generic;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Events;
using Assets.Scripts.CardEngine.Game;

namespace Assets.Scripts.CardEngine.Keywords
{
    public sealed class TauntKeywordHandler : KeywordHandlerBase
    {
        public override void FilterAttackDefenders(GameState gameState, Card attacker, List<ITargetable> defenders)
        {
            if (defenders == null || defenders.Count == 0)
                return;

            bool hasTauntTroop = false;
            for (int i = 0; i < defenders.Count; i++)
            {
                if (defenders[i] is Card c && HasTaunt(c))
                {
                    hasTauntTroop = true;
                    break;
                }
            }

            if (!hasTauntTroop)
                return;

            for (int i = defenders.Count - 1; i >= 0; i--)
            {
                if (defenders[i] is not Card c || !HasTaunt(c))
                    defenders.RemoveAt(i);
            }
        }

        private static bool HasTaunt(Card card) => CardKeywordUtils.HasKeyword(card, CardKeyword.Taunt);

        public override bool CanDirectAttackThroughTroops(GameState gameState, Card attacker) => false;

        public override void ModifyPendingAttackDamage(GameState gameState, AttackDeclaration declaration, List<PendingAttackDamage> pending) { }
    }
}
