using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Events;
using Assets.Scripts.CardEngine.Game;

namespace Assets.Scripts.CardEngine.Utils
{
    public static class GameEventUtils
    {
        public static Player GetActingPlayer(IGameEvent gameEvent)
        {
            return gameEvent switch
            {
                CardPlayedEvent cpe => cpe.Player,
                EffectActivatedEvent eae => eae.Activator,
                TroopDamagedEvent tde => tde.Instigator?.Owner,
                TroopDiedEvent tdie => tdie.Instigator?.Owner,
                CardDestroyedEvent cde => cde.Instigator?.Owner,
				AttackStartedEvent bse => bse.AttackingPlayer,
				AttackDeclaredEvent ade => ade.AttackingPlayer,
				AttackDamageAppliedEvent bdae => bdae.AttackingPlayer,
				AttackEndedEvent bee => bee.AttackingPlayer,
                _ => null,
            };
        }

        public static Card GetSubjectCard(IGameEvent gameEvent)
        {
            return gameEvent switch
            {
                CardPlayedEvent cpe => cpe.Source,
                EffectActivatedEvent eae => eae.Source,
                TroopDamagedEvent tde => tde.Source,
                TroopDiedEvent tdie => tdie.Source,
                CardDestroyedEvent cde => cde.Target,
                _ => null,
            };
        }
    }
}
