using Assets.Scripts.CardEngine.Game;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Scripts.CardEngine.UI
{
    public sealed class OptionalEffectOptionClickHandler : MonoBehaviour, IPointerClickHandler
    {
        private OptionalEffectPromptUI _ui;
        private OptionalEffectOption _option;

        public void Bind(OptionalEffectPromptUI ui, OptionalEffectOption option)
        {
            _ui = ui;
            _option = option;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
                return;

            _ui?.SelectOption(_option);
        }
    }
}
