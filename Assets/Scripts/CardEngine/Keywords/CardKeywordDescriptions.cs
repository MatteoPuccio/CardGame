namespace Assets.Scripts.CardEngine.Keywords
{
    public static class CardKeywordDescriptions
    {
        public static string GetDisplayName(CardKeyword keyword)
        {
            return keyword.ToString();
        }

        public static string GetDescription(CardKeyword keyword)
        {
            return keyword switch
            {
                CardKeyword.Taunt => "Enemies must attack Taunt troops if any are present.",
                CardKeyword.FirstStrike => "Deals combat damage before a troop without FirstStrike.",
                CardKeyword.Lifesteal => "Heals its owner for damage it deals.",
                CardKeyword.BypassTroops => "Can attack the opposing player directly even if troops are present.",
                _ => string.Empty,
            };
        }
    }
}
