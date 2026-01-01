using System;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Game;

namespace Assets.Scripts.CardEngine.Board
{
    public class PlayAreaZone: ICardZone
    {
        public int ZoneIndex { get; set; }
        public Card OccupyingCard { get; private set; }
        public bool IsOccupied => OccupyingCard != null;

        public event Action<Card> OnCardAssigned;
        public event Action OnCardRemoved;
        public string ZoneName => $"PlayAreaZone_{ZoneIndex}";

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

        public bool CanEnter(Card card)
        {
            return !IsOccupied;
        }

        public bool EnterCard(Card card)
        {
            return TryOccupy(card);
        }

        public bool ExitCard(Card card)
        {
            if (OccupyingCard != card)
                return false;
            Vacate();
            return true;
        }
    }

    
}