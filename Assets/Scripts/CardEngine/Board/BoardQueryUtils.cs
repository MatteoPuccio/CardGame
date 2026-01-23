using Assets.Scripts.CardEngine.Cards;

namespace Assets.Scripts.CardEngine.Board
{
	public static class BoardQueryUtils
	{
		public static PlayAreaZone FindZoneContaining(Card card)
		{
			var owner = card?.Owner;
			var zones = owner?.PlayZones;
			if (zones == null)
				return null;

			for (int i = 0; i < zones.Count; i++)
			{
				var zone = zones[i];
				if (zone != null && zone.OccupyingCard == card)
					return zone;
			}

			return null;
		}

		public static bool IsInPlayZone(Card card)
		{
			return FindZoneContaining(card) != null;
		}
	}
}
