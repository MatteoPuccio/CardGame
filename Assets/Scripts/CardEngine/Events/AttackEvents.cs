using System;
using System.Collections.Generic;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Game;

namespace Assets.Scripts.CardEngine.Events
{
	public sealed class AttackStartedEvent : IGameEvent
	{
		public string EventType => "AttackStarted";
		public Card Source { get; }
		public Player AttackingPlayer { get; }
		public int TurnNumber { get; }

		public AttackStartedEvent(Player attackingPlayer, int turnNumber, Card source = null)
		{
			AttackingPlayer = attackingPlayer;
			TurnNumber = turnNumber;
			Source = source;
		}
	}

	public sealed class AttackDeclaredEvent : IGameEvent
	{
		public string EventType => "AttackDeclared";
		public Card Source { get; }
		public Player AttackingPlayer { get; }
		public AttackDeclaration Declaration { get; }

		public AttackDeclaredEvent(Player attackingPlayer, AttackDeclaration declaration)
		{
			AttackingPlayer = attackingPlayer;
			Declaration = declaration;
			Source = declaration?.Attacker;
		}
	}

	public sealed class AttackDamageAppliedEvent : IGameEvent
	{
		public string EventType => "AttackDamageApplied";
		public Card Source { get; }
		public Player AttackingPlayer { get; }
		public IReadOnlyList<PendingAttackDamage> Damages { get; }

		public AttackDamageAppliedEvent(Player attackingPlayer, IReadOnlyList<PendingAttackDamage> damages, Card source = null)
		{
			AttackingPlayer = attackingPlayer;
			Damages = damages ?? Array.Empty<PendingAttackDamage>();
			Source = source;
		}
	}

	public sealed class AttackEndedEvent : IGameEvent
	{
		public string EventType => "AttackEnded";
		public Card Source { get; }
		public Player AttackingPlayer { get; }

		public AttackEndedEvent(Player attackingPlayer, Card source = null)
		{
			AttackingPlayer = attackingPlayer;
			Source = source;
		}
	}

	public readonly struct PendingAttackDamage
	{
		public readonly Card Instigator;
		public readonly ITargetable Target;
		public readonly int Amount;

		public PendingAttackDamage(Card instigator, ITargetable target, int amount)
		{
			Instigator = instigator;
			Target = target;
			Amount = amount;
		}
	}
}
