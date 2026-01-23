using System.Collections.Generic;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Events;
using Assets.Scripts.CardEngine.Game;

namespace Assets.Scripts.CardEngine.Keywords
{
    public sealed class FirstStrikeKeywordHandler : KeywordHandlerBase
    {
        public override void ModifyPendingAttackDamage(GameState gameState, AttackDeclaration declaration, List<PendingAttackDamage> pending)
        {
            if (declaration?.Attacker == null)
                return;

            if (declaration.Defender is not Card defenderCard)
                return;

            if (declaration.Attacker.Behavior is not TroopBehavior attackerTroop)
                return;

            if (defenderCard.Behavior is not TroopBehavior defenderTroop)
                return;

            bool attackerFirst = attackerTroop.HasKeyword(CardKeyword.FirstStrike);
            bool defenderFirst = defenderTroop.HasKeyword(CardKeyword.FirstStrike);

            if (attackerFirst == defenderFirst)
                return;

            int atk = attackerTroop.Power;
            if (atk < 0) atk = 0;

            int defAtk = defenderTroop.Power;
            if (defAtk < 0) defAtk = 0;

            pending.Clear();

            if (attackerFirst)
            {
                pending.Add(new PendingAttackDamage(instigator: declaration.Attacker, target: defenderCard, amount: atk));
                bool defenderSurvives = defenderTroop.Health - atk > 0;
                if (defenderSurvives)
                    pending.Add(new PendingAttackDamage(instigator: defenderCard, target: declaration.Attacker, amount: defAtk));
                return;
            }

            // Defender first strike.
            pending.Add(new PendingAttackDamage(instigator: defenderCard, target: declaration.Attacker, amount: defAtk));
            bool attackerSurvives = attackerTroop.Health - defAtk > 0;
            if (attackerSurvives)
                pending.Add(new PendingAttackDamage(instigator: declaration.Attacker, target: defenderCard, amount: atk));
        }
    }
}
