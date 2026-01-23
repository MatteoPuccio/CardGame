using System;

namespace Assets.Scripts.CardEngine.Effects
{
    [Serializable]
    public abstract class EffectDefinition
    {
        public Effect CreateRuntimeEffect()
        {
            var effect = CreateRuntimeEffectCore();
            return effect;
        }

        protected abstract Effect CreateRuntimeEffectCore();
    }
}
