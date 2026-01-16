using System;
using Assets.Scripts.CardEngine.Events;
using UnityEngine;

namespace Assets.Scripts.CardEngine.Game
{
    public class TurnFlow
    {
        private static readonly TurnPhase[] PhaseOrder =
        {
            TurnPhase.Draw,
            TurnPhase.Ritual,
            TurnPhase.Play,
            TurnPhase.End
        };

        private readonly GameState _state;

        public TurnFlow(GameState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        public void BeginGame()
        {
            EnterPhase(_state.Phase);
        }

        public void AdvancePhase()
        {
            if (_state.ActivePlayer == null)
                return;

            TurnPhase fromPhase = _state.Phase;

            if (fromPhase == TurnPhase.End)
            {
                AdvanceTurn();
                return;
            }

            _state.SetPhase(GetNextPhase(fromPhase));
            PublishPhaseChanged(fromPhase, _state.Phase, startedNewTurn: false);
            EnterPhase(_state.Phase);
        }

        public void AdvanceToPhase(TurnPhase targetPhase)
        {
            if (_state.ActivePlayer == null)
                return;

            const int maxLoopSafetyBuffer = 2;
            int maxSteps = PhaseOrder.Length + maxLoopSafetyBuffer;
            int steps = 0;

            while (_state.Phase != targetPhase && steps++ < maxSteps)
                AdvancePhase();
        }

        public void AdvanceTurn()
        {
            if (_state.ActivePlayer == null)
                return;

            Player nextPlayer = _state.GetOpponent(_state.ActivePlayer);
            if (nextPlayer == null)
            {
                Debug.LogError("TurnFlow: Could not resolve next player.");
                return;
            }

            TurnPhase from = TurnPhase.End;
            _state.SetActivePlayer(nextPlayer);
            _state.IncrementTurnNumber();
            _state.SetPhase(TurnPhase.Draw);
            PublishPhaseChanged(from, TurnPhase.Draw, startedNewTurn: true);
            EnterPhase(TurnPhase.Draw);
        }

        private void PublishPhaseChanged(TurnPhase fromPhase, TurnPhase toPhase, bool startedNewTurn)
        {
            _state.EventBus?.Publish(new PhaseChangedEvent(
                fromPhase: fromPhase,
                toPhase: toPhase,
                activePlayer: _state.ActivePlayer,
                turnNumber: _state.TurnNumber,
                startedNewTurn: startedNewTurn
            ));
        }

        private static TurnPhase GetNextPhase(TurnPhase phase)
        {
            int index = Array.IndexOf(PhaseOrder, phase);
            if (index < 0)
                return TurnPhase.Draw;

            int nextIndex = index + 1;
            if (nextIndex >= PhaseOrder.Length)
                return TurnPhase.Draw;

            return PhaseOrder[nextIndex];
        }

        private void EnterPhase(TurnPhase phase)
        {
            switch (phase)
            {
                case TurnPhase.Draw:
                    ExecuteDrawPhase();
                    break;

                case TurnPhase.Ritual:
                    ExecuteRitualPhase();
                    break;

                case TurnPhase.Play:
                case TurnPhase.End:
                default:
                    break;
            }
        }

        private void ExecuteDrawPhase()
        {
            Player player = _state.ActivePlayer;

            player.DeployPoints = player.DeployPointsPerTurn;

            Deck deck = player.Deck;

            deck.DrawTop();
        }

        private void ExecuteRitualPhase()
        {
            // Rituals no longer auto-advance on Ritual phase entry.
            // Advancement is player-triggered and limited to once per turn per ritual.
        }
    }
}
