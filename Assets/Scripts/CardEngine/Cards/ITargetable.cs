namespace Assets.Scripts.CardEngine.Cards
{
    public enum TargetableKind
    {
        Card,
        Player,
    }

    public interface ITargetable
    {
        TargetableKind Kind { get; }
    }
}