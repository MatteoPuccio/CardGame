using Assets.Scripts.CardEngine.Cards;

namespace Assets.Scripts.CardEngine.Keywords
{
    public static class CardKeywordUtils
    {
        public static bool HasKeyword(Card card, CardKeyword keyword)
        {
            if (card?.Behavior is not TroopBehavior troop)
                return false;

            return troop.HasKeyword(keyword);
        }
    }
}
