using System;

namespace Assets.Scripts.CardEngine.Utils
{
	/// <summary>
	/// Identifies the different card zones in the game.
	/// </summary>
	public enum CardZone
	{
		None = 0,
		Deck,
		ExtraDeck,
		Hand,
		Cemetery,
		RitualZone,
		InPlay,  // Covers all PlayAreaZone_* slots
	}

	/// <summary>
	/// Flags enum for querying multiple zones at once.
	/// </summary>
	[Flags]
	public enum CardZones
	{
		None = 0,
		Deck = 1 << 0,
		ExtraDeck = 1 << 1,
		Hand = 1 << 2,
		Cemetery = 1 << 3,
		RitualZone = 1 << 4,
		InPlay = 1 << 5,

		/// <summary>All zones combined.</summary>
		All = Deck | ExtraDeck | Hand | Cemetery | RitualZone | InPlay,
	}

	/// <summary>
	/// String constants for zone names (used by ICardZone.ZoneName).
	/// </summary>
	public static class ZoneNames
	{
		public const string Deck = "Deck";
		public const string ExtraDeck = "ExtraDeck";
		public const string Hand = "Hand";
		public const string Cemetery = "Cemetery";
		public const string RitualZone = "RitualZone";
		public const string PlayAreaZonePrefix = "PlayAreaZone_";

		/// <summary>
		/// Converts a CardZone enum to its string name.
		/// Note: InPlay returns the prefix; individual play zones have indexed names.
		/// </summary>
		public static string ToZoneName(CardZone zone) => zone switch
		{
			CardZone.Deck => Deck,
			CardZone.ExtraDeck => ExtraDeck,
			CardZone.Hand => Hand,
			CardZone.Cemetery => Cemetery,
			CardZone.RitualZone => RitualZone,
			CardZone.InPlay => PlayAreaZonePrefix,
			_ => string.Empty,
		};

		/// <summary>
		/// Parses a zone name string to CardZone enum.
		/// </summary>
		public static CardZone FromZoneName(string zoneName)
		{
			if (string.IsNullOrEmpty(zoneName))
				return CardZone.None;

			if (string.Equals(zoneName, Deck, StringComparison.Ordinal))
				return CardZone.Deck;
			if (string.Equals(zoneName, ExtraDeck, StringComparison.Ordinal))
				return CardZone.ExtraDeck;
			if (string.Equals(zoneName, Hand, StringComparison.Ordinal))
				return CardZone.Hand;
			if (string.Equals(zoneName, Cemetery, StringComparison.Ordinal))
				return CardZone.Cemetery;
			if (string.Equals(zoneName, RitualZone, StringComparison.Ordinal))
				return CardZone.RitualZone;
			if (zoneName.StartsWith(PlayAreaZonePrefix, StringComparison.Ordinal))
				return CardZone.InPlay;

			return CardZone.None;
		}

		/// <summary>
		/// Converts CardZones to the equivalent CardZone (only if a single flag is set).
		/// </summary>
		public static CardZone ToCardZone(CardZones flags) => flags switch
		{
			CardZones.Deck => CardZone.Deck,
			CardZones.ExtraDeck => CardZone.ExtraDeck,
			CardZones.Hand => CardZone.Hand,
			CardZones.Cemetery => CardZone.Cemetery,
			CardZones.RitualZone => CardZone.RitualZone,
			CardZones.InPlay => CardZone.InPlay,
			_ => CardZone.None,
		};

		/// <summary>
		/// Converts a CardZone to its equivalent flag.
		/// </summary>
		public static CardZones ToFlags(CardZone zone) => zone switch
		{
			CardZone.Deck => CardZones.Deck,
			CardZone.ExtraDeck => CardZones.ExtraDeck,
			CardZone.Hand => CardZones.Hand,
			CardZone.Cemetery => CardZones.Cemetery,
			CardZone.RitualZone => CardZones.RitualZone,
			CardZone.InPlay => CardZones.InPlay,
			_ => CardZones.None,
		};
	}
}
