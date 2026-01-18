using UnityEngine;

namespace Assets.Scripts.CardEngine.Game
{
	/// <summary>
	/// Enables selecting a Player as an ITargetable during TargetingManager flows
	/// by clicking on that player's board/play-area surface.
	/// </summary>
	[DisallowMultipleComponent]
	public sealed class PlayerTargetClickView : MonoBehaviour
	{
		private GameController _gameController;
		private Player _player;

		public void Bind(GameController gameController, Player player)
		{
			_gameController = gameController;
			_player = player;
		}

		private void OnMouseDown()
		{
			var gs = _gameController != null ? _gameController.GameState : null;
			if (gs?.Targeting == null || !gs.Targeting.IsActive)
				return;
			if (_player == null)
				return;

			gs.Targeting.TrySelect(_player);
		}
	}
}
