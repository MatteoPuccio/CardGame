using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Events;
using Assets.Scripts.CardEngine.Game;

namespace Assets.Scripts.CardEngine.Keywords
{
    public sealed class LifestealKeywordHandler : KeywordHandlerBase
    {
        public override void OnTroopDamaged(GameState gameState, TroopDamagedEvent e)
        {
            if (gameState == null || e == null)
                return;

            if (e.Amount <= 0)
                return;

            var instigator = e.Instigator;
            if (!CardKeywordUtils.HasKeyword(instigator, CardKeyword.Lifesteal))
                return;

            if (instigator?.Owner == null)
                return;

            KeywordSystem.HealPlayer(gameState, instigator.Owner, e.Amount, source: instigator);
        }

        public override void OnPlayerLifeChanged(GameState gameState, PlayerLifeChangedEvent e)
        {
            if (gameState == null || e == null)
                return;

            // Only respond to damage (After < Before). Ignore heals to prevent loops.
            if (e.After >= e.Before)
                return;

            int dealt = (int)(e.Before - e.After);
            if (dealt <= 0)
                return;

            var instigator = e.Source;
            if (!CardKeywordUtils.HasKeyword(instigator, CardKeyword.Lifesteal))
                return;

            if (instigator?.Owner == null)
                return;

            KeywordSystem.HealPlayer(gameState, instigator.Owner, dealt, source: instigator);
        }
    }
}
