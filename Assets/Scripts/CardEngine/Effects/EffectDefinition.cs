using System;

namespace Assets.Scripts.CardEngine.Effects
{
    [Serializable]
    public abstract class EffectDefinition
    {
        public abstract Effect CreateRuntimeEffect();
    }
}
