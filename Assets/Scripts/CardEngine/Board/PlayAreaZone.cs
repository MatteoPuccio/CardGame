using UnityEngine;
using System;
using System.Collections.Generic;
using Assets.Scripts.CardEngine.Cards;

namespace Assets.Scripts.CardEngine.Board
{
    /// <summary>
    /// Model for a single zone: Unity-free, pure data.
    /// </summary>
    public class PlayAreaZone
    {
        public int ZoneIndex { get; set; }
        public Card OccupyingCard { get; private set; }
        public bool IsOccupied => OccupyingCard != null;

        public event Action<Card> OnCardAssigned;
        public event Action OnCardRemoved;

        public bool TryOccupy(Card card)
        {
            if (IsOccupied) return false;
            OccupyingCard = card;
            OnCardAssigned?.Invoke(card);
            return true;
        }

        public void Vacate()
        {
            OccupyingCard = null;
            OnCardRemoved?.Invoke();
        }
    }

    
}