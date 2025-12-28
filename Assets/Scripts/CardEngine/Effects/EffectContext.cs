using System.Collections.Generic;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Game;

namespace Assets.Scripts.CardEngine.Effects {
    public class EffectContext
    {
        public Card Source { get; set; }
        public List<ITargetable> Targets { get; set; }
        public GameState GameState { get; set; }
        public IGameEvent TriggeringEvent { get; set; }
    }
}