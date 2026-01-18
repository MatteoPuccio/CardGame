using Assets.Scripts.CardEngine.Game;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Scripts.CardEngine.UI
{
    public sealed class RapidEffectOptionClickHandler : MonoBehaviour, IPointerClickHandler
    {
        private RapidEffectPromptUI _ui;
        private RapidEffectOption _option;

        public void Bind(RapidEffectPromptUI ui, RapidEffectOption option)
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
