using Assets.Scripts.CardEngine.Game;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.CardEngine
{
	[DisallowMultipleComponent]
	public sealed class UIPhaseButton : MonoBehaviour
	{
		[SerializeField] private TurnPhase _phase;
		[SerializeField] private Button _button;

		public TurnPhase Phase => _phase;
		public Button Button => _button != null ? _button : (_button = GetComponent<Button>());

		private void Reset()
		{
			_button = GetComponent<Button>();
		}
	}
}
