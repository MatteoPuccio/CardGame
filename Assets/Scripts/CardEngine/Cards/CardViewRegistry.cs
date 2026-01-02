using System;
using System.Collections.Generic;

namespace Assets.Scripts.CardEngine.Cards
{
    public class CardViewRegistry
    {
        private readonly Dictionary<Card, CardView> _byCard = new();

        public bool TryGet(Card card, out CardView view)
        {
            if (card == null)
            {
                view = null;
                return false;
            }

            return _byCard.TryGetValue(card, out view) && view != null;
        }

        public CardView GetOrNull(Card card)
        {
            TryGet(card, out var view);
            return view;
        }

        public void Register(Card card, CardView view)
        {
            if (card == null)
                throw new ArgumentNullException(nameof(card));
            if (view == null)
                throw new ArgumentNullException(nameof(view));

            _byCard[card] = view;
        }

        public bool Unregister(Card card)
        {
            if (card == null)
                return false;

            return _byCard.Remove(card);
        }

        public void Clear()
        {
            _byCard.Clear();
        }
    }
}
