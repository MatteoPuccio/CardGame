using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Effects;

namespace Assets.Scripts.CardEngine.Game
{
    public readonly struct RapidEffectOption
    {
        public readonly Card SourceCard;
        public readonly RapidEffect Effect;

        public RapidEffectOption(Card sourceCard, RapidEffect effect)
        {
            SourceCard = sourceCard;
            Effect = effect;
        }

        public override string ToString()
        {
            string cardName = SourceCard != null ? SourceCard.Name : "<null card>";
            string effectName = Effect != null ? Effect.GetType().Name : "<null effect>";
            return $"{cardName}.{effectName}";
        }
    }
}
