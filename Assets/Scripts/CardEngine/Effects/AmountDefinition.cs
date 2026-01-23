using System;
using System.Collections.Generic;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Game;

namespace Assets.Scripts.CardEngine.Effects
{
    [Serializable]
    public abstract class AmountDefinition
    {
        public abstract int Evaluate(EffectContext context);
    }

    [Serializable]
    public sealed class ConstantAmountDefinition : AmountDefinition
    {
        public int Value;

        public override int Evaluate(EffectContext context) => Value;
    }

    [Serializable]
    public sealed class TroopsWithRaceOnFieldAmountDefinition : AmountDefinition
    {
        public enum OwnerScope
        {
            Any,
            SourcePlayer,
            Opponent,
        }

        public TroopRaces Race = TroopRaces.None;
        public OwnerScope Scope = OwnerScope.Any;

        public override int Evaluate(EffectContext context)
        {
            if (context?.GameState == null)
                return 0;

            if (Race == TroopRaces.None)
                return 0;

            var sourceOwner = context.Source?.Owner;
            var gs = context.GameState;

            IEnumerable<Player> players = ResolvePlayers(gs, sourceOwner, Scope);

            int count = 0;
            foreach (var p in players)
            {
                if (p?.PlayZones == null)
                    continue;

                for (int i = 0; i < p.PlayZones.Count; i++)
                {
                    var card = p.PlayZones[i]?.OccupyingCard;
                    if (card?.Behavior is not TroopBehavior troop)
                        continue;

                    if (troop.IsRace(Race))
                        count++;
                }
            }

            return count;
        }

        private static IEnumerable<Player> ResolvePlayers(GameState gameState, Player sourceOwner, OwnerScope scope)
        {
            if (gameState == null)
                yield break;

            if (scope == OwnerScope.SourcePlayer)
            {
                if (sourceOwner != null)
                    yield return sourceOwner;
                yield break;
            }

            if (scope == OwnerScope.Opponent)
            {
                foreach (var p in ResolveOpponent(gameState, sourceOwner))
                    yield return p;
                yield break;
            }

            foreach (var p in ResolveAny(gameState))
                yield return p;
        }

        private static IEnumerable<Player> ResolveOpponent(GameState gameState, Player sourceOwner)
        {
            if (gameState == null || sourceOwner == null)
                yield break;

            var opp = gameState.GetOpponent(sourceOwner);
            if (opp != null)
                yield return opp;
        }

        private static IEnumerable<Player> ResolveAny(GameState gameState)
        {
            if (gameState?.Player1 != null)
                yield return gameState.Player1;
            if (gameState?.Player2 != null)
                yield return gameState.Player2;
        }
    }
}
