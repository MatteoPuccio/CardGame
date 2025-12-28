using System.Collections.Generic;

namespace Assets.Scripts.CardEngine.Effects
{
    public class EffectStack
    {
        private readonly Queue<(IEffect, EffectContext)> Stack = new();

        public void Push(IEffect effect, EffectContext context)
        {
            Stack.Enqueue((effect, context));
        }

        public void Resolve()
        {
            while (Stack.Count > 0)
            {
                var (effect, context) = Stack.Dequeue();
                effect.Apply(context);
            }
        }
    }
}
