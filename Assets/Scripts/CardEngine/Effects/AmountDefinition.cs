using System;
using System.Collections.Generic;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Game;
using Assets.Scripts.CardEngine.Utils;

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

    /// <summary>
    /// Counts cards with a specific tag in various zones.
    /// </summary>
    [Serializable]
    public sealed class CardsWithTagAmountDefinition : AmountDefinition
    {
        public enum OwnerScope
        {
            Any,
            SourcePlayer,
            Opponent,
        }

        [UnityEngine.Tooltip("The tag to search for (case-insensitive).")]
        public string Tag;

        public OwnerScope Owner = OwnerScope.SourcePlayer;

        [UnityEngine.Tooltip("Which zones to search. Use flags to combine multiple zones.")]
        public CardZones Zones = CardZones.InPlay;

        public override int Evaluate(EffectContext context)
        {
            if (context?.GameState == null || string.IsNullOrWhiteSpace(Tag))
                return 0;

            var sourceOwner = context.Source?.Owner;
            var gs = context.GameState;

            int count = 0;
            foreach (var player in ResolvePlayers(gs, sourceOwner, Owner))
            {
                if (player == null)
                    continue;

                count += CountCardsWithTag(player, Tag, Zones);
            }

            return count;
        }

        private static int CountCardsWithTag(Player player, string tag, CardZones zones)
        {
            int count = 0;

            if ((zones & CardZones.InPlay) != 0)
                count += CountInPlayZones(player, tag);

            if ((zones & CardZones.Hand) != 0)
                count += CountInCardList(player.Hand?.Cards, tag);

            if ((zones & CardZones.Cemetery) != 0)
                count += CountInCardList(player.Cemetery?.Cards, tag);

            if ((zones & CardZones.Deck) != 0)
                count += CountInCardList(player.Deck?.Cards, tag);

            return count;
        }

        private static int CountInPlayZones(Player player, string tag)
        {
            if (player.PlayZones == null)
                return 0;

            int count = 0;
            for (int i = 0; i < player.PlayZones.Count; i++)
            {
                var card = player.PlayZones[i]?.OccupyingCard;
                if (card != null && card.HasTag(tag))
                    count++;
            }
            return count;
        }

        private static int CountInCardList(IEnumerable<Card> cards, string tag)
        {
            if (cards == null)
                return 0;

            int count = 0;
            foreach (var card in cards)
            {
                if (card != null && card.HasTag(tag))
                    count++;
            }
            return count;
        }

        private static IEnumerable<Player> ResolvePlayers(GameState gameState, Player sourceOwner, OwnerScope scope)
        {
            if (gameState == null)
                yield break;

            switch (scope)
            {
                case OwnerScope.SourcePlayer:
                    if (sourceOwner != null)
                        yield return sourceOwner;
                    break;

                case OwnerScope.Opponent:
                    if (sourceOwner != null)
                    {
                        var opp = gameState.GetOpponent(sourceOwner);
                        if (opp != null)
                            yield return opp;
                    }
                    break;

                default:
                    if (gameState.Player1 != null)
                        yield return gameState.Player1;
                    if (gameState.Player2 != null)
                        yield return gameState.Player2;
                    break;
            }
        }
    }
}
