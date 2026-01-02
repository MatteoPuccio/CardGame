using Assets.Scripts.CardEngine.Effects;

public class TargetedEffect : IEffect
{
    private readonly IEffectSelector _selector;
    private readonly IEffect _effect;

    public TargetedEffect(IEffectSelector selector, IEffect effect)
    {
        _selector = selector;
        _effect = effect;
    }

    public void Resolve(EffectContext context)
    {
        var targets = _selector.Select(context);

        context.Targets = targets;

        _effect.Resolve(context);
    }
}
