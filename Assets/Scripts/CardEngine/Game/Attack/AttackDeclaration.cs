using System;
using Assets.Scripts.CardEngine.Cards;

namespace Assets.Scripts.CardEngine.Game
{
	public sealed class AttackDeclaration
	{
		public Card Attacker { get; }
		public ITargetable Defender { get; }

		public AttackDeclaration(Card attacker, ITargetable defender)
		{
			Attacker = attacker ?? throw new ArgumentNullException(nameof(attacker));
			Defender = defender ?? throw new ArgumentNullException(nameof(defender));
		}
	}
}
