using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Game;

namespace Assets.Scripts.CardEngine.Events
{
	public sealed class TroopDamagedEvent : IGameEvent
	{
		public string EventType => "TroopDamaged";
		public Card Source { get; }

		public Card Instigator { get; }
		public int Amount { get; }

		public TroopDamagedEvent(Card troop, int amount, Card instigator = null)
		{
			Source = troop;
			Instigator = instigator;
			Amount = amount;
		}
	}

	public sealed class TroopDiedEvent : IGameEvent
	{
		public string EventType => "TroopDied";
		public Card Source { get; }

		public Card Instigator { get; }
		public bool MovedToCemetery { get; }

		public TroopDiedEvent(Card troop, Card instigator = null, bool movedToCemetery = false)
		{
			Source = troop;
			Instigator = instigator;
			MovedToCemetery = movedToCemetery;
		}
	}
}
