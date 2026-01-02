using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Game;

namespace Assets.Scripts.CardEngine.Effects
{
    public class DestroyEffect : IEffect
    {
        public void Resolve(EffectContext context)
        {
            foreach (var target in context.Targets)
            {
                if (target is Card card)
                {
                    ICardZone playAreaZone = card.Owner.PlayZones.Find(zone => zone.OccupyingCard == card);
                    card.GameState.TryMoveToZone(
                        card,
                        playAreaZone,
                        card.Owner.Deck
                    );
                }
            }
        }
    }
}
