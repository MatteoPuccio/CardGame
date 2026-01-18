using System;
using System.Collections.Generic;
using Assets.Scripts.CardEngine.Cards;

namespace Assets.Scripts.CardEngine.Game
{
	/// <summary>
	/// Drives a single attack declaration using the existing TargetingManager click pipeline:
	/// pick attacker (friendly troop in play) -> pick defender (enemy troop; or enemy player if direct attack is legal).
	/// When a declaration is completed, the session finishes (TargetingManager clears it).
	/// </summary>
	public sealed class AttackTargetingSession : ITargetingSession
	{
		private readonly GameState _gameState;
		private readonly Action<AttackDeclaration> _onDeclared;

		private Card _pendingAttacker;
		private bool _wasCancelled;
		private string _cancelReason;
		private bool _isFinished;

		public Card Card => _pendingAttacker;
		public bool WasCancelled => _wasCancelled;
		public string CancelReason => _cancelReason;

		public AttackTargetingSession(GameState gameState, Action<AttackDeclaration> onDeclared, Card attacker = null)
		{
			_gameState = gameState ?? throw new ArgumentNullException(nameof(gameState));
			_onDeclared = onDeclared ?? throw new ArgumentNullException(nameof(onDeclared));
			if (attacker != null && TryValidateAttacker(attacker))
				_pendingAttacker = attacker;
		}

		public bool TryAdvance(out List<ITargetable> candidates)
		{
			candidates = null;
			if (_wasCancelled || _isFinished)
				return false;

			if (_gameState.ActivePlayer == null)
			{
				Cancel("No active player.");
				return false;
			}
			if (_gameState.Phase != TurnPhase.Attack)
			{
				Cancel("Not in Attack phase.");
				return false;
			}

			if (_pendingAttacker == null)
			{
				candidates = GetAttackers();
				return candidates.Count > 0;
			}

			candidates = GetDefenders(_pendingAttacker);
			return candidates.Count > 0;
		}

		public void ProvideTargets(List<ITargetable> targets)
		{
			if (_wasCancelled || _isFinished)
				return;

			var chosen = targets != null && targets.Count > 0 ? targets[0] : null;
			if (chosen == null)
				return;

			if (_pendingAttacker == null)
			{
				if (chosen is not Card attacker)
					return;
				if (!TryValidateAttacker(attacker))
					return;
				_pendingAttacker = attacker;
				return;
			}

			if (!TryValidateDefender(_pendingAttacker, chosen))
				return;

			_isFinished = true;
			_onDeclared(new AttackDeclaration(_pendingAttacker, chosen));
		}

		public void Cancel(string reason = null)
		{
			_wasCancelled = true;
			_cancelReason = string.IsNullOrWhiteSpace(reason) ? "Cancelled." : reason;
			_pendingAttacker = null;
			_isFinished = true;
		}

		private List<ITargetable> GetAttackers()
		{
			var result = new List<ITargetable>();
			var player = _gameState.ActivePlayer;
			if (player?.PlayZones == null)
				return result;

			foreach (var zone in player.PlayZones)
			{
				var card = zone?.OccupyingCard;
				if (card == null)
					continue;
				if (!TryValidateAttacker(card))
					continue;
				result.Add(card);
			}

			return result;
		}

		private List<ITargetable> GetDefenders(Card attacker)
		{
			var result = new List<ITargetable>();
			if (attacker?.Owner == null)
				return result;

			var opponent = _gameState.GetOpponent(attacker.Owner);
			if (opponent == null)
				return result;

			bool opponentHasTroops = AttackRules.PlayerHasAnyTroopsInPlay(opponent);

			if (opponent.PlayZones != null)
			{
				foreach (var zone in opponent.PlayZones)
				{
					var card = zone?.OccupyingCard;
					if (card?.Behavior is TroopBehavior)
						result.Add(card);
				}
			}

			// Yugioh-style: only allow direct attack if opponent has no troops, unless keywords allow.
			if (!opponentHasTroops || AttackRules.AttackerCanDirectAttackThroughTroops(attacker))
				result.Add(opponent);

			return result;
		}

		private bool TryValidateAttacker(Card attacker)
		{
			if (attacker == null)
				return false;
			if (_gameState.ActivePlayer == null)
				return false;
			if (attacker.Owner != _gameState.ActivePlayer)
				return false;
			if (attacker.Behavior is not TroopBehavior)
				return false;

			return true;
		}

		private bool TryValidateDefender(Card attacker, ITargetable defender)
		{
			if (attacker == null || defender == null)
				return false;

			var opponent = attacker.Owner != null ? _gameState.GetOpponent(attacker.Owner) : null;
			if (opponent == null)
				return false;

			if (defender is Player)
			{
				bool opponentHasTroops = AttackRules.PlayerHasAnyTroopsInPlay(opponent);
				return !opponentHasTroops || AttackRules.AttackerCanDirectAttackThroughTroops(attacker);
			}

			if (defender is Card targetCard)
			{
				if (targetCard.Owner != opponent)
					return false;
				return targetCard.Behavior is TroopBehavior;
			}

			return false;
		}
	}

	internal static class AttackRules
	{
		public static bool PlayerHasAnyTroopsInPlay(Player player)
		{
			if (player?.PlayZones == null)
				return false;
			foreach (var zone in player.PlayZones)
			{
				if (zone?.OccupyingCard?.Behavior is TroopBehavior)
					return true;
			}
			return false;
		}

		public static bool AttackerCanDirectAttackThroughTroops(Card attacker)
		{
			// Extension hook for future keywords.
			return false;
		}
	}
}
