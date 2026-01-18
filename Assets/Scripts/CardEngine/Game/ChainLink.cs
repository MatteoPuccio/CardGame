using Assets.Scripts.CardEngine.Effects;

namespace Assets.Scripts.CardEngine.Game
{
    public readonly struct ChainLink
    {
        public readonly RapidEffect Effect;
        public readonly RapidEffectContext Context;

        public ChainLink(RapidEffect effect, RapidEffectContext context)
        {
            Effect = effect;
            Context = context;
        }
    }
}
