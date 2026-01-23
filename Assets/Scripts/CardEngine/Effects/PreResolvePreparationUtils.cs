using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Assets.Scripts.CardEngine.Effects
{
    public static class PreResolvePreparationUtils
    {
        public static async Task<PreResolveResult> PrepareAsync(Effect root, EffectContext context)
        {
            if (root == null)
                return PreResolveResult.Success();

            if (context == null)
                return PreResolveResult.Cancel("Missing effect context.");

            // Depth-first, preserve effect order where possible.
            var stack = new Stack<Effect>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (current == null)
                    continue;

                if (current is IPreResolveEffect prep)
                {
                    var result = await prep.PrepareAsync(context);
                    if (!result.Ok)
                        return result;
                }

                if (current is ICompositeEffect composite)
                {
                    // Reverse-push so the first child is processed first.
                    var children = composite.GetChildEffects();
                    if (children == null)
                        continue;

                    var buffer = new List<Effect>();
                    foreach (var child in children)
                        buffer.Add(child);

                    for (int i = buffer.Count - 1; i >= 0; i--)
                        stack.Push(buffer[i]);
                }
            }

            return PreResolveResult.Success();
        }
    }
}
