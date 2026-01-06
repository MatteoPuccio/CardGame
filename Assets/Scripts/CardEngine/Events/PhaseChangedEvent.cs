using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Game;

namespace Assets.Scripts.CardEngine.Events
{
    public class PhaseChangedEvent : IGameEvent
    {
        public string EventType { get; } = "PhaseChanged";
        public Card Source { get; } = null;

        public TurnPhase FromPhase { get; }
        public TurnPhase ToPhase { get; }
        public Player ActivePlayer { get; }
        public int TurnNumber { get; }
        public bool StartedNewTurn { get; }

        public PhaseChangedEvent(
            TurnPhase fromPhase,
            TurnPhase toPhase,
            Player activePlayer,
            int turnNumber,
            bool startedNewTurn)
        {
            FromPhase = fromPhase;
            ToPhase = toPhase;
            ActivePlayer = activePlayer;
            TurnNumber = turnNumber;
            StartedNewTurn = startedNewTurn;
        }
    }
}
