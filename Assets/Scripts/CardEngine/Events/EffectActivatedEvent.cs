using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Effects;
using Assets.Scripts.CardEngine.Game;

namespace Assets.Scripts.CardEngine.Events
{
    public sealed class EffectActivatedEvent : IGameEvent
    {
        public string EventType => "EffectActivated";
        public Card Source { get; }
        public Effect Effect { get; }
        public Player Activator { get; }

        public EffectActivatedEvent(Card source, Effect effect, Player activator)
        {
            Source = source;
            Effect = effect;
            Activator = activator;
        }
    }
}
