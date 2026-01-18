using Assets.Scripts.CardEngine.Cards;
using System.Collections.Generic;
using System;
using UnityEngine;
using Assets.Scripts.CardEngine.Game;

namespace Assets.Scripts.CardEngine.Effects
{
    public interface IEffectSelector
    {
        List<ITargetable> Select(EffectContext effectContext);
    }

    /// <summary>
    /// Optional hint for interactive play: some selectors are deterministic and don't require
    /// the player to manually pick targets (e.g., "all enemies").
    /// </summary>
    public interface IPlayerChoiceSelector
    {
        bool RequiresPlayerChoice { get; }
    }

    [Serializable]
    public abstract class EffectSelectorDefinition
    {
        public abstract IEffectSelector CreateRuntimeSelector();
    }

    public class AllEnemyCharactersSelector : IEffectSelector, IPlayerChoiceSelector
    {
        public bool RequiresPlayerChoice => false;

        public List<ITargetable> Select(EffectContext effectContext)
        {
            return effectContext.GameState.GetEnemyCharacters(effectContext.Source.Owner);
        }
    }

    [Serializable]
    public sealed class AllEnemyCharactersSelectorDefinition : EffectSelectorDefinition
    {
        public override IEffectSelector CreateRuntimeSelector() => new AllEnemyCharactersSelector();
    }

    public class AllFriendlyCharactersSelector : IEffectSelector, IPlayerChoiceSelector
    {
        public bool RequiresPlayerChoice => false;

        public List<ITargetable> Select(EffectContext effectContext)
        {
            return GameState.GetFriendlyCharacters(effectContext.Source.Owner);
        }
    }

    [Serializable]
    public sealed class AllFriendlyCharactersSelectorDefinition : EffectSelectorDefinition
    {
        public override IEffectSelector CreateRuntimeSelector() => new AllFriendlyCharactersSelector();
    }

    public class PlayerSelector : IEffectSelector, IPlayerChoiceSelector
    {
        public enum PlayerSelection
        {
            Self,
            Opponent,
            Any,
        }

        private readonly PlayerSelection _selection;

        public bool RequiresPlayerChoice => _selection == PlayerSelection.Any;

        public PlayerSelector(PlayerSelection selection = PlayerSelection.Self)
        {
            _selection = selection;
        }
        public List<ITargetable> Select(EffectContext effectContext)
        {
            if (effectContext?.GameState == null || effectContext.Source?.Owner == null)
                return new List<ITargetable>();

            switch (_selection)
            {
                case PlayerSelection.Self:
                    return new List<ITargetable> { effectContext.Source.Owner };
                case PlayerSelection.Opponent:
                    return new List<ITargetable> { effectContext.GameState.GetOpponent(effectContext.Source.Owner) };
                case PlayerSelection.Any:
                default:
                    return new List<ITargetable> { effectContext.Source.Owner, effectContext.GameState.GetOpponent(effectContext.Source.Owner) };
            }
        }
    }

    [Serializable]
    public sealed class PlayerSelectorDefinition : EffectSelectorDefinition
    {
        [SerializeField] private PlayerSelector.PlayerSelection _selection = PlayerSelector.PlayerSelection.Self;
        public override IEffectSelector CreateRuntimeSelector() => new PlayerSelector(_selection);
    }

    /// <summary>
    /// Candidate selector for interactive targeting: returns all cards currently in play
    /// (both friendly and enemy by default), optionally filtered.
    /// </summary>
    public sealed class SingleCardInPlaySelector : IEffectSelector, IPlayerChoiceSelector
    {
        public bool RequiresPlayerChoice => true;

        private readonly bool _mustBeOwnedBySourceOwner;
        private readonly CardType _mustBeType;

        public SingleCardInPlaySelector(bool mustBeOwnedBySourceOwner = false, CardType mustBeType = CardType.None)
        {
            _mustBeOwnedBySourceOwner = mustBeOwnedBySourceOwner;
            _mustBeType = mustBeType;
        }

        public List<ITargetable> Select(EffectContext effectContext)
        {
            var result = new List<ITargetable>();

            if (effectContext?.GameState == null || effectContext.Source?.Owner == null)
                return result;

            // Start from all cards in play.
            result.AddRange(GameState.GetFriendlyCharacters(effectContext.Source.Owner));

            if (!_mustBeOwnedBySourceOwner)
                result.AddRange(effectContext.GameState.GetEnemyCharacters(effectContext.Source.Owner));

            if (_mustBeType != CardType.None)
            {
                // Filter only Card targets by CardType.
                for (int i = result.Count - 1; i >= 0; i--)
                {
                    if (result[i] is Card card && card.Category != _mustBeType)
                        result.RemoveAt(i);
                }
            }

            return result;
        }
    }

    [Serializable]
    public sealed class SingleCardInPlaySelectorDefinition : EffectSelectorDefinition
    {
        [SerializeField] private bool _mustBeOwnedBySourceOwner;
        [SerializeField] private CardType _mustBeType = CardType.Troop;

        public override IEffectSelector CreateRuntimeSelector() =>
            new SingleCardInPlaySelector(
                mustBeOwnedBySourceOwner: _mustBeOwnedBySourceOwner,
                mustBeType: _mustBeType
            );
    }

}