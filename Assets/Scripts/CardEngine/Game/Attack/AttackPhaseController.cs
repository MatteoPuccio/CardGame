using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Assets.Scripts.CardEngine.Board;
using Assets.Scripts.CardEngine.Cards;
using Assets.Scripts.CardEngine.Events;
using Assets.Scripts.CardEngine.Keywords;
using UnityEngine;

namespace Assets.Scripts.CardEngine.Game
{
	/// <summary>
	/// Attack phase controller: each declared attack runs through AttackSteps,
	/// then the controller returns to Start and waits for the next attacker declaration.
	/// The Attack phase ends when the turn phase changes away from TurnPhase.Attack.
	/// </summary>
	public sealed class AttackPhaseController
	{
		private readonly GameState _gameState;
		private readonly EventBus _bus;

		private bool _isActive;
		private bool _isResolving;
		private bool _awaitingDeclaration;
		private AttackSteps _step = AttackSteps.End;
		private readonly HashSet<Card> _attackersThatAttackedThisPhase = new();

		public AttackSteps Step => _step;
		public bool IsActive => _isActive;

		public AttackPhaseController(GameState gameState)
		{
			_gameState = gameState ?? throw new ArgumentNullException(nameof(gameState));
			_bus = _gameState.EventBus;
		}

		public void BeginAttackPhase()
		{
			if (_isActive)
				return;

			_isActive = true;
			_isResolving = false;
			_awaitingDeclaration = true;
			_step = AttackSteps.Start;
			_attackersThatAttackedThisPhase.Clear();

			PrepareNextDeclaration();
		}

		public void EndAttackPhase(string reason = null)
		{
			if (!_isActive)
				return;

			// If the user switches to End phase mid-targeting, cancel cleanly.
			_gameState.Targeting?.Cancel(reason ?? "Attack phase ended.");
			_isActive = false;
			_isResolving = false;
			_awaitingDeclaration = false;
			_step = AttackSteps.End;
			_attackersThatAttackedThisPhase.Clear();
		}

		/// <summary>
		/// UI hook: begin defender targeting for a specific attacker (chosen via sword icon).
		/// </summary>
		public bool TryBeginAttackWithAttacker(Card attacker, out string reason)
		{
			reason = null;
			if (!_isActive)
			{
				reason = "Attack phase is not active.";
				return false;
			}
			if (_isResolving)
			{
				reason = "Already resolving an attack.";
				return false;
			}
			if (!_awaitingDeclaration)
			{
				reason = "Not ready to declare an attack.";
				return false;
			}
			if (_gameState.Phase != TurnPhase.Attack)
			{
				reason = "Not in Attack phase.";
				return false;
			}
			if (_gameState.Targeting != null && _gameState.Targeting.IsActive)
			{
				reason = "Already targeting.";
				return false;
			}
			if (!CanDeclareAttackFrom(attacker))
			{
				reason = "This troop cannot attack right now.";
				return false;
			}

			// Automatic direct attack: if the opponent has no troops in play, there's no meaningful target choice.
			// Skip targeting and immediately resolve the attack against the opponent player.
			var opponent = attacker?.Owner != null ? _gameState.GetOpponent(attacker.Owner) : null;
			if (opponent != null && !AttackRules.PlayerHasAnyTroopsInPlay(opponent))
			{
				_step = AttackSteps.DeclareAttackers;
				_awaitingDeclaration = false;
				_attackersThatAttackedThisPhase.Add(attacker);
				OnAttackDeclared(new AttackDeclaration(attacker, opponent));
				return true;
			}

			_step = AttackSteps.DeclareAttackers;
			var session = new AttackTargetingSession(_gameState, OnAttackDeclared, attacker);
			if (!session.TryAdvance(out var candidates) || candidates == null || candidates.Count == 0)
			{
				reason = "No valid targets.";
				return false;
			}

			_awaitingDeclaration = false;
			_attackersThatAttackedThisPhase.Add(attacker);
			_gameState.Targeting?.Begin(session, candidates, onCancelled: () =>
			{
				// If the player cancels targeting, allow the attacker to be used again.
				_attackersThatAttackedThisPhase.Remove(attacker);
				if (_isActive && !_isResolving && _gameState.Phase == TurnPhase.Attack)
					_awaitingDeclaration = true;
			});

			return true;
		}

		public bool CanDeclareAttackFrom(Card attacker)
		{
			if (!_isActive)
				return false;
			if (_isResolving)
				return false;
			if (!_awaitingDeclaration)
				return false;
			if (_gameState.Phase != TurnPhase.Attack)
				return false;
			if (_gameState.Targeting != null && _gameState.Targeting.IsActive)
				return false;
			if (_gameState.TurnNumber <= 1)
				return false;

			if (attacker == null)
				return false;
			if (_gameState.ActivePlayer == null)
				return false;
			if (attacker.Owner != _gameState.ActivePlayer)
				return false;
			if (attacker.Behavior is not TroopBehavior)
				return false;
			if (!BoardQueryUtils.IsInPlayZone(attacker))
				return false;
			if (_attackersThatAttackedThisPhase.Contains(attacker))
				return false;

			return true;
		}

		private void PrepareNextDeclaration()
		{
			if (!_isActive)
				return;
			if (_isResolving)
				return;
			if (_gameState.Phase != TurnPhase.Attack)
				return;

			_step = AttackSteps.Start;
			var startEvt = new AttackStartedEvent(attackingPlayer: _gameState.ActivePlayer, turnNumber: _gameState.TurnNumber);
			_bus?.Publish(startEvt);

			_step = AttackSteps.DeclareAttackers;
			_awaitingDeclaration = true;
			// UI will call TryBeginAttackWithAttacker() when the player clicks a sword icon.
		}

		private void OnAttackDeclared(AttackDeclaration declaration)
		{
			if (!_isActive || _isResolving)
				return;
			// Safety: ensure the attacker is marked as having attacked even if the declaration came from elsewhere.
			if (declaration?.Attacker != null)
				_attackersThatAttackedThisPhase.Add(declaration.Attacker);

			_isResolving = true;
			_ = ResolveAttackAsync(declaration);
		}

		private async Task ResolveAttackAsync(AttackDeclaration declaration)
		{
			try
			{
				if (!_isActive || _gameState.Phase != TurnPhase.Attack)
					return;

				// Trigger: "when attacks" effects + priority window
				var declaredEvt = new AttackDeclaredEvent(attackingPlayer: _gameState.ActivePlayer, declaration: declaration);
				_bus?.Publish(declaredEvt);
				await OpenPriorityWindowAsync(declaredEvt);

				if (!_isActive || _gameState.Phase != TurnPhase.Attack)
					return;

				_step = AttackSteps.DamageCalculation;
				var pending = BuildPendingDamage(declaration);
				ApplyPendingDamage(pending);

				// Trigger: damage effects + priority window
				var damageEvt = new AttackDamageAppliedEvent(attackingPlayer: _gameState.ActivePlayer, damages: pending, source: declaration?.Attacker);
				_bus?.Publish(damageEvt);
				await OpenPriorityWindowAsync(damageEvt);

				_step = AttackSteps.End;
				var endEvt = new AttackEndedEvent(attackingPlayer: _gameState.ActivePlayer, source: declaration?.Attacker);
				_bus?.Publish(endEvt);
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
			finally
			{
				_isResolving = false;
				// Restart the per-attack sequence for the next attacker.
				PrepareNextDeclaration();
			}
		}

		private List<PendingAttackDamage> BuildPendingDamage(AttackDeclaration declaration)
		{
			var result = new List<PendingAttackDamage>();
			var attacker = declaration?.Attacker;
			if (attacker?.Behavior is not TroopBehavior attackerTroop)
				return result;

			int atk = attackerTroop.Power;
			if (atk < 0) atk = 0;

			if (declaration.Defender is Card defenderCard)
			{
				if (defenderCard.Behavior is not TroopBehavior defenderTroop)
					return result;

				int defAtk = defenderTroop.Power;
				if (defAtk < 0) defAtk = 0;

				result.Add(new PendingAttackDamage(instigator: attacker, target: defenderCard, amount: atk));
				result.Add(new PendingAttackDamage(instigator: defenderCard, target: attacker, amount: defAtk));
				_bus?.Publish(new AttackModifyPendingDamageEvent(declaration, result));
				return result;
			}

			if (declaration.Defender is Player defenderPlayer)
			{
				result.Add(new PendingAttackDamage(instigator: attacker, target: defenderPlayer, amount: atk));
				return result;
			}

			return result;
		}

		private void ApplyPendingDamage(List<PendingAttackDamage> pending)
		{
			if (pending == null || pending.Count == 0)
				return;

			for (int i = 0; i < pending.Count; i++)
			{
				var pd = pending[i];
				if (pd.Amount <= 0)
					continue;

				if (pd.Target is Card targetCard)
				{
					_gameState.ApplyDamage(targetCard, pd.Amount, instigator: pd.Instigator);
				}
				else if (pd.Target is Player targetPlayer)
				{
					ApplyPlayerDamage(targetPlayer, pd.Amount, instigator: pd.Instigator);
				}
			}
		}

		private void ApplyPlayerDamage(Player player, int amount, Card instigator)
		{
			if (player == null || amount <= 0)
				return;

			uint before = player.Life;
			uint dmg = (uint)Mathf.Max(0, amount);
			player.Life = dmg >= before ? 0 : before - dmg;

			if (player.Life != before)
				_bus?.Publish(new PlayerLifeChangedEvent(player, before, player.Life, source: instigator));

			if (before > 0 && player.Life == 0)
				_bus?.Publish(new PlayerDefeatedEvent(player, source: instigator));
		}

		private async Task OpenPriorityWindowAsync(IGameEvent triggeringEvent)
		{
			var chain = _gameState.RapidEffectChain;
			if (chain == null || triggeringEvent == null)
				return;

			try
			{
				await chain.TryOpenChainWindowAsync(triggeringEvent);
			}
			catch
			{
				// Priority UI should not hard-block attack flow.
			}
		}
	}
}
